using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// Hourglass torus / tornado optical illusion — coarse monochrome stripes, continuous spin.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class TunnelIllusionBackground : MonoBehaviour
    {
        [Header("Tornado motion")]
        [SerializeField] float spinRadiansPerSecond = 0.52f;
        [SerializeField] float verticalFlow = 0.38f;
        [SerializeField] float rippleStrength = 0.05f;
        [SerializeField] float rippleSpeed = 0.95f;

        [Header("Hourglass shape")]
        [SerializeField] float waistPinch = 0.26f;
        [SerializeField] float waistCurve = 1.9f;
        [SerializeField] float twistAlongHeight = 0.72f;

        [Header("Stripes")]
        [Tooltip("Larger = thicker, coarser stripes.")]
        [SerializeField] float stripeCoarseness = 3.2f;
        [SerializeField] float spiralBands = 11f;
        [SerializeField] float depthBands = 5.5f;
        [SerializeField] float ringBands = 18f;
        [SerializeField] float spiralTurns = 2.4f;
        [SerializeField] float holeRadius = 0.06f;
        [SerializeField] float tunnelDepth = 4.2f;

        [Header("Appearance")]
        [SerializeField] Color colorBlack = new Color32(0x2a, 0x2a, 0x34, 0xff);
        [SerializeField] Color colorWhite = new Color32(0xb4, 0xb4, 0xbe, 0xff);

        [Header("Performance")]
        [SerializeField] int maxRenderWidth = 1600;
        [SerializeField] int framesPerRender = 1;

        [Header("Control")]
        [SerializeField] bool paused;

        static readonly float TwoPi = Mathf.PI * 2f;

        RawImage _rawImage;
        Texture2D _texture;
        Color32[] _pixels;
        int _texW;
        int _texH;
        float _elapsed;
        int _frame;
        Color32 _black;
        Color32 _white;

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
            if (_texture == null || _pixels == null) return;

            if (!paused)
                _elapsed += Time.deltaTime;

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
            _black = (Color32)colorBlack;
            _white = (Color32)colorWhite;

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
            float spin = _elapsed * spinRadiansPerSecond;
            float phase = _elapsed * rippleSpeed;
            float coarse = 1f / Mathf.Max(0.35f, stripeCoarseness);
            float spiralFreq = spiralBands * coarse;
            float depthFreq = depthBands * coarse;
            float ringFreq = ringBands * coarse;

            float aspect = (float)_texH / _texW;
            float invW = 1f / _texW;
            float invH = 1f / _texH;

            for (int py = 0; py < _texH; py++)
            {
                float fy = (py + 0.5f) * invH - 0.5f;
                fy *= aspect;
                int row = py * _texW;
                for (int px = 0; px < _texW; px++)
                {
                    float fx = (px + 0.5f) * invW - 0.5f;

                    float waist = Mathf.Lerp(waistPinch, 1f, Mathf.Clamp01(Mathf.Abs(fy) * waistCurve));
                    float gx = fx / waist;

                    float rx = gx + rippleStrength * Mathf.Sin(fy * 11.5f + phase);
                    float ry = fy + rippleStrength * 0.4f * Mathf.Cos(gx * 9.2f - phase * 0.85f);

                    float r = Mathf.Sqrt(rx * rx + ry * ry);
                    float theta = Mathf.Atan2(ry, rx) + spin + twistAlongHeight * fy;

                    if (r < holeRadius * 0.5f)
                    {
                        _pixels[row + px] = _white;
                        continue;
                    }

                    float depth = tunnelDepth / (r + holeRadius) + verticalFlow * ry;
                    float spiral = spiralFreq * (theta / TwoPi * spiralTurns) + depth * depthFreq;
                    float rings = ringFreq * ry + ringFreq * 0.42f * r;

                    int layer = Mathf.FloorToInt(spiral) + Mathf.FloorToInt(rings);
                    _pixels[row + px] = (layer & 1) == 0 ? _white : _black;
                }
            }
        }

        void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }
    }
}
