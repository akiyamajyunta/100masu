using System;
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

        [SerializeField] string _resourcePath = DefaultResourcePath;

        UnityEngine.UI.Image _image;
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
        GifDecoder _decoder;
        GifFrame _upcoming;
        float _nextFrameTime;
        float _retryAfter;
        Color32[] _displayedPixels;
        Color32[] _firstPixels;

        public string ActiveResourcePath => _resourcePath;
        public bool IsPlaying => _wantsPlayback && isActiveAndEnabled;

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
            DisposeDecoder();
        }

        void OnDestroy()
        {
            DisposeDecoder();
            DestroyDisplayedSprite();
            ClearFirstFrame();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            Reupload(_firstTexture, _firstPixels);
            Reupload(_texture, _displayedPixels);
            if (_wantsPlayback && isActiveAndEnabled && _decoder == null)
                _nextFrameTime = 0f;
        }

        void Update()
        {
            if (!_wantsPlayback || !isActiveAndEnabled || string.IsNullOrEmpty(_resourcePath)) return;
            if (Time.unscaledTime < _retryAfter) return;
            if (Time.unscaledTime < _nextFrameTime) return;

            if (_decoder == null)
            {
                if (!BeginCycle())
                    _retryAfter = Time.unscaledTime + DecodeErrorRetrySeconds;
                return;
            }

            Advance();
        }

        public void Play(string resourcesPath)
        {
            var pathChanged = resourcesPath != _resourcePath && resourcesPath != _pendingResourcePath;
            _wantsPlayback = true;
            _resourcePath = resourcesPath;
            _pendingResourcePath = resourcesPath;
            if (!isActiveAndEnabled)
                return;
            BeginPlayback(soft: !pathChanged && _gifBytes != null);
        }

        public void StopPlayback()
        {
            _wantsPlayback = false;
            PreserveAspect = false;
            _pendingResourcePath = null;
            DisposeDecoder();
            DestroyDisplayedSprite();
            ClearFirstFrame();
            if (_image != null)
                _image.sprite = null;
            _gifBytes = null;
            _bytesPath = null;
            _displayedPixels = null;
        }

        public void SetResourcePath(string resourcesPath) => Play(resourcesPath);

        void BeginPlayback(bool soft)
        {
            if (!string.IsNullOrEmpty(_pendingResourcePath))
            {
                _resourcePath = _pendingResourcePath;
                _pendingResourcePath = null;
            }

            if (!soft || _cachedForPath != _resourcePath)
            {
                DestroyDisplayedSprite();
                ClearFirstFrame();
                if (_image != null) _image.sprite = null;
            }

            DisposeDecoder();
            _nextFrameTime = 0f;
            _retryAfter = 0f;
            if (!TryLoadGifBytes())
                return;
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

        /// <summary>
        /// 待ちはコルーチンにしない。フォーカス移動やオブジェクトの無効化で
        /// WaitForSeconds が戻らないと、再生中フラグだけ残って絵が止まったままになる。
        /// </summary>
        bool BeginCycle()
        {
            if (!TryLoadGifBytes()) return false;
            if (!HasFirstFrame() && !TryCacheFirstFrame()) return false;

            ShowSprite(_firstSprite);
            DisposeDecoder();
            try
            {
                _decoder = new GifDecoder(_gifBytes);
                _decoder.NextImage();
                _upcoming = _decoder.NextImage();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MasuUiGifPlayer] open failed ({_resourcePath}): {ex.Message}");
                DisposeDecoder();
                return false;
            }

            _nextFrameTime = Time.unscaledTime + _firstDelay;
            return true;
        }

        void Advance()
        {
            if (_upcoming == null)
            {
                if (!BeginCycle())
                    _retryAfter = Time.unscaledTime + DecodeErrorRetrySeconds;
                return;
            }

            var delay = FrameDelay(_upcoming);
            if (!TryApplyFrame(_upcoming))
            {
                DisposeDecoder();
                _retryAfter = Time.unscaledTime + DecodeErrorRetrySeconds;
                return;
            }

            try
            {
                _upcoming = _decoder.NextImage();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MasuUiGifPlayer] decode error ({_resourcePath}): {ex.Message}");
                _upcoming = null;
            }

            _nextFrameTime = Time.unscaledTime + delay;
        }

        void DisposeDecoder()
        {
            _upcoming = null;
            if (_decoder == null) return;
            _decoder.Dispose();
            _decoder = null;
        }

        static void Reupload(Texture2D texture, Color32[] pixels)
        {
            if (texture == null || pixels == null || pixels.Length != texture.width * texture.height) return;
            texture.SetPixels32(pixels);
            texture.Apply(false);
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
                    CopyInto(ref _firstPixels, frame.RawImage);
                    _firstTexture = new Texture2D(frame.Width, frame.Height, TextureFormat.ARGB32, false);
                    _firstTexture.filterMode = FilterMode.Bilinear;
                    _firstTexture.wrapMode = TextureWrapMode.Clamp;
                    _firstTexture.SetPixels32(_firstPixels);
                    _firstTexture.Apply(false);
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

            CopyInto(ref _displayedPixels, frame.RawImage);
            _texture.SetPixels32(_displayedPixels);
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
            _firstPixels = null;
        }

        static void CopyInto(ref Color32[] dest, Color32[] source)
        {
            if (source == null || source.Length == 0)
            {
                dest = Array.Empty<Color32>();
                return;
            }

            if (dest == null || dest.Length != source.Length)
                dest = new Color32[source.Length];
            Array.Copy(source, dest, source.Length);
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
