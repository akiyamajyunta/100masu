using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// Rainbow block equalizer background (bars grow from the bottom). Same setup as GameOfLifeBackground:
    /// full-screen RawImage, raycast off. No audio — procedural loop.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class EqualizerBackground : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] int bars = 32;
        [SerializeField] int segments = 26;
        [SerializeField] int barGapPixels = 3;
        [SerializeField] int segmentGapPixels = 2;
        [SerializeField] float marginFraction = 0.05f;

        [Header("Motion")]
        [SerializeField] float loopPeriodSeconds = 8f;

        [Header("Appearance")]
        [SerializeField] Color backgroundColor = new Color32(0x0a, 0x10, 0x22, 0xff);
        [SerializeField] float glowStrength = 0.35f;
        [SerializeField] float highlightStrength = 1.15f;

        [Header("Performance")]
        [SerializeField] int maxRenderWidth = 960;
        [SerializeField] int framesPerRender = 1;

        [Header("Control")]
        [SerializeField] bool paused;

        static readonly (float t, Color32 c)[] RainbowStops =
        {
            (0f, new Color32(32, 96, 255, 255)),
            (0.22f, new Color32(32, 224, 128, 255)),
            (0.45f, new Color32(255, 224, 32, 255)),
            (0.68f, new Color32(255, 128, 32, 255)),
            (1f, new Color32(255, 64, 160, 255)),
        };

        RawImage _rawImage;
        Texture2D _texture;
        Color32[] _pixels;
        int _texW;
        int _texH;
        float _elapsed;
        int _frame;
        Color32 _bg;

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
            ResolveTargetSize(out var w, out var h);
            if (_texture != null && w == _texW && h == _texH)
                return;
            RebuildTexture();
            if (_texture != null && _pixels != null)
                PushTexture();
        }

        void Update()
        {
            if (_texture == null || _pixels == null) return;

            if (!paused)
                _elapsed += Time.deltaTime;

            _frame++;
            if (_frame % Mathf.Max(1, framesPerRender) != 0) return;

            Render();
            PushTexture();
        }

        /// <summary>プレイ画面表示時にテーママネージャから呼ぶ。</summary>
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
            var rect = rt.rect;
            w = Mathf.RoundToInt(rect.width);
            h = Mathf.RoundToInt(rect.height);

            if (w < 2 || h < 2)
            {
                w = Mathf.RoundToInt(Mathf.Abs(rt.rect.width));
                h = Mathf.RoundToInt(Mathf.Abs(rt.rect.height));
            }

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

        public void RebuildTexture()
        {
            bars = Mathf.Max(1, bars);
            segments = Mathf.Max(2, segments);

            ResolveTargetSize(out var w, out var h);

            if (w > maxRenderWidth)
            {
                float aspect = (float)h / w;
                w = maxRenderWidth;
                h = Mathf.Max(64, Mathf.RoundToInt(w * aspect));
            }

            _texW = w;
            _texH = h;
            _pixels = new Color32[_texW * _texH];
            _bg = (Color32)backgroundColor;

            if (_texture != null)
                Destroy(_texture);

            _texture = new Texture2D(_texW, _texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _rawImage.texture = _texture;
            Render();
            PushTexture();
        }

        void Render()
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = _bg;

            float phase = loopPeriodSeconds > 0.001f
                ? _elapsed / loopPeriodSeconds * Mathf.PI * 2f
                : 0f;

            int padX = Mathf.Max(8, Mathf.RoundToInt(_texW * marginFraction));
            int padY = Mathf.Max(10, Mathf.RoundToInt(_texH * marginFraction));
            int innerW = _texW - padX * 2;
            int innerH = _texH - padY * 2;
            int barW = Mathf.Max(4, (innerW - barGapPixels * (bars - 1)) / bars);
            int segH = Mathf.Max(3, (innerH - segmentGapPixels * (segments - 1)) / segments);
            for (int b = 0; b < bars; b++)
            {
                int bx = padX + b * (barW + barGapPixels);
                for (int s = 0; s < segments; s++)
                {
                    if (!SegmentOn(b, s, phase, segments))
                        continue;

                    float tColor = s / (float)(segments - 1);
                    Color32 core = RainbowAt(tColor);
                    int by = ComputeSegmentY(padY, s, segH, segmentGapPixels);
                    DrawBlock(bx, by, barW, segH, core);
                }
            }
        }

        static float BarLevel(int bar, float phase, int segmentCount)
        {
            float w1 = 0.5f + 0.5f * Mathf.Sin(phase * 1f + bar * 0.71f);
            float w2 = 0.5f + 0.5f * Mathf.Sin(phase * 2f + bar * 1.31f);
            float w3 = 0.5f + 0.5f * Mathf.Sin(phase * 0.5f + bar * 1.97f);
            float mix = w1 * 0.5f + w2 * 0.32f + w3 * 0.18f;
            float wobble = 0.04f * Mathf.Sin(phase * 3f + bar * 2.17f)
                           + 0.03f * Mathf.Sin(phase * 5f + bar * 0.83f);
            return segmentCount * Mathf.Clamp(mix + wobble, 0.08f, 1f);
        }

        static bool SegmentOn(int bar, int segFromBottom, float phase, int segmentCount)
        {
            return segFromBottom + 0.5f < BarLevel(bar, phase, segmentCount);
        }

        /// <summary>Texture2D は左下原点。seg 0 を内側下端に置き、上へ積む。</summary>
        static int ComputeSegmentY(int padY, int segFromBottom, int segH, int segGap)
        {
            return padY + segFromBottom * (segH + segGap);
        }

        static Color32 RainbowAt(float t)
        {
            t = Mathf.Clamp01(t);
            for (int i = 0; i < RainbowStops.Length - 1; i++)
            {
                var a = RainbowStops[i];
                var b = RainbowStops[i + 1];
                if (t >= a.t && t <= b.t)
                {
                    float u = (t - a.t) / (b.t - a.t);
                    return LerpColor(a.c, b.c, u);
                }
            }

            return RainbowStops[RainbowStops.Length - 1].c;
        }

        void DrawBlock(int x, int y, int w, int h, Color32 core)
        {
            Color32 glow = ScaleColor(core, glowStrength);
            FillRect(x - 1, y - 1, w + 2, h + 2, glow);
            FillRect(x, y, w, h, core);
            int strip = Mathf.Max(1, Mathf.RoundToInt(h * 0.35f));
            Color32 hi = ScaleColor(core, highlightStrength);
            FillRect(x, y + h - strip, w, strip, hi);
        }

        static Color32 ScaleColor(Color32 c, float mul)
        {
            return new Color32(
                (byte)Mathf.Min(255, Mathf.RoundToInt(c.r * mul)),
                (byte)Mathf.Min(255, Mathf.RoundToInt(c.g * mul)),
                (byte)Mathf.Min(255, Mathf.RoundToInt(c.b * mul)),
                255);
        }

        static Color32 LerpColor(Color32 a, Color32 b, float t)
        {
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

        void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }
    }
}
