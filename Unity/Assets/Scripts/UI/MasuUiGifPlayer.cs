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

        void Awake()
        {
            _image = GetComponent<UnityEngine.UI.Image>();
            _image.raycastTarget = false;
        }

        void OnEnable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PlayLoop());
        }

        void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        void OnDestroy() => ClearSprite();

        IEnumerator PlayLoop()
        {
            while (isActiveAndEnabled)
            {
                var asset = Resources.Load<TextAsset>(_resourcePath);
                if (asset == null || asset.bytes == null || asset.bytes.Length == 0)
                    yield break;

                using (var decoder = new GifDecoder(asset.bytes))
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
            _sprite = Sprite.Create(
                _texture,
                new Rect(0, 0, _texture.width, _texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            _image.sprite = _sprite;
            _image.type = UnityEngine.UI.Image.Type.Simple;
            _image.preserveAspect = false;
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
