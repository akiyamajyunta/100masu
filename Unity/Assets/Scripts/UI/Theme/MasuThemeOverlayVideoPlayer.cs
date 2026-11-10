using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Masu.UI
{
    /// <summary>
    /// プレイ背景の MP4。表示中のデコーダは1本だけにする。
    /// 2本目の Prepare や再生中の time シークは、Windows でデコードが止まったまま戻らない。
    /// ループは isLooping のみ。止まったときも Play() だけで再開し、先頭へ巻き戻さない。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RawImage))]
    public sealed class MasuThemeOverlayVideoPlayer : MasuThemeOverlayVideoHost
    {
        const float StallGraceSeconds = 0.35f;
        const float RecoverCooldownSeconds = 1.5f;
        const double SeamWindowSeconds = 0.2;
        const float AfterLoopQuietSeconds = 0.6f;

        RawImage _rawImage;
        VideoPlayer _player;
        RenderTexture _renderTexture;
        string _playingKey = "";
        Coroutine _bootRoutine;
        float _notPlayingSince = -1f;
        float _nextRecoverTime = -1f;
        float _seamQuietUntil = -1f;
        double _prevTime = -1;

        void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _rawImage.raycastTarget = false;
            _rawImage.color = Color.white;

            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = true;
            _player.waitForFirstFrame = false;
            _player.skipOnDrop = true;
            _player.playbackSpeed = 1f;
            _player.timeReference = VideoTimeReference.Freerun;
            _player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.None;
            _player.errorReceived += OnError;
        }

        void OnDestroy()
        {
            if (_player != null)
                _player.errorReceived -= OnError;
            ReleaseRenderTexture();
        }

        void LateUpdate()
        {
            if (string.IsNullOrEmpty(_playingKey) || _player == null || _player.clip == null) return;
            if (!isActiveAndEnabled || _bootRoutine != null) return;

            var time = _player.time;
            var length = _player.length;
            if (_prevTime > 0.5 && time < _prevTime - 0.5)
                _seamQuietUntil = Time.unscaledTime + AfterLoopQuietSeconds;
            var inSeam = Time.unscaledTime < _seamQuietUntil
                         || (length > 0.5 && time >= length - SeamWindowSeconds);
            _prevTime = time;

            if (inSeam && _player.isPlaying)
            {
                _notPlayingSince = -1f;
                return;
            }

            if (inSeam && !_player.isPlaying)
            {
                if (_notPlayingSince < 0f)
                    _notPlayingSince = Time.unscaledTime;
                if (Time.unscaledTime - _notPlayingSince < 1f)
                    return;
            }

            if (!_player.isPrepared)
            {
                Recover("not prepared");
                return;
            }

            if (_player.isPlaying)
            {
                _notPlayingSince = -1f;
                return;
            }

            if (_notPlayingSince < 0f)
                _notPlayingSince = Time.unscaledTime;
            else if (Time.unscaledTime - _notPlayingSince >= StallGraceSeconds)
                _player.Play();
        }

        void OnError(VideoPlayer _, string message)
        {
            Debug.LogWarning($"[MasuTheme] Video overlay error: {message}");
            Recover("error");
        }

        void Recover(string reason)
        {
            if (string.IsNullOrEmpty(_playingKey) || _player == null || _player.clip == null) return;
            if (Time.unscaledTime < _nextRecoverTime) return;
            _nextRecoverTime = Time.unscaledTime + RecoverCooldownSeconds;
            _notPlayingSince = -1f;
            Debug.LogWarning($"[MasuTheme] Video overlay resume ({reason}): {_playingKey}");

            if (!_player.isPrepared)
            {
                if (_bootRoutine != null)
                    StopCoroutine(_bootRoutine);
                _bootRoutine = StartCoroutine(PrepareAndPlay());
                return;
            }

            _player.Play();
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

            _player.isLooping = true;
            _player.clip = clip;
            _player.targetTexture = _renderTexture;
            _rawImage.texture = _renderTexture;

            _bootRoutine = StartCoroutine(PrepareAndPlay());
            return true;
        }

        IEnumerator PrepareAndPlay()
        {
            if (_player == null || _player.clip == null)
            {
                _bootRoutine = null;
                yield break;
            }

            _player.isLooping = true;
            if (!_player.isPrepared)
                _player.Prepare();

            while (_player != null && !_player.isPrepared)
                yield return null;

            if (_player == null || string.IsNullOrEmpty(_playingKey) || _player.clip == null)
            {
                _bootRoutine = null;
                yield break;
            }

            _player.isLooping = true;
            _player.Play();
            _notPlayingSince = -1f;
            _bootRoutine = null;
        }

        public override void StopPlayback()
        {
            if (_bootRoutine != null)
            {
                StopCoroutine(_bootRoutine);
                _bootRoutine = null;
            }

            _playingKey = "";
            _notPlayingSince = -1f;
            _nextRecoverTime = -1f;
            _seamQuietUntil = -1f;
            _prevTime = -1;

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
