using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 般若心経グリーンストリーム。全画面 RawImage、下→上に流れる緑の文字。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class HannyaShingyoGreenStreamBackground : MonoBehaviour
    {
        const string SutraText =
            "摩訶般若波羅蜜多心経観自在菩薩行深般若波羅蜜多時照見五蘊皆空度一切苦厄舎利子色不異空空不異色色即是空空即是色受想行識亦復如是舎利子是諸法空相不生不滅不垢不浄不増不減是故空中無色無受想行識無眼耳鼻舌身意無色声香味触法無眼界乃至無意識界無無明亦無無明尽乃至無老死亦無老死尽無苦集滅道無智亦無得以無所得故菩提薩埵依般若波羅蜜多故心無罣礙無罣礙故無有恐怖遠離一切顛倒夢想究竟涅槃三世諸仏依般若波羅蜜多故得阿耨多羅三藐三菩提故知般若波羅蜜多是大神呪是大明呪是無上呪是無等等呪能除一切苦真実不虚故説般若波羅蜜多呪即説呪曰羯諦羯諦波羅羯諦波羅僧羯諦菩提薩婆訶";

        [Header("Font")]
        [SerializeField] Font font;

        [Header("Layout")]
        [SerializeField] int fontSize = 25;
        [SerializeField] int cellHeight = 32;
        [SerializeField] int glyphBakeSize = 64;

        [Header("Motion")]
        [SerializeField] float minSpeed = 1.2f;
        [SerializeField] float maxSpeed = 3f;
        [SerializeField] int minTrailLength = 8;
        [SerializeField] int maxTrailLength = 24;
        [SerializeField] float charCycleMs = 160f;
        [SerializeField] float fadeOverlay = 0.2f;

        [Header("Performance")]
        [SerializeField] int maxRenderWidth = 1280;
        [SerializeField] int framesPerRender = 1;

        [Header("Control")]
        [SerializeField] bool paused;

        sealed class Column
        {
            public float x;
            public float y;
            public float speed;
            public int length;
            public int offset;
            public float phase;
            public int colW;
        }

        RawImage _rawImage;
        Texture2D _texture;
        Color32[] _pixels;
        int _texW;
        int _texH;
        int _frame;
        Column[] _columns;
        readonly Dictionary<char, Color32[]> _glyphs = new Dictionary<char, Color32[]>();
        bool _glyphsReady;

        GameObject _bakeRoot;
        Camera _bakeCam;
        Text _bakeText;
        RenderTexture _bakeRt;
        Texture2D _bakeRead;
        bool _ownsTexture;
        Coroutine _loadRoutine;

        static Texture2D s_solidBlack;

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
            if (font == null)
                font = MasuFonts.GetMinchoForSize(fontSize);
            EnsureBlackPlaceholder();
        }

        void OnEnable()
        {
            if (!_glyphsReady)
                EnsureBlackPlaceholder();
            BeginLoadIfNeeded();
        }

        void OnDisable()
        {
            if (_loadRoutine == null)
                return;
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        void Start()
        {
            BeginLoadIfNeeded();
        }

        void OnRectTransformDimensionsChange()
        {
            if (_rawImage == null || !_rawImage.isActiveAndEnabled || !_glyphsReady)
                return;
            ResolveTargetSize(out int w, out int h);
            if (_texture != null && w == _texW && h == _texH)
                return;
            RebuildTexture();
        }

        void Update()
        {
            if (_texture == null || _pixels == null || !_glyphsReady)
                return;

            if (!paused)
            {
                _frame++;
                if (_frame % Mathf.Max(1, framesPerRender) == 0)
                {
                    Render();
                    PushTexture();
                }
            }
        }

        public void PlayBackground()
        {
            paused = false;

            if (font == null)
                font = MasuFonts.GetMinchoForSize(fontSize);

            if (!_glyphsReady)
                EnsureBlackPlaceholder();

            Canvas.ForceUpdateCanvases();
            BeginLoadIfNeeded();
            if (_glyphsReady)
                RebuildTexture();
        }

        public void StopBackground()
        {
            paused = true;
        }

        void BeginLoadIfNeeded()
        {
            if (_glyphsReady || _loadRoutine != null)
                return;
            _loadRoutine = StartCoroutine(LoadGlyphsAndRebuild());
        }

        IEnumerator LoadGlyphsAndRebuild()
        {
            EnsureBlackPlaceholder();

            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            if (font == null)
            {
                Debug.LogWarning(
                    "[HannyaShingyoGreenStream] 日本語フォントを取得できませんでした。",
                    this);
                _loadRoutine = null;
                yield break;
            }

            CreateBakeRig();
            yield return BuildGlyphCache();
            RebuildTexture();
            _loadRoutine = null;
        }

        static Texture2D SolidBlackTexture()
        {
            if (s_solidBlack != null)
                return s_solidBlack;

            s_solidBlack = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            s_solidBlack.SetPixel(0, 0, Color.black);
            s_solidBlack.Apply(false);
            return s_solidBlack;
        }

        void EnsureBlackPlaceholder()
        {
            if (_rawImage == null || _glyphsReady)
                return;

            ReleaseOwnedTexture();
            _pixels = null;
            _texture = null;
            _rawImage.color = Color.white;
            _rawImage.material = null;
            _rawImage.texture = SolidBlackTexture();
        }

        void ReleaseOwnedTexture()
        {
            if (_texture == null || !_ownsTexture)
                return;
            Destroy(_texture);
            _texture = null;
            _ownsTexture = false;
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
        }

        public void RebuildTexture()
        {
            if (!_glyphsReady)
                return;

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

            ReleaseOwnedTexture();

            _texture = new Texture2D(_texW, _texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _rawImage.texture = _texture;
            _ownsTexture = true;

            InitColumns();
            ClearBlack();
            PushTexture();
        }

        void InitColumns()
        {
            int colW = Mathf.Max(31, Mathf.RoundToInt(fontSize * 1.45f));
            int count = Mathf.CeilToInt((float)_texW / colW) + 1;
            _columns = new Column[count];
            for (int i = 0; i < count; i++)
                _columns[i] = NewColumn(i * colW, colW, true);
        }

        Column NewColumn(float x, int colW, bool randomY)
        {
            return new Column
            {
                x = x,
                y = randomY ? Random.Range(0f, _texH) : _texH,
                speed = Random.Range(minSpeed, maxSpeed),
                length = Random.Range(minTrailLength, maxTrailLength + 1),
                offset = Random.Range(0, SutraText.Length),
                phase = Random.Range(0f, Mathf.PI * 2f),
                colW = colW
            };
        }

        void ClearBlack()
        {
            var black = new Color32(0, 0, 0, 255);
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = black;
        }

        void ApplyFade()
        {
            float keep = 1f - Mathf.Clamp01(fadeOverlay);
            for (int i = 0; i < _pixels.Length; i++)
            {
                var p = _pixels[i];
                _pixels[i] = new Color32(
                    (byte)(p.r * keep),
                    (byte)(p.g * keep),
                    (byte)(p.b * keep),
                    255);
            }
        }

        void Render()
        {
            ApplyFade();

            if (_columns == null || _columns.Length == 0)
                return;

            float timeMs = Time.time * 1000f;
            int timeSlot = Mathf.FloorToInt(timeMs / charCycleMs);

            for (int c = 0; c < _columns.Length; c++)
            {
                var col = _columns[c];
                for (int i = 0; i < col.length; i++)
                {
                    float y = col.y + i * cellHeight;
                    if (y < -cellHeight || y > _texH + cellHeight)
                        continue;

                    int idx = (col.offset + i + timeSlot) % SutraText.Length;
                    char ch = SutraText[idx];
                    bool head = i == 0;
                    float flicker = 0.68f + 0.32f * Mathf.Sin(timeMs * 0.003f + col.phase + i);
                    float alpha = Mathf.Max(0.16f, (1f - i / (float)col.length) * flicker);

                    Color32 tint;
                    if (head)
                        tint = new Color32(0xd5, 0xff, 0xdc, 255);
                    else
                    {
                        byte g = (byte)Mathf.Min(255, 175 + Mathf.FloorToInt(80f * alpha));
                        byte b = (byte)Mathf.Min(255, 35 + Mathf.FloorToInt(25f * alpha));
                        byte a = (byte)Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0.35f, alpha) * 255f), 0, 255);
                        tint = new Color32(0, g, b, a);
                    }

                    int cx = Mathf.RoundToInt(col.x + col.colW * 0.5f);
                    int cy = Mathf.RoundToInt(y);
                    StampGlyph(ch, cx, cy, tint, head);
                }

                col.y -= col.speed;
                if (col.y + col.length * cellHeight < 0f)
                {
                    var fresh = NewColumn(col.x, col.colW, false);
                    col.y = _texH + Random.Range(0f, _texH * 0.35f);
                    col.speed = fresh.speed;
                    col.length = fresh.length;
                    col.offset = fresh.offset;
                }
            }
        }

        void StampGlyph(char c, int cx, int cy, Color32 tint, bool head)
        {
            if (!_glyphs.TryGetValue(c, out var glyph))
                return;

            int gw = glyphBakeSize;
            int gh = glyphBakeSize;
            int ox = cx - gw / 2;
            int oy = cy - gh / 2;

            for (int gy = 0; gy < gh; gy++)
            {
                int dy = oy + gy;
                if (dy < 0 || dy >= _texH)
                    continue;
                int row = dy * _texW;
                for (int gx = 0; gx < gw; gx++)
                {
                    int dx = ox + gx;
                    if (dx < 0 || dx >= _texW)
                        continue;

                    Color32 mask = glyph[gy * gw + gx];
                    float a = mask.a / 255f;
                    if (a < 0.03f)
                        continue;

                    float ta = tint.a / 255f * a;
                    if (head)
                        ta = Mathf.Min(1f, ta * 1.15f);

                    int idx = row + dx;
                    _pixels[idx] = BlendOver(_pixels[idx], tint, ta);
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

        void CreateBakeRig()
        {
            if (_bakeRoot != null)
                return;

            const int bakeLayer = 31;
            _bakeRoot = new GameObject("HannyaShingyoGlyphBake")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(_bakeRoot.transform, false);
            camGo.layer = bakeLayer;
            _bakeCam = camGo.AddComponent<Camera>();
            _bakeCam.orthographic = true;
            _bakeCam.orthographicSize = glyphBakeSize * 0.5f;
            _bakeCam.clearFlags = CameraClearFlags.SolidColor;
            _bakeCam.backgroundColor = new Color(0, 0, 0, 0);
            _bakeCam.cullingMask = 1 << bakeLayer;
            _bakeCam.nearClipPlane = 0.1f;
            _bakeCam.farClipPlane = 100f;
            _bakeCam.enabled = false;

            _bakeRt = new RenderTexture(glyphBakeSize, glyphBakeSize, 0, RenderTextureFormat.ARGB32);
            _bakeCam.targetTexture = _bakeRt;

            var canvasGo = new GameObject("Canvas");
            canvasGo.layer = bakeLayer;
            canvasGo.transform.SetParent(_bakeRoot.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _bakeCam;
            canvas.planeDistance = 10f;
            canvasGo.AddComponent<CanvasScaler>();

            var textGo = new GameObject("Text");
            textGo.layer = bakeLayer;
            textGo.transform.SetParent(canvasGo.transform, false);
            _bakeText = textGo.AddComponent<Text>();
            _bakeText.alignment = TextAnchor.MiddleCenter;
            _bakeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bakeText.verticalOverflow = VerticalWrapMode.Overflow;
            _bakeText.color = Color.white;
            var rt = _bakeText.rectTransform;
            rt.sizeDelta = new Vector2(glyphBakeSize, glyphBakeSize);
            rt.localPosition = Vector3.zero;

            camGo.transform.position = new Vector3(0, 0, -10f);

            _bakeRead = new Texture2D(glyphBakeSize, glyphBakeSize, TextureFormat.RGBA32, false);
        }

        IEnumerator BuildGlyphCache()
        {
            _glyphs.Clear();
            if (font == null)
            {
                _glyphsReady = false;
                yield break;
            }

            _bakeText.font = font;
            _bakeText.fontSize = fontSize;
            font.RequestCharactersInTexture(SutraText, fontSize, FontStyle.Normal);

            var unique = new HashSet<char>();
            foreach (char ch in SutraText)
                unique.Add(ch);

            foreach (char ch in unique)
            {
                _bakeText.text = ch.ToString();
                Canvas.ForceUpdateCanvases();
                yield return null;

                _bakeCam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = _bakeRt;
                _bakeRead.ReadPixels(new Rect(0, 0, glyphBakeSize, glyphBakeSize), 0, 0, false);
                _bakeRead.Apply(false);
                RenderTexture.active = prev;

                var copy = _bakeRead.GetPixels32();
                _glyphs[ch] = copy;
            }

            _glyphsReady = true;
        }

        void OnDestroy()
        {
            ReleaseOwnedTexture();
            if (_bakeRt != null)
                _bakeRt.Release();
            if (_bakeRead != null)
                Destroy(_bakeRead);
            if (_bakeRoot != null)
                Destroy(_bakeRoot);
        }
    }
}
