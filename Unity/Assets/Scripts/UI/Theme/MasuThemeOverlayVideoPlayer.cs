using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Masu.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class MasuThemeOverlayVideoPlayer : MasuThemeOverlayVideoHost
    {
        RawImage _rawImage;
        VideoPlayer _player;
        RenderTexture _renderTexture;
        string _playingKey = "";

        void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _rawImage.raycastTarget = false;
            _rawImage.color = Color.white;

            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = true;
            _player.skipOnDrop = true;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.None;
        }

        void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        public override bool TryPlayResource(string titleArtClipKey)
        {
            if (string.IsNullOrEmpty(titleArtClipKey)) return false;
            if (_playingKey == titleArtClipKey && _player != null && _player.isPlaying)
                return true;

            if (!MasuThemeResourceCache.TryGetVideoClip(titleArtClipKey, out var clip) || clip == null)
                return false;

            StopPlayback();
            _playingKey = titleArtClipKey;

            var width = (int)clip.width;
            var height = (int)clip.height;
            if (width < 1) width = 1920;
            if (height < 1) height = 1080;
            EnsureRenderTexture(width, height);

            _player.clip = clip;
            _player.targetTexture = _renderTexture;
            _rawImage.texture = _renderTexture;
            _player.Play();
            return true;
        }

        public override void StopPlayback()
        {
            _playingKey = "";
            if (_player != null)
            {
                if (_player.isPlaying)
                    _player.Stop();
                _player.clip = null;
                _player.targetTexture = null;
            }

            if (_rawImage != null)
                _rawImage.texture = null;
        }

        void EnsureRenderTexture(int width, int height)
        {
            if (_renderTexture != null
                && _renderTexture.width == width
                && _renderTexture.height == height)
                return;

            ReleaseRenderTexture();
            _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _renderTexture.Create();
        }

        void ReleaseRenderTexture()
        {
            if (_renderTexture == null) return;
            if (_renderTexture.IsCreated())
                _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}
