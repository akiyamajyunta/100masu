using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GifDecoder = MG.GIF.Decoder;
using GifFrame = MG.GIF.Image;

namespace Masu.UI
{
    /// <summary>Resources の GIF（.gif.bytes）を UI Image でループ再生。</summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class MasuUiGifPlayer : MonoBehaviour
    {
        const string DefaultResourcePath = "TitleArt/title_game_overlay.gif";
        const float DecodeErrorRetrySeconds = 0.2f;
        const int WatchdogRestartDelayFrames = 2;

        [SerializeField] string _resourcePath = DefaultResourcePath;

        UnityEngine.UI.Image _image;
        Coroutine _routine;
        Texture2D _texture;
        Sprite _sprite;
        Texture2D _firstTexture;
        Sprite _firstSprite;
        float _firstDelay = 0.1f;
        string _cachedForPath;
        byte[] _gifBytes;
        string _bytesPath;
        string _pendingResourcePath;
        bool _wantsPlayback;
        int _watchdogDelayFrames;

        public string ActiveResourcePath => _resourcePath;
        public bool IsPlaying => _routine != null;

        /// <summary>中央配置オーバーレイなどでスプライトのアスペクト比を保つ。</summary>
        public bool PreserveAspect { get; set; }

        void Awake()
        {
            _image = GetComponent<UnityEngine.UI.Image>();
            _image.raycastTarget = false;
        }

        void OnEnable()
        {
            if (!_wantsPlayback && string.IsNullOrEmpty(_pendingResourcePath))
                return;
            if (string.IsNullOrEmpty(_resourcePath) && string.IsNullOrEmpty(_pendingResourcePath))
                return;
            BeginPlayback(soft: _gifBytes != null);
        }

        void OnDisable()
        {
            StopCoroutineIfRunning();
        }

        void OnDestroy()
        {
            StopCoroutineIfRunning();
            DestroyDisplayedSprite();
            ClearFirstFrame();
        }

        void Update()
        {
            if (!_wantsPlayback || !isActiveAndEnabled || string.IsNullOrEmpty(_resourcePath)) return;
            if (_routine != null)
            {
                _watchdogDelayFrames = 0;
                return;
            }

            if (_gifBytes == null && !TryLoadGifBytes()) return;

            if (_watchdogDelayFrames < WatchdogRestartDelayFrames)
            {
                _watchdogDelayFrames++;
                return;
            }

            BeginPlayback(soft: true);
        }

        public void Play(string resourcesPath)
        {
            var pathChanged = resourcesPath != _resourcePath && resourcesPath != _pendingResourcePath;
            _wantsPlayback = true;
            _resourcePath = resourcesPath;
            _pendingResourcePath = resourcesPath;
            _watchdogDelayFrames = 0;
            if (!isActiveAndEnabled)
                return;
            BeginPlayback(soft: !pathChanged && _gifBytes != null);
        }

        public void StopPlayback()
        {
            _wantsPlayback = false;
            PreserveAspect = false;
            _pendingResourcePath = null;
            _watchdogDelayFrames = 0;
            StopCoroutineIfRunning();
            DestroyDisplayedSprite();
            ClearFirstFrame();
            if (_image != null)
                _image.sprite = null;
            _gifBytes = null;
            _bytesPath = null;
        }

        public void SetResourcePath(string resourcesPath) => Play(resourcesPath);

        void BeginPlayback(bool soft)
        {
            if (!string.IsNullOrEmpty(_pendingResourcePath))
            {
                _resourcePath = _pendingResourcePath;
                _pendingResourcePath = null;
            }

            StopCoroutineIfRunning();
            if (!soft || _cachedForPath != _resourcePath)
            {
                DestroyDisplayedSprite();
                ClearFirstFrame();
                if (_image != null) _image.sprite = null;
            }

            if (!TryLoadGifBytes())
                return;

            _routine = StartCoroutine(PlayLoop());
        }

        bool TryLoadGifBytes()
        {
            if (_gifBytes != null && _gifBytes.Length > 0 && _bytesPath == _resourcePath)
                return true;
            _gifBytes = null;
            _bytesPath = null;
            if (string.IsNullOrEmpty(_resourcePath))
                return false;

            var fileName = _resourcePath;
            const string prefix = "TitleArt/";
            if (fileName.StartsWith(prefix))
                fileName = fileName.Substring(prefix.Length);

            if (!MasuThemeResourceCache.TryGetGifAsset(fileName, out var asset) || asset == null)
            {
                Debug.LogWarning($"[MasuUiGifPlayer] GIF not found: {_resourcePath}");
                return false;
            }

            _gifBytes = asset.bytes;
            if (_gifBytes == null || _gifBytes.Length == 0)
                return false;
            _bytesPath = _resourcePath;
            return true;
        }

        IEnumerator PlayLoop()
        {
            try
            {
                if (_gifBytes == null || _gifBytes.Length == 0)
                    yield break;

                if (!HasFirstFrame())
                {
                    if (!TryCacheFirstFrame())
                    {
                        yield return new WaitForSecondsRealtime(DecodeErrorRetrySeconds);
                        yield break;
                    }
                }

                while (isActiveAndEnabled && _wantsPlayback)
                    yield return PlayCachedLoop();
            }
            finally
            {
                _routine = null;
            }
        }

        /// <summary>
        /// 先頭フレームはキャッシュを即表示し、次フレームのデコードは表示中に済ませる。
        /// ループ境界でデコーダを作り直してから描くと、その間だけ絵が止まる。
        /// </summary>
        IEnumerator PlayCachedLoop()
        {
            ShowSprite(_firstSprite);

            GifDecoder decoder = null;
            try
            {
                decoder = new GifDecoder(_gifBytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MasuUiGifPlayer] open failed ({_resourcePath}): {ex.Message}");
            }

            if (decoder == null)
            {
                yield return new WaitForSecondsRealtime(DecodeErrorRetrySeconds);
                yield break;
            }

            using (decoder)
            {
                var decodeStart = Time.realtimeSinceStartup;
                GifFrame upcoming = null;
                try
                {
                    decoder.NextImage();
                    upcoming = decoder.NextImage();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MasuUiGifPlayer] decode error ({_resourcePath}): {ex.Message}");
                }

                yield return WaitRemaining(_firstDelay, decodeStart);
                if (!isActiveAndEnabled || !_wantsPlayback) yield break;

                while (upcoming != null)
                {
                    if (!TryApplyFrame(upcoming))
                    {
                        yield return new WaitForSecondsRealtime(DecodeErrorRetrySeconds);
                        yield break;
                    }

                    var delay = FrameDelay(upcoming);
                    decodeStart = Time.realtimeSinceStartup;
                    try
                    {
                        upcoming = decoder.NextImage();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MasuUiGifPlayer] decode error ({_resourcePath}): {ex.Message}");
                        upcoming = null;
                    }

                    yield return WaitRemaining(delay, decodeStart);
                    if (!isActiveAndEnabled || !_wantsPlayback) yield break;
                }
            }
        }

        static IEnumerator WaitRemaining(float delaySeconds, float startedAt)
        {
            var left = delaySeconds - (Time.realtimeSinceStartup - startedAt);
            if (left > 0.0005f)
                yield return new WaitForSecondsRealtime(left);
        }

        bool HasFirstFrame() =>
            _firstSprite != null && _cachedForPath == _resourcePath;

        bool TryCacheFirstFrame()
        {
            try
            {
                using (var decoder = new GifDecoder(_gifBytes))
                {
                    var frame = decoder.NextImage();
                    if (frame == null) return false;
                    ClearFirstFrame();
                    _firstTexture = frame.CreateTexture();
                    _firstTexture.filterMode = FilterMode.Bilinear;
                    _firstTexture.wrapMode = TextureWrapMode.Clamp;
                    _firstSprite = Sprite.Create(
                        _firstTexture,
                        new Rect(0, 0, _firstTexture.width, _firstTexture.height),
                        new Vector2(0.5f, 0.5f),
                        MasuThemePlayfieldVisualLayout.OverlaySpritePixelsPerUnit);
                    _firstDelay = FrameDelay(frame);
                    _cachedForPath = _resourcePath;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MasuUiGifPlayer] first frame failed ({_resourcePath}): {ex.Message}");
                return false;
            }
        }

        static float FrameDelay(GifFrame frame) => Mathf.Max(0.02f, frame.Delay / 1000f);

        void ShowSprite(Sprite sprite)
        {
            if (_image == null || sprite == null) return;
            _image.sprite = sprite;
            _image.type = UnityEngine.UI.Image.Type.Simple;
            _image.preserveAspect = PreserveAspect;
            _image.color = Color.white;
            _image.enabled = true;
            if (_image.canvasRenderer != null)
                _image.canvasRenderer.cullTransparentMesh = false;
        }

        bool TryApplyFrame(GifFrame frame)
        {
            try
            {
                ApplyFrame(frame);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MasuUiGifPlayer] frame apply failed: {ex.Message}");
                return false;
            }
        }

        void ApplyFrame(GifFrame frame)
        {
            if (_texture == null || _texture.width != frame.Width || _texture.height != frame.Height)
            {
                if (_sprite != null)
                {
                    Destroy(_sprite);
                    _sprite = null;
                }

                if (_texture != null)
                    Destroy(_texture);

                _texture = new Texture2D(frame.Width, frame.Height, TextureFormat.ARGB32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            _texture.SetPixels32(frame.RawImage);
            _texture.Apply(false);
            if (_sprite == null)
            {
                _sprite = Sprite.Create(
                    _texture,
                    new Rect(0, 0, _texture.width, _texture.height),
                    new Vector2(0.5f, 0.5f),
                    MasuThemePlayfieldVisualLayout.OverlaySpritePixelsPerUnit);
            }

            ShowSprite(_sprite);
        }

        void ClearFirstFrame()
        {
            if (_firstSprite != null)
            {
                Destroy(_firstSprite);
                _firstSprite = null;
            }

            if (_firstTexture != null)
            {
                Destroy(_firstTexture);
                _firstTexture = null;
            }

            _cachedForPath = null;
        }

        void StopCoroutineIfRunning()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
        }

        void DestroyDisplayedSprite()
        {
            if (_sprite != null)
            {
                Destroy(_sprite);
                _sprite = null;
            }

            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
        }
    }
}
