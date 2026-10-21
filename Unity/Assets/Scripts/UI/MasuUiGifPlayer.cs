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

        [SerializeField] string _resourcePath = DefaultResourcePath;

        UnityEngine.UI.Image _image;
        Coroutine _routine;
        Texture2D _texture;
        Sprite _sprite;
        byte[] _gifBytes;
        string _pendingResourcePath;

        public string ActiveResourcePath => _resourcePath;
        public bool IsPlaying => _routine != null;

        void Awake()
        {
            _image = GetComponent<UnityEngine.UI.Image>();
            _image.raycastTarget = false;
        }

        void OnEnable()
        {
            if (string.IsNullOrEmpty(_resourcePath) && string.IsNullOrEmpty(_pendingResourcePath))
                return;
            RestartPlayback();
        }

        void OnDisable()
        {
            StopCoroutineIfRunning();
            ClearSprite();
            if (_image != null) _image.sprite = null;
        }

        void OnDestroy()
        {
            StopCoroutineIfRunning();
            ClearSprite();
        }

        /// <summary>パスを差し替えて再生（TitleArt/xxx.gif）。未アクティブ時は有効化後に再生。</summary>
        public void Play(string resourcesPath)
        {
            _resourcePath = resourcesPath;
            _pendingResourcePath = resourcesPath;
            if (!isActiveAndEnabled)
                return;
            RestartPlayback();
        }

        public void StopPlayback()
        {
            _pendingResourcePath = null;
            StopCoroutineIfRunning();
            ClearSprite();
            if (_image != null)
                _image.sprite = null;
            _gifBytes = null;
        }

        /// <summary>互換: <see cref="Play"/> へ委譲。</summary>
        public void SetResourcePath(string resourcesPath) => Play(resourcesPath);

        void RestartPlayback()
        {
            if (!string.IsNullOrEmpty(_pendingResourcePath))
            {
                _resourcePath = _pendingResourcePath;
                _pendingResourcePath = null;
            }

            StopCoroutineIfRunning();
            ClearSprite();
            if (_image != null) _image.sprite = null;

            if (!TryLoadGifBytes())
                return;

            _routine = StartCoroutine(PlayLoop());
        }

        bool TryLoadGifBytes()
        {
            _gifBytes = null;
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
            return _gifBytes != null && _gifBytes.Length > 0;
        }

        IEnumerator PlayLoop()
        {
            if (_gifBytes == null || _gifBytes.Length == 0)
                yield break;

            while (isActiveAndEnabled)
            {
                using (var decoder = new GifDecoder(_gifBytes))
                {
                    for (var frame = decoder.NextImage(); frame != null; frame = decoder.NextImage())
                    {
                        if (!isActiveAndEnabled) yield break;
                        ApplyFrame(frame);
                        var delaySec = Mathf.Max(0.02f, frame.Delay / 1000f);
                        yield return new WaitForSeconds(delaySec);
                    }
                }
            }
        }

        void ApplyFrame(GifFrame frame)
        {
            ClearSprite();
            _texture = frame.CreateTexture();
            _texture.filterMode = FilterMode.Bilinear;
            _sprite = Sprite.Create(
                _texture,
                new Rect(0, 0, _texture.width, _texture.height),
                new Vector2(0.5f, 0.5f),
                MasuThemePlayfieldVisualLayout.OverlaySpritePixelsPerUnit);
            _image.sprite = _sprite;
            _image.type = UnityEngine.UI.Image.Type.Simple;
            _image.preserveAspect = false;
            _image.color = Color.white;
            _image.enabled = true;
            if (_image.canvasRenderer != null)
                _image.canvasRenderer.cullTransparentMesh = false;
        }

        void StopCoroutineIfRunning()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
        }

        void ClearSprite()
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
