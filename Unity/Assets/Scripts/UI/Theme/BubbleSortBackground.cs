using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// Sorting visualizer grid (default 4×3): each panel runs a different algorithm (see SortPanelAlgorithms).
    /// RawImage background, SetPixels32 + Apply. Scrolls right → left (seamless loop).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class BubbleSortBackground : MonoBehaviour
    {
        [Header("Grid")]
        [Tooltip("4×3 = 12 panels, each with a different sort algorithm.")]
        [SerializeField] int gridColumns = 4;
        [SerializeField] int gridRows = 3;
        [SerializeField] int panelGapPixels = 6;

        [Header("Layout")]
        [SerializeField] int barsPerPanel = 16;
        [SerializeField] int barGapPixels = 2;
        [SerializeField] float marginFraction = 0.04f;
        [SerializeField] float panelMarginFraction = 0.04f;

        [Header("Timing")]
        [SerializeField] float stepInterval = 0.07f;
        [SerializeField] float swapFlashSeconds = 0.12f;
        [SerializeField] float scrollPixelsPerSecond = 22f;

        [Header("Appearance")]
        [SerializeField] Color backgroundColor = new Color32(0x0a, 0x10, 0x22, 0xff);
        [SerializeField] Color panelBackgroundColor = new Color32(0x0c, 0x14, 0x2a, 0xff);
        [SerializeField] Color barLowColor = new Color32(0x28, 0x58, 0xc8, 0xff);
        [SerializeField] Color barHighColor = new Color32(0x38, 0xe8, 0xc8, 0xff);
        [SerializeField] Color compareColor = new Color32(0xff, 0xc8, 0x28, 0xff);
        [SerializeField] Color swapColor = new Color32(0xff, 0x38, 0x38, 0xff);

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
        int _tileW;
        float _scrollPx;
        Color32 _bg;
        Color32 _panelBg;

        SortPanelState[] _panels;
        float[] _panelStepTimers;
        int _frame;

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
            if (_texture == null || _pixels == null || _panels == null) return;

            if (!paused)
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < _panels.Length; i++)
                {
                    if (_panels[i].SwapFlash > 0f)
                    {
                        var p = _panels[i];
                        p.SwapFlash = Mathf.Max(0f, p.SwapFlash - dt);
                        _panels[i] = p;
                    }

                    if (_panelStepTimers == null || i >= _panelStepTimers.Length)
                        continue;

                    _panelStepTimers[i] += dt;
                    while (_panelStepTimers[i] >= stepInterval)
                    {
                        _panelStepTimers[i] -= stepInterval;
                        SortPanelAlgorithms.Step(ref _panels[i], swapFlashSeconds);
                    }
                }

                int tileW = ComputeTileWidth();
                if (tileW > 0)
                {
                    _scrollPx += scrollPixelsPerSecond * Time.deltaTime;
                    _scrollPx %= tileW;
                }
            }

            _frame++;
            if (_frame % Mathf.Max(1, framesPerRender) != 0) return;

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

        void ResolveTargetSize(out int w, out int h)
        {
            var rt = _rawImage.rectTransform;
            w = Mathf.Max(64, Mathf.RoundToInt(rt.rect.width));
            h = Mathf.Max(64, Mathf.RoundToInt(rt.rect.height));
            if (w < 2 || h < 2)
            {
                w = Mathf.Max(64, Mathf.RoundToInt(Mathf.Abs(rt.sizeDelta.x)));
                h = Mathf.Max(64, Mathf.RoundToInt(Mathf.Abs(rt.sizeDelta.y)));
            }

            var canvas = _rawImage.canvas;
            if ((w < 2 || h < 2) && canvas != null)
            {
                var pr = canvas.rootCanvas.pixelRect;
                w = Mathf.Max(w, Mathf.RoundToInt(pr.width));
                h = Mathf.Max(h, Mathf.RoundToInt(pr.height));
            }
        }

        public void RebuildTexture()
        {
            gridColumns = Mathf.Max(1, gridColumns);
            gridRows = Mathf.Max(1, gridRows);
            barsPerPanel = Mathf.Max(4, barsPerPanel);

            ResolveTargetSize(out int w, out int h);
            if (w > maxRenderWidth)
            {
                float aspect = (float)h / w;
                w = maxRenderWidth;
                h = Mathf.Max(64, Mathf.RoundToInt(w * aspect));
            }

            _texW = w;
            _texH = h;
            _pixels = new Color32[_texW * _texH];
            _scrollPx = 0f;
            _tileW = 0;
            _bg = (Color32)backgroundColor;
            _panelBg = (Color32)panelBackgroundColor;

            if (_texture != null)
                Destroy(_texture);

            _texture = new Texture2D(_texW, _texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _rawImage.texture = _texture;

            InitPanels();
            Render();
            PushTexture();
        }

        void InitPanels()
        {
            int count = gridColumns * gridRows;
            _panels = new SortPanelState[count];
            _panelStepTimers = new float[count];
            float stagger = stepInterval / Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                var kind = SortPanelAlgorithms.KindForPanel(i);
                SortPanelAlgorithms.Reset(ref _panels[i], barsPerPanel, kind);
                _panelStepTimers[i] = i * stagger;
            }
        }

        void Render()
        {
            if (_pixels == null || _pixels.Length != _texW * _texH)
                return;

            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = _bg;

            int padY = Mathf.Max(6, Mathf.RoundToInt(_texH * marginFraction));
            int innerH = _texH - padY * 2;

            int totalGapX = panelGapPixels * (gridColumns - 1);
            int cellW = (_texW - totalGapX) / gridColumns;
            _tileW = ComputeTileWidth();
            if (_tileW < 1)
                return;

            int totalGapY = panelGapPixels * (gridRows - 1);
            int baseCellH = (innerH - totalGapY) / gridRows;
            int extraH = innerH - totalGapY - baseCellH * gridRows;

            Color32 low = (Color32)barLowColor;
            Color32 high = (Color32)barHighColor;
            Color32 cmp = (Color32)compareColor;
            Color32 swp = (Color32)swapColor;

            int scroll = Mathf.FloorToInt(_scrollPx) % _tileW;
            int tileCount = _tileW > 0 ? (_texW + _tileW - 1) / _tileW + 2 : 1;

            for (int tile = -1; tile < tileCount; tile++)
            {
                int baseX = tile * _tileW - scroll;
                int yTop = padY;
                for (int row = 0; row < gridRows; row++)
                {
                    int fromBottom = gridRows - 1 - row;
                    int cellH = baseCellH + (fromBottom < extraH ? 1 : 0);
                    for (int col = 0; col < gridColumns; col++)
                    {
                        int idx = row * gridColumns + col;
                        int ox = baseX + col * (cellW + panelGapPixels);
                        if (ox + cellW < 0 || ox >= _texW)
                            continue;
                        FillRectUi(ox, yTop, cellW, cellH, _panelBg);
                        RenderPanel(_panels[idx], ox, yTop, cellW, cellH, low, high, cmp, swp);
                    }

                    yTop += cellH + panelGapPixels;
                }
            }
        }

        void RenderPanel(
            SortPanelState panel,
            int cellX,
            int yTop,
            int cellW,
            int cellH,
            Color32 low,
            Color32 high,
            Color32 cmp,
            Color32 swp)
        {
            if (panel.Values == null || panel.Values.Length == 0)
                return;

            int n = panel.Values.Length;
            int maxVal = n;
            int m = Mathf.RoundToInt(Mathf.Min(cellW, cellH) * panelMarginFraction);
            m = Mathf.Clamp(m, 1, Mathf.Max(1, cellH / 5));
            int innerW = cellW - m * 2;
            int innerH = cellH - m * 2;
            if (innerW < 4 || innerH < 4)
                return;

            int barW = Mathf.Max(2, (innerW - barGapPixels * (n - 1)) / n);
            bool flashSwap = panel.SwapFlash > 0f;

            for (int i = 0; i < n; i++)
            {
                float t = (panel.Values[i] - 1f) / Mathf.Max(1, maxVal - 1);
                Color32 col = LerpColor(low, high, Mathf.Clamp01(t));
                if (flashSwap && (i == panel.CompareA || i == panel.CompareB))
                    col = swp;
                else if (i == panel.CompareA || i == panel.CompareB)
                    col = cmp;

                int barH = Mathf.Max(2, Mathf.RoundToInt(innerH * (panel.Values[i] / (float)maxVal)));
                int bx = cellX + m + i * (barW + barGapPixels);
                int barTop = yTop + cellH - m - barH;
                FillRectUi(bx, barTop, barW, barH, col);
            }
        }

        /// <summary>yTop = distance from top of texture (matches UI / RawImage layout).</summary>
        void FillRectUi(int x, int yTop, int w, int h, Color32 color)
        {
            int yBottom = _texH - yTop - h;
            FillRect(x, yBottom, w, h, color);
        }

        static Color32 LerpColor(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(a.r + (b.r - a.r) * t),
                (byte)Mathf.RoundToInt(a.g + (b.g - a.g) * t),
                (byte)Mathf.RoundToInt(a.b + (b.b - a.b) * t),
                255);
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

        int ComputeTileWidth()
        {
            if (_texW < 1 || gridColumns < 1)
                return 0;
            int totalGapX = panelGapPixels * (gridColumns - 1);
            int cellW = (_texW - totalGapX) / gridColumns;
            return gridColumns * cellW + totalGapX;
        }

        void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }
    }
}
