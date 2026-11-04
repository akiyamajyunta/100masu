using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 赤い波打ちグリッド ＋ 暗色レインボー方形パルス（game-watch-red-grid-pulse-background.js 相当）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class RedGridPulseBackground : MonoBehaviour
    {
        sealed class RainbowPulse
        {
            public float X;
            public float Y;
            public float CurrentRadius;
            public float MaxRadius;
            public float Speed;
            public float Thickness;
            public float FadeProgress;
            public bool Alive = true;
        }

        const float ReferenceMinSize = 720f;

        [Header("Grid")]
        [SerializeField] float gridSpacingRef = 22f;
        [SerializeField] int gridLineWidthRef = 2;
        [SerializeField] float gridOpacity = 0.45f;
        [SerializeField] float gridWaveAmpRef = 7f;
        [SerializeField] float gridWaveSpeed = 1f;

        [Header("Pulses")]
        [SerializeField] float pulseIntervalSeconds = 0.8f;
        [SerializeField] int maxPulses = 12;
        [SerializeField] float pulseMinRadiusRef = 25f;
        [SerializeField] float pulseRadiusRef = 70f;
        [SerializeField] float pulseSpeedRef = 80f;
        [SerializeField] float pulseFadeTime = 0.3f;

        [Header("Display")]
        [SerializeField] bool scanlines = true;
        [SerializeField] Color32 backgroundColor = new Color32(0, 0, 0, 255);
        static readonly Color32 GridRed = new Color32(239, 68, 68, 255);

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
        float _gridSpacing;
        int _gridLineWidth;
        float _gridWaveAmp;
        float _pulseMinRadius;
        float _pulseRadius;
        float _pulseSpeed;
        float _gridWaveTime;
        float _pulseTimer;
        float _nextSpawnTime = 0.5f;
        int _frame;
        readonly List<RainbowPulse> _pulses = new List<RainbowPulse>();

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
            for (int i = 0; i < 4; i++)
                _pulses.Add(SpawnPulse(null, null));
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
            if (_pulses.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                    _pulses.Add(SpawnPulse(null, null));
            }
        }

        public void StopBackground()
        {
            paused = true;
        }

        void RecomputeLayoutMetrics()
        {
            float minDim = Mathf.Min(_texW, _texH);
            _layoutScale = Mathf.Max(0.35f, minDim / ReferenceMinSize);
            _gridSpacing = gridSpacingRef * _layoutScale;
            _gridLineWidth = Mathf.Max(3, Mathf.RoundToInt(gridLineWidthRef * _layoutScale));
            _gridWaveAmp = gridWaveAmpRef * _layoutScale;
            _pulseMinRadius = pulseMinRadiusRef * _layoutScale;
            _pulseRadius = pulseRadiusRef * _layoutScale;
            _pulseSpeed = pulseSpeedRef * _layoutScale;
        }

        void Simulate(float dt)
        {
            dt = Mathf.Min(dt, 0.1f);
            _gridWaveTime += dt;

            _pulseTimer += dt;
            if (_pulseTimer >= _nextSpawnTime)
            {
                if (_pulses.Count < maxPulses)
                    _pulses.Add(SpawnPulse(null, null));
                _pulseTimer = 0f;
                _nextSpawnTime = pulseIntervalSeconds * (0.5f + Random.value);
            }

            for (int i = _pulses.Count - 1; i >= 0; i--)
            {
                var p = _pulses[i];
                p.MaxRadius = _pulseRadius;
                if (p.CurrentRadius < p.MaxRadius)
                    p.CurrentRadius += _pulseSpeed * dt;
                else
                {
                    p.FadeProgress += dt / Mathf.Max(0.1f, pulseFadeTime);
                    if (p.FadeProgress >= 1f)
                        p.Alive = false;
                }

                if (!p.Alive)
                    _pulses.RemoveAt(i);
            }
        }

        RainbowPulse SpawnPulse(float? x, float? y)
        {
            float spacing = _gridSpacing;
            int halfGrid = Mathf.FloorToInt(spacing * 0.5f);

            float px;
            if (x.HasValue)
                px = x.Value;
            else
            {
                int cols = Mathf.Max(1, Mathf.FloorToInt((_texW - halfGrid) / spacing));
                int col = Random.Range(0, cols);
                px = halfGrid + col * spacing;
            }

            float pyTop;
            if (y.HasValue)
                pyTop = y.Value;
            else
            {
                int rows = Mathf.Max(1, Mathf.FloorToInt((_texH - halfGrid) / spacing));
                int row = Random.Range(0, rows);
                pyTop = halfGrid + row * spacing;
            }

            float maxR = Mathf.Max(_pulseMinRadius + 10f * _layoutScale, _pulseRadius);
            return new RainbowPulse
            {
                X = px,
                Y = pyTop,
                CurrentRadius = _pulseMinRadius,
                MaxRadius = maxR,
                Speed = _pulseSpeed,
                Thickness = Mathf.Clamp(maxR * 0.34f, 12f * _layoutScale, 26f * _layoutScale),
            };
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
            Render();
            PushTexture();
        }

        void Render()
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = backgroundColor;

            DrawWavyGrid(_gridWaveTime);
            DrawRainbowPulses();

            if (scanlines)
                ApplyScanlines();
        }

        void DrawWavyGrid(float t)
        {
            float spacing = _gridSpacing;
            if (spacing < 4f)
                return;

            int half = Mathf.FloorToInt(spacing * 0.5f);
            var gridColor = MakeGridLineColor();

            int verticalLines = Mathf.CeilToInt((_texW - half) / spacing) + 2;
            for (int li = 0; li <= verticalLines; li++)
            {
                float x = half + li * spacing;
                float prevX = WarpGridX(x, 0f, t);
                for (int y = 1; y <= _texH; y++)
                {
                    float wx = WarpGridX(x, y, t);
                    DrawGridLineTopLeft(prevX, y - 1, wx, y, gridColor, _gridLineWidth);
                    prevX = wx;
                }
            }

            int horizontalLines = Mathf.CeilToInt((_texH - half) / spacing) + 2;
            for (int li = 0; li <= horizontalLines; li++)
            {
                float yLine = half + li * spacing;
                float prevWy = WarpGridY(0f, yLine, t);
                for (int x = 1; x <= _texW; x++)
                {
                    float wy = WarpGridY(x, yLine, t);
                    DrawGridLineTopLeft(x - 1, prevWy, x, wy, gridColor, _gridLineWidth);
                    prevWy = wy;
                }
            }
        }

        Color32 MakeGridLineColor()
        {
            float o = Mathf.Clamp01(gridOpacity);
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(GridRed.r * o), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(GridRed.g * o), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(GridRed.b * o), 0, 255),
                255);
        }

        void BlendGridPixel(int x, int y, Color32 gridColor)
        {
            if (x < 0 || x >= _texW || y < 0 || y >= _texH)
                return;
            int idx = y * _texW + x;
            var dst = _pixels[idx];
            if (dst.r >= gridColor.r && dst.g >= gridColor.g && dst.b >= gridColor.b)
                return;
            _pixels[idx] = new Color32(
                (byte)Mathf.Max(dst.r, gridColor.r),
                (byte)Mathf.Max(dst.g, gridColor.g),
                (byte)Mathf.Max(dst.b, gridColor.b),
                255);
        }

        void DrawRainbowPulses()
        {
            if (_pulses.Count == 0)
                return;

            float spacing = _gridSpacing;
            int halfGrid = Mathf.FloorToInt(spacing * 0.5f);
            int gridW = _gridLineWidth;

            foreach (var pulse in _pulses)
            {
                float r = pulse.CurrentRadius;
                float pulseFade = 1f - pulse.FadeProgress;
                if (pulseFade <= 0f)
                    continue;

                float innerR = Mathf.Max(0f, r - pulse.Thickness);
                float outerR = r + pulse.Thickness;

                for (float x = halfGrid; x <= _texW; x += spacing)
                {
                    for (float yTop = halfGrid; yTop <= _texH; yTop += spacing)
                    {
                        float wx = WarpGridX(x, yTop, _gridWaveTime);
                        float wy = WarpGridY(x, yTop, _gridWaveTime);
                        float dx = wx - pulse.X;
                        float dy = wy - pulse.Y;
                        float dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

                        if (dist < innerR || dist > outerR)
                            continue;

                        float normDist = Mathf.Abs(dist - r) / pulse.Thickness;
                        float alpha = (1f - normDist) * pulseFade;
                        if (alpha <= 0.02f)
                            continue;

                        float angleDeg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (angleDeg < 0f)
                            angleDeg += 360f;
                        float lightness = 20f + Mathf.Sin(angleDeg * Mathf.Deg2Rad) * 15f;
                        HslToColor32(angleDeg, 90f, lightness, alpha * 0.95f, out var color);

                        int dotSize = Mathf.Max(gridW + 1, 3);
                        FillRectTopLeft(
                            Mathf.RoundToInt(wx - dotSize * 0.5f),
                            Mathf.RoundToInt(wy - dotSize * 0.5f),
                            dotSize,
                            dotSize,
                            color);

                        DrawLineTopLeft(wx - halfGrid, wy, wx + halfGrid, wy, color, gridW + 1);
                        DrawLineTopLeft(wx, wy - halfGrid, wx, wy + halfGrid, color, gridW + 1);
                    }
                }
            }
        }

        float WarpGridX(float x, float y, float t)
        {
            if (_gridWaveAmp <= 0f)
                return x;
            return x + _gridWaveAmp * Mathf.Sin(y * 0.014f + t * gridWaveSpeed);
        }

        float WarpGridY(float x, float y, float t)
        {
            if (_gridWaveAmp <= 0f)
                return y;
            return y + _gridWaveAmp * Mathf.Sin(x * 0.014f * 0.88f - t * gridWaveSpeed * 0.95f);
        }

        static void HslToColor32(float h, float sPercent, float lPercent, float a, out Color32 c)
        {
            float s = Mathf.Clamp01(sPercent / 100f);
            float l = Mathf.Clamp01(lPercent / 100f);
            h = (h % 360f + 360f) % 360f;
            float c1 = (1f - Mathf.Abs(2f * l - 1f)) * s;
            float x = c1 * (1f - Mathf.Abs((h / 60f) % 2f - 1f));
            float m = l - c1 * 0.5f;
            float r1, g1, b1;
            if (h < 60f) { r1 = c1; g1 = x; b1 = 0; }
            else if (h < 120f) { r1 = x; g1 = c1; b1 = 0; }
            else if (h < 180f) { r1 = 0; g1 = c1; b1 = x; }
            else if (h < 240f) { r1 = 0; g1 = x; b1 = c1; }
            else if (h < 300f) { r1 = x; g1 = 0; b1 = c1; }
            else { r1 = c1; g1 = 0; b1 = x; }

            byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255);
            c = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt((r1 + m) * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt((g1 + m) * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt((b1 + m) * 255f), 0, 255),
                alpha);
        }

        void DrawGridLineTopLeft(float x0, float y0Top, float x1, float y1Top, Color32 gridColor, int width)
        {
            BresenhamTopLeft(x0, y0Top, x1, y1Top, width, (xTop, yTop) => StampGridTopLeft(xTop, yTop, gridColor));
        }

        void DrawLineTopLeft(float x0, float y0Top, float x1, float y1Top, Color32 color, int width)
        {
            BresenhamTopLeft(x0, y0Top, x1, y1Top, width, (xTop, yTop) =>
            {
                FillRectTopLeft(xTop - width / 2, yTop - width / 2, width, width, color);
            });
        }

        void BresenhamTopLeft(float x0, float y0Top, float x1, float y1Top, int width, System.Action<int, int> stamp)
        {
            int xStart = Mathf.RoundToInt(x0);
            int yStart = Mathf.RoundToInt(y0Top);
            int xEnd = Mathf.RoundToInt(x1);
            int yEnd = Mathf.RoundToInt(y1Top);

            int dx = Mathf.Abs(xEnd - xStart);
            int dy = Mathf.Abs(yEnd - yStart);
            int sx = xStart < xEnd ? 1 : -1;
            int sy = yStart < yEnd ? 1 : -1;
            int err = dx - dy;
            int x = xStart;
            int y = yStart;
            int halfW = Mathf.Max(0, (width - 1) / 2);

            while (true)
            {
                for (int oy = -halfW; oy <= halfW; oy++)
                {
                    for (int ox = -halfW; ox <= halfW; ox++)
                        stamp(x + ox, y + oy);
                }

                if (x == xEnd && y == yEnd)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        void StampGridTopLeft(int xTop, int yTop, Color32 gridColor)
        {
            int yBottom = _texH - 1 - yTop;
            BlendGridPixel(xTop, yBottom, gridColor);
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
                            (byte)(p.r * 0.88f),
                            (byte)(p.g * 0.88f),
                            (byte)(p.b * 0.88f),
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
            float a = color.a / 255f;
            for (int py = y0; py < y1; py++)
            {
                int row = py * _texW;
                for (int px = x0; px < x1; px++)
                {
                    int idx = row + px;
                    _pixels[idx] = a >= 0.99f ? color : BlendOver(_pixels[idx], color, a);
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
