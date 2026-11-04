using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// Game &amp; Watch 風 LCD 花火（横スクロール星ドット ＋ 放射線点滅花火）。HTML シミュレータ相当。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class LcdFireworksBackground : MonoBehaviour
    {
        struct Star
        {
            public float X;
            public float Y;
            public float SpeedMult;
            public float Opacity;
        }

        sealed class LineBurst
        {
            public int X;
            public int Y;
            public int PixelSize;
            public int RayCount;
            public float MaxLength;
            public int BlinkSpeed;
            public int MaxDuration;
            public int Life;
        }

        const float ReferenceMinSize = 720f;

        [Header("Stars (classic preset)")]
        [SerializeField] float scrollSpeed = 1.5f;
        [SerializeField] int starCount = 40;
        [SerializeField] float starSizeRef = 3f;

        [Header("Fireworks")]
        [SerializeField] float spawnIntervalSeconds = 0.65f;
        [SerializeField] int burstHeightMinPercent = 20;
        [SerializeField] int burstHeightMaxPercent = 80;
        [SerializeField] int rayCount = 16;
        [SerializeField] float lineLengthRef = 80f;
        [SerializeField] int blinkRate = 5;
        [SerializeField] int lineDurationFrames = 45;
        [SerializeField] float fireworkPixelSizeRef = 6f;

        [Header("Display")]
        [SerializeField] bool scanlines = true;
        [SerializeField] Color32 backgroundColor = new Color32(0, 0, 0, 255);

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
        float _layoutScale = 1f;
        int _starPixelSize;
        int _burstPixelSize;
        float _lineLengthPx;
        int _frame;
        float _spawnTimer;
        readonly List<Star> _stars = new List<Star>();
        readonly List<LineBurst> _bursts = new List<LineBurst>();

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
            ResolveTargetSize(out int w, out int h);
            if (_texture != null && w == _texW && h == _texH)
                return;
            RebuildTexture();
        }

        void Update()
        {
            if (_texture == null || _pixels == null)
                return;

            if (!paused)
                Simulate(Time.deltaTime);

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

        void Simulate(float dt)
        {
            UpdateStars(dt);

            _spawnTimer += dt;
            if (_spawnTimer >= spawnIntervalSeconds)
            {
                SpawnFirework();
                _spawnTimer = 0f;
            }

            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                _bursts[i].Life++;
                if (_bursts[i].Life >= _bursts[i].MaxDuration)
                    _bursts.RemoveAt(i);
            }
        }

        void UpdateStars(float dt)
        {
            float baseSpeed = scrollSpeed * 60f * dt * _layoutScale;
            for (int i = 0; i < _stars.Count; i++)
            {
                var s = _stars[i];
                s.X -= baseSpeed * s.SpeedMult;
                if (s.X < -10f * _layoutScale)
                {
                    s.X = _texW + Random.Range(0f, 10f * _layoutScale);
                    s.Y = Random.Range(0f, _texH);
                }

                _stars[i] = s;
            }
        }

        void SpawnFirework(float? customX = null, float? customYTop = null)
        {
            int ps = _burstPixelSize;
            float x = customX ?? Random.Range(60f * _layoutScale, _texW - 60f * _layoutScale);
            float yTop = customYTop ?? ComputeRandomBurstYTop();

            int gridX = Mathf.FloorToInt(x / ps) * ps;
            int gridYTop = Mathf.FloorToInt(yTop / ps) * ps;

            _bursts.Add(new LineBurst
            {
                X = gridX,
                Y = gridYTop,
                PixelSize = ps,
                RayCount = rayCount,
                MaxLength = _lineLengthPx,
                BlinkSpeed = blinkRate,
                MaxDuration = lineDurationFrames,
                Life = 0,
            });
        }

        float ComputeRandomBurstYTop()
        {
            int minP = Mathf.Min(burstHeightMinPercent, burstHeightMaxPercent - 5);
            int maxP = Mathf.Max(burstHeightMaxPercent, minP + 5);
            float factor = minP / 100f + Random.value * ((maxP - minP) / 100f);
            return _texH * (1f - factor);
        }

        void InitStars()
        {
            _stars.Clear();
            int count = Mathf.Max(10, starCount);
            for (int i = 0; i < count; i++)
            {
                _stars.Add(new Star
                {
                    X = Random.Range(0f, _texW),
                    Y = Random.Range(0f, _texH),
                    SpeedMult = Random.Range(0.5f, 1.5f),
                    Opacity = Random.Range(0.3f, 1f),
                });
            }
        }

        void RecomputeLayoutMetrics()
        {
            float minDim = Mathf.Min(_texW, _texH);
            _layoutScale = Mathf.Max(0.35f, minDim / ReferenceMinSize);
            _starPixelSize = Mathf.Max(2, Mathf.RoundToInt(starSizeRef * _layoutScale));
            _burstPixelSize = Mathf.Max(2, Mathf.RoundToInt(fireworkPixelSizeRef * _layoutScale));
            _lineLengthPx = lineLengthRef * _layoutScale;
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
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _rawImage.texture = _texture;

            RecomputeLayoutMetrics();
            InitStars();
            _bursts.Clear();
            _spawnTimer = 0f;
            Render();
            PushTexture();
        }

        void Render()
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = backgroundColor;

            DrawStars();

            foreach (var burst in _bursts)
                DrawBurst(burst);

            if (scanlines)
                ApplyScanlines();
        }

        void DrawStars()
        {
            int size = _starPixelSize;
            foreach (var star in _stars)
            {
                int gridX = Mathf.FloorToInt(star.X / size) * size;
                int gridYTop = Mathf.FloorToInt(star.Y / size) * size;
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(star.Opacity * 255f), 0, 255);
                FillRectTopLeft(gridX, gridYTop, size, size, new Color32(255, 255, 255, a));
            }
        }

        void DrawBurst(LineBurst burst)
        {
            float lifeProgress = burst.Life / (float)burst.MaxDuration;
            float baseAlpha = Mathf.Max(0f, 1f - lifeProgress);
            if (baseAlpha <= 0f)
                return;

            byte alphaByte = (byte)Mathf.Clamp(Mathf.RoundToInt(baseAlpha * 255f), 0, 255);
            var color = new Color32(255, 255, 255, alphaByte);
            float blinkThreshold = 0.12f + burst.BlinkSpeed * 0.07f;

            int ps = burst.PixelSize;
            for (int i = 0; i < burst.RayCount; i++)
            {
                float angle = i / (float)burst.RayCount * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                for (float r = 0f; r <= burst.MaxLength; r += ps)
                {
                    if (Random.value < blinkThreshold)
                        continue;

                    float dotX = burst.X + cos * r;
                    float dotYTop = burst.Y + sin * r;
                    int gridX = Mathf.FloorToInt(dotX / ps) * ps;
                    int gridYTop = Mathf.FloorToInt(dotYTop / ps) * ps;
                    FillRectTopLeft(gridX, gridYTop, ps, ps, color);
                }
            }
        }

        void ApplyScanlines()
        {
            for (int y = 0; y < _texH; y++)
            {
                int yFromTop = _texH - 1 - y;
                if (yFromTop % 4 >= 2)
                {
                    int row = y * _texW;
                    for (int x = 0; x < _texW; x++)
                    {
                        int idx = row + x;
                        var p = _pixels[idx];
                        _pixels[idx] = new Color32(
                            (byte)(p.r * 0.65f),
                            (byte)(p.g * 0.65f),
                            (byte)(p.b * 0.65f),
                            255);
                    }
                }
            }
        }

        void FillRectTopLeft(int x, int yTop, int w, int h, Color32 color)
        {
            int yBottom = _texH - yTop - h;
            FillRect(x, yBottom, w, h, color);
        }

        void FillRect(int x, int y, int w, int h, Color32 color)
        {
            int x0 = Mathf.Max(0, x);
            int y0 = Mathf.Max(0, y);
            int x1 = Mathf.Min(_texW, x + w);
            int y1 = Mathf.Min(_texH, y + h);
            for (int py = y0; py < y1; py++)
            {
                int row = py * _texW;
                for (int px = x0; px < x1; px++)
                    _pixels[row + px] = color;
            }
        }

        void PushTexture()
        {
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        void ResolveTargetSize(out int w, out int h)
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

            if (w > maxRenderWidth)
            {
                float aspect = (float)h / w;
                w = maxRenderWidth;
                h = Mathf.Max(64, Mathf.RoundToInt(w * aspect));
            }
        }

        void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }
    }
}
