using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>プレイ画面の背景・オーバーレイ再生ライフサイクル（テーマ切替・画面出入り）。</summary>
    public sealed class MasuThemePlayfieldVisualController
    {
        private MasuThemeSpriteBindings _bindings;

        public void Bind(MasuThemeSpriteBindings bindings) => _bindings = bindings;

        public void ApplyGameplay(MasuThemeDefinition definition)
        {
            if (_bindings == null || definition == null) return;

            MasuThemeResourceCache.WarmPlayfield(definition);
            var visual = MasuThemeGameVisualConfig.FromDefinition(definition);

            MasuThemeBackdropApplier.ApplyBackdrop(
                _bindings.GameBackground,
                visual.BackgroundResourceKey,
                visual.BackgroundFallback);

            StopOverlayPlayback();
            ApplyOverlay(visual.Overlay);
        }

        public void StopGameplayOverlays() => StopOverlayPlayback();

        private void ApplyOverlay(MasuThemeGameOverlayConfig overlay)
        {
            switch (overlay.Kind)
            {
                case MasuThemeGameOverlayKind.None:
                    return;
                case MasuThemeGameOverlayKind.StaticTexture:
                    ShowStaticOverlay(overlay);
                    return;
                case MasuThemeGameOverlayKind.GifBytes:
                    ShowGifOverlay(overlay.ResourceKey);
                    return;
                case MasuThemeGameOverlayKind.Video:
                    ShowVideoOverlay(overlay.ResourceKey);
                    return;
            }
        }

        private void ShowStaticOverlay(MasuThemeGameOverlayConfig overlay)
        {
            var img = _bindings?.GameOverlayStatic;
            if (img == null) return;

            SetGifLayerActive(false);
            SetVideoLayerActive(false);
            MasuThemeBackdropApplier.ApplyOverlay(img, overlay.ResourceKey);
            img.gameObject.SetActive(true);
            PinOverlayAboveBackground(img.transform);

            var scroll = img.GetComponent<MasuThemeOverlayScrollMotion>();
            if (scroll == null)
                scroll = img.gameObject.AddComponent<MasuThemeOverlayScrollMotion>();
            if (overlay.ScrollSpeedPixelsPerSecond > 0.01f)
                scroll.Begin(overlay.ScrollSpeedPixelsPerSecond);
            else
                scroll.Stop();
        }

        private void ShowGifOverlay(string gifFileName)
        {
            var player = _bindings?.GameOverlayGif;
            if (player == null) return;

            StopStaticScroll();
            SetStaticLayerActive(false);
            SetVideoLayerActive(false);

            if (!MasuThemeResourceCache.TryGetGifAsset(gifFileName, out _))
            {
                Debug.LogWarning($"[MasuTheme] Game overlay GIF not found: TitleArt/{gifFileName}");
                return;
            }

            var path = MasuThemeResourceCache.TitleArtPath(gifFileName);
            player.Play(path);
            player.gameObject.SetActive(true);
            PinOverlayAboveBackground(player.transform);
        }

        private void ShowVideoOverlay(string clipKey)
        {
            var host = _bindings?.GameOverlayVideo;
            if (host == null)
            {
                Debug.LogWarning(
                    $"[MasuTheme] Video overlay requested ({clipKey}) but {nameof(MasuThemeOverlayVideoHost)} is not registered.");
                return;
            }

            StopStaticScroll();
            SetGifLayerActive(false);
            SetStaticLayerActive(false);
            host.gameObject.SetActive(true);
            if (!host.TryPlayResource(clipKey))
                Debug.LogWarning($"[MasuTheme] Video overlay failed: TitleArt/{clipKey}");
        }

        private void StopOverlayPlayback()
        {
            StopStaticScroll();
            SetStaticLayerActive(false);
            SetGifLayerActive(false);
            SetVideoLayerActive(false);

            _bindings?.GameOverlayGif?.StopPlayback();
            _bindings?.GameOverlayVideo?.StopPlayback();
        }

        private void StopStaticScroll()
        {
            if (_bindings?.GameOverlayStatic == null) return;
            var scroll = _bindings.GameOverlayStatic.GetComponent<MasuThemeOverlayScrollMotion>();
            scroll?.Stop();
        }

        private void SetGifLayerActive(bool active)
        {
            if (_bindings?.GameOverlayGif != null)
                _bindings.GameOverlayGif.gameObject.SetActive(active);
        }

        private void SetStaticLayerActive(bool active)
        {
            if (_bindings?.GameOverlayStatic != null)
            {
                if (!active)
                {
                    StopStaticScroll();
                    MasuThemeBackdropApplier.ClearRuntimeSprite(_bindings.GameOverlayStatic);
                }

                _bindings.GameOverlayStatic.gameObject.SetActive(active);
            }
        }

        private void SetVideoLayerActive(bool active)
        {
            if (_bindings?.GameOverlayVideo == null) return;
            if (!active)
                _bindings.GameOverlayVideo.StopPlayback();
            _bindings.GameOverlayVideo.gameObject.SetActive(active);
        }

        /// <summary>背景の真上・100マス盤面より下（GameBg の次の sibling）。</summary>
        private void PinOverlayAboveBackground(Transform overlay)
        {
            var bg = _bindings?.GameBackground;
            if (bg == null || overlay == null) return;
            overlay.SetSiblingIndex(bg.transform.GetSiblingIndex() + 1);
        }
    }
}
