using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 同心円ドットグリッド ＋ STG 全方位弾幕。ドットサイズ・間隔・弾速は画面最小辺に対する比率で算出する。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class RadialDotPatternBackground : MonoBehaviour
    {
        public enum RadialDotLayout
        {
            Concentric = 0,
            HexRadial = 1,
            Phyllotaxis = 2,
        }

        public enum RadialDotAnimMode
        {
            Bullet = 0,
            Expand = 1,
            Pulse = 2,
            Wave = 3,
            None = 4,
        }

        struct Dot
        {
            public float X;
            public float Y;
            public float Dist;
            public float Angle;
            public int Ring;
        }

        const float DefaultDotRadiusRatio = 0.012f;
        const float DefaultSpacingRatio = 0.035f;
        /// <summary>旧 HTML の 60px/s を spacing=35 相当に正規化した係数（spacing × 係数 × speed = 半径方向 px/s）。</summary>
        const float DefaultBulletRadialSpeedPerSpacing = 60f / 35f;

        [Header("Grid")]
        [SerializeField] RadialDotLayout layout = RadialDotLayout.Concentric;
        [Tooltip("画面端まで届くリング数に加える余白（リング単位）")]
        [SerializeField] int extraCoverRings = 1;
        [SerializeField] int maxRingLayers = 64;
        [Tooltip("dotRadius = min(幅,高さ) × この値")]
        [SerializeField] float dotRadiusRatio = DefaultDotRadiusRatio;
        [Tooltip("spacing = min(幅,高さ) × この値")]
        [SerializeField] float spacingRatio = DefaultSpacingRatio;
        [Tooltip("白ドット半径 = spacing × この値（0.5 で直径=spacing。隙間ギリギリなら 0.47 前後）")]
        [SerializeField] float gridDotRadiusSpacingFraction = 0.47f;

        [Header("Bullet streams")]
        [SerializeField] bool bulletRingsActive = true;
        [SerializeField] float bulletWaveGap = 3f;
        [SerializeField] float bulletSpeed1 = 1f;
        [SerializeField] float bulletSpeed2 = 1.5f;
        [SerializeField] float bulletSpeed3 = 2.2f;
        [Tooltip("弾幕リングの半径方向速度 = spacing × この値 × 各弾速")]
        [SerializeField] float bulletRadialSpeedPerSpacing = DefaultBulletRadialSpeedPerSpacing;

        [Header("Motion")]
        [SerializeField] RadialDotAnimMode animMode = RadialDotAnimMode.Bullet;
        [SerializeField] float expandSpeedPerSpacing = 1.14f;
        [SerializeField] float waveAmplitudeSpacingFraction = 0.23f;

        [Header("Appearance")]
        [SerializeField] Color32 backgroundColor = new Color32(0x1e, 0x1e, 0x1e, 0xff);
        [SerializeField] Color32 gridDotColor = new Color32(255, 255, 255, 230);
        static readonly Color32 BulletPink = new Color32(0xff, 0x2a, 0x85, 255);
        static readonly Color32 BulletRed = new Color32(0xff, 0x22, 0x33, 255);
        static readonly Color32 BulletViolet = new Color32(0xd0, 0x20, 0xff, 255);

        [Header("Performance")]
        [SerializeField] int maxRenderWidth = 960;
        [SerializeField] int framesPerRender = 1;

        [Header("Control")]
        [SerializeField] bool paused;

        RawImage _rawImage;
        Texture2D _texture;
        Color32[] _pixels;
        int _texW;
        int _texH;
        int _viewW;
        int _viewH;
        int _frame;
        float _time;
        float _viewMinDimension;
        float _dotRadius;
        float _spacing;
        float _coverRadius;
        readonly List<Dot> _dots = new List<Dot>();

        public bool Paused
        {
            get => paused;
            set => paused = value;
        }

        void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _rawImage.raycastTarget = false;
            _rawImage.color = Color.white;
            _rawImage.material = null;
        }

        IEnumerator Start()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
            RebuildTexture();
        }

        void OnRectTransformDimensionsChange()
        {
            if (_rawImage == null || !_rawImage.isActiveAndEnabled)
                return;

            ResolveViewSize(out int viewW, out int viewH);
            ResolveTargetSize(out int texW, out int texH);

            bool viewChanged = viewW != _viewW || viewH != _viewH;
            bool texChanged = _texture == null || texW != _texW || texH != _texH;

            if (!viewChanged && !texChanged)
                return;

            if (texChanged)
            {
                RebuildTexture();
                return;
            }

            _viewW = viewW;
            _viewH = viewH;
            RecomputeLayoutMetrics();
            RebuildDotGrid();
        }

        void Update()
        {
            if (_texture == null || _pixels == null)
                return;

            if (!paused)
                _time += Time.deltaTime;

            _frame++;
            if (_frame % Mathf.Max(1, framesPerRender) != 0)
                return;

            Render();
            PushTexture();
        }

        public void PlayBackground()
        {
            paused = false;
            _rawImage.color = Color.white;
            _rawImage.material = null;
            Canvas.ForceUpdateCanvases();
            RebuildTexture();
        }

        public void StopBackground()
        {
            paused = true;
        }

        void PushTexture()
        {
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        void ResolveViewSize(out int w, out int h)
        {
            var rt = _rawImage.rectTransform;
            w = Mathf.RoundToInt(rt.rect.width);
            h = Mathf.RoundToInt(rt.rect.height);

            if (w < 2 || h < 2)
            {
                w = Mathf.RoundToInt(Mathf.Abs(rt.sizeDelta.x));
                h = Mathf.RoundToInt(Mathf.Abs(rt.sizeDelta.y));
            }

            var canvas = _rawImage.canvas;
            if ((w < 2 || h < 2) && canvas != null)
            {
                var pr = canvas.rootCanvas.pixelRect;
                w = Mathf.Max(w, Mathf.RoundToInt(pr.width));
                h = Mathf.Max(h, Mathf.RoundToInt(pr.height));
            }

            w = Mathf.Max(64, w);
            h = Mathf.Max(64, h);
        }

        void ResolveTargetSize(out int w, out int h)
        {
            ResolveViewSize(out w, out h);

            if (w > maxRenderWidth)
            {
                float aspect = (float)h / w;
                w = maxRenderWidth;
                h = Mathf.Max(64, Mathf.RoundToInt(w * aspect));
            }
        }

        /// <summary>表示矩形とテクスチャ解像度から、テクスチャ空間の dotRadius / spacing を算出する。</summary>
        void RecomputeLayoutMetrics()
        {
            ResolveViewSize(out _viewW, out _viewH);
            _viewMinDimension = Mathf.Min(_viewW, _viewH);

            float texMin = Mathf.Min(_texW, _texH);
            float viewMin = Mathf.Max(1f, _viewMinDimension);
            float texToView = texMin / viewMin;

            _dotRadius = _viewMinDimension * dotRadiusRatio * texToView;
            _spacing = _viewMinDimension * spacingRatio * texToView;

            float halfW = _texW * 0.5f;
            float halfH = _texH * 0.5f;
            _coverRadius = Mathf.Sqrt(halfW * halfW + halfH * halfH)
                + Mathf.Max(_dotRadius, _spacing * gridDotRadiusSpacingFraction) * 2f;
        }

        int ComputeRingLayersForCover()
        {
            if (_spacing < 0.001f)
                return 1;

            float coverUnits = _coverRadius / _spacing;
            int rings = Mathf.CeilToInt(coverUnits) + extraCoverRings;
            return Mathf.Clamp(rings, 1, maxRingLayers);
        }

        void RebuildDotGrid()
        {
            _dots.Clear();
            if (_spacing < 0.001f)
                return;

            int rings = ComputeRingLayersForCover();
            float coverUnits = _coverRadius / _spacing;
            const float unitSpacing = 1f;

            switch (layout)
            {
                case RadialDotLayout.HexRadial:
                    GenerateHexDots(rings, unitSpacing);
                    break;
                case RadialDotLayout.Phyllotaxis:
                    GeneratePhyllotaxisDots(coverUnits, unitSpacing);
                    break;
                default:
                    GenerateConcentricDots(rings, unitSpacing);
                    break;
            }
        }

        public void RebuildTexture()
        {
            ResolveTargetSize(out int w, out int h);

            _texW = w;
            _texH = h;
            _pixels = new Color32[_texW * _texH];

            if (_texture != null)
                Destroy(_texture);

            _texture = new Texture2D(_texW, _texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _rawImage.texture = _texture;

            RecomputeLayoutMetrics();
            RebuildDotGrid();
            Render();
            PushTexture();
        }

        void Render()
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = backgroundColor;

            float centerX = _texW * 0.5f;
            float centerY = _texH * 0.5f;
            float dotRadius = Mathf.Max(0.5f, _dotRadius);
            float spacing = Mathf.Max(0.001f, _spacing);
            float gridRadius = spacing * gridDotRadiusSpacingFraction;
            float maxR = _coverRadius;

            foreach (var dot in _dots)
            {
                float x = dot.X * spacing;
                float y = dot.Y * spacing;
                float currentRadius = gridRadius;
                float opacity = 0.9f;

                float r = dot.Dist * spacing;
                float angle = dot.Angle;

                switch (animMode)
                {
                    case RadialDotAnimMode.Expand:
                    {
                        float expandSpeed = spacing * expandSpeedPerSpacing;
                        float expandedR = (r + _time * expandSpeed) % maxR;
                        x = Mathf.Cos(angle) * expandedR;
                        y = Mathf.Sin(angle) * expandedR;
                        opacity = Mathf.Sin((expandedR / maxR) * Mathf.PI) * 0.9f;
                        break;
                    }
                    case RadialDotAnimMode.Pulse:
                    {
                        float ringIndex = spacing > 0.001f ? r / spacing : 0f;
                        float scale = 1f + 0.15f * Mathf.Sin(_time * 3f - ringIndex * 0.35f);
                        currentRadius *= scale;
                        break;
                    }
                    case RadialDotAnimMode.Wave:
                    {
                        float ringIndex = spacing > 0.001f ? r / spacing : 0f;
                        float waveOffset = Mathf.Sin(_time * 4f - ringIndex * 0.45f)
                            * spacing * waveAmplitudeSpacingFraction;
                        float newR = Mathf.Max(0f, r + waveOffset);
                        x = Mathf.Cos(angle) * newR;
                        y = Mathf.Sin(angle) * newR;
                        break;
                    }
                }

                var c = gridDotColor;
                c.a = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, opacity) * c.a), 0, 255);
                DrawFilledCircle(centerX + x, centerY - y, currentRadius, c);
            }

            if (bulletRingsActive || animMode == RadialDotAnimMode.Bullet)
                DrawBulletRings(centerX, centerY, dotRadius, spacing, maxR);
        }

        void DrawBulletRings(float centerX, float centerY, float dotRadius, float spacing, float maxR)
        {
            float waveStep = spacing * bulletWaveGap;
            if (waveStep < 0.5f)
                return;

            var streams = new[]
            {
                (BulletPink, bulletSpeed1),
                (BulletRed, bulletSpeed2),
                (BulletViolet, bulletSpeed3),
            };

            foreach (var (color, speed) in streams)
            {
                float radialSpeed = spacing * bulletRadialSpeedPerSpacing * speed;
                float currentWaveR = (_time * radialSpeed) % waveStep;
                for (float r = currentWaveR; r <= maxR; r += waveStep)
                {
                    if (r <= 0f)
                        continue;

                    int bulletCount = Mathf.Max(6, Mathf.FloorToInt(r / spacing * 6f));
                    for (int i = 0; i < bulletCount; i++)
                    {
                        float angle = i / (float)bulletCount * Mathf.PI * 2f;
                        float bx = centerX + Mathf.Cos(angle) * r;
                        float by = centerY - Mathf.Sin(angle) * r;
                        DrawFilledCircle(bx, by, dotRadius, color);
                    }
                }
            }
        }

        void DrawFilledCircle(float cx, float cy, float radius, Color32 color)
        {
            int ir = Mathf.CeilToInt(radius);
            int cxI = Mathf.RoundToInt(cx);
            int cyI = Mathf.RoundToInt(cy);
            float r2 = radius * radius;

            for (int dy = -ir; dy <= ir; dy++)
            {
                int py = cyI + dy;
                if (py < 0 || py >= _texH)
                    continue;
                int row = py * _texW;
                for (int dx = -ir; dx <= ir; dx++)
                {
                    if (dx * dx + dy * dy > r2)
                        continue;
                    int px = cxI + dx;
                    if (px < 0 || px >= _texW)
                        continue;
                    int idx = row + px;
                    _pixels[idx] = BlendOver(_pixels[idx], color, color.a / 255f);
                }
            }
        }

        static Color32 BlendOver(Color32 dst, Color32 src, float a)
        {
            float ia = 1f - a;
            return new Color32(
                (byte)Mathf.Clamp(dst.r * ia + src.r * a, 0, 255),
                (byte)Mathf.Clamp(dst.g * ia + src.g * a, 0, 255),
                (byte)Mathf.Clamp(dst.b * ia + src.b * a, 0, 255),
                255);
        }

        void GenerateConcentricDots(int ringLayersLocal, float spacing)
        {
            _dots.Add(new Dot());
            for (int ring = 1; ring <= ringLayersLocal; ring++)
            {
                int count = ring * 6;
                float radius = ring * spacing;
                for (int i = 0; i < count; i++)
                {
                    float angle = i / (float)count * Mathf.PI * 2f;
                    _dots.Add(new Dot
                    {
                        X = Mathf.Cos(angle) * radius,
                        Y = Mathf.Sin(angle) * radius,
                        Ring = ring,
                        Dist = radius,
                        Angle = angle,
                    });
                }
            }
        }

        void GenerateHexDots(int ringLayersLocal, float spacing)
        {
            _dots.Add(new Dot());
            for (int ring = 1; ring <= ringLayersLocal; ring++)
            {
                int dotsInRing = ring * 6;
                for (int i = 0; i < dotsInRing; i++)
                {
                    int side = i / ring;
                    int posOnSide = i % ring;

                    float angle1 = side * 60f * Mathf.Deg2Rad;
                    float angle2 = (side + 1) * 60f * Mathf.Deg2Rad;

                    float p1x = ring * spacing * Mathf.Cos(angle1);
                    float p1y = ring * spacing * Mathf.Sin(angle1);
                    float p2x = ring * spacing * Mathf.Cos(angle2);
                    float p2y = ring * spacing * Mathf.Sin(angle2);

                    float t = posOnSide / (float)ring;
                    float x = p1x + (p2x - p1x) * t;
                    float y = p1y + (p2y - p1y) * t;
                    float dist = Mathf.Sqrt(x * x + y * y);
                    float angle = Mathf.Atan2(y, x);
                    _dots.Add(new Dot { X = x, Y = y, Ring = ring, Dist = dist, Angle = angle });
                }
            }
        }

        void GeneratePhyllotaxisDots(float coverRadiusUnits, float spacing)
        {
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            int maxIndex = Mathf.CeilToInt(Mathf.Pow(Mathf.Max(1f, coverRadiusUnits / 0.4f), 2f)) + 8;
            maxIndex = Mathf.Clamp(maxIndex, 32, 8000);

            for (int i = 0; i < maxIndex; i++)
            {
                float radius = spacing * 0.4f * Mathf.Sqrt(i);
                if (radius > coverRadiusUnits)
                    break;

                float angle = i * goldenAngle;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _dots.Add(new Dot
                {
                    X = x,
                    Y = y,
                    Ring = Mathf.FloorToInt(radius / spacing),
                    Dist = radius,
                    Angle = angle,
                });
            }
        }

        void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }
    }
}
