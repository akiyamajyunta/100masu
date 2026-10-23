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

            ApplyBackground(visual);
            StopOverlayPlayback();
            ApplyOverlay(visual.Overlay, definition);
        }

        public void StopGameplayOverlays()
        {
            StopOverlayPlayback();
            HideProceduralBackgrounds();
        }

        private void ApplyBackground(MasuThemeGameVisualConfig visual)
        {
            HideProceduralBackgrounds();

            switch (visual.BackgroundKind)
            {
                case MasuThemeGameBackgroundKind.GameOfLife:
                    ShowGameOfLife(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.Mandelbrot:
                    ShowMandelbrot(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.Julia:
                    ShowJulia(visual.BackgroundFallback);
                    return;
            }

            if (_bindings.GameBackground != null)
                _bindings.GameBackground.enabled = true;
            MasuThemeBackdropApplier.ApplyBackdrop(
                _bindings.GameBackground,
                visual.BackgroundResourceKey,
                visual.BackgroundFallback);
        }

        private void PrepareBackdropFallback(Color fallback)
        {
            if (_bindings?.GameBackground == null) return;
            MasuThemeBackdropApplier.ClearRuntimeSprite(_bindings.GameBackground);
            _bindings.GameBackground.sprite = null;
            _bindings.GameBackground.color = fallback;
            // プロシージャル RawImage の下に単色 GameBg が見えると「灰色だけ」に見えるため非表示にする。
            _bindings.GameBackground.enabled = false;
        }

        private void ShowGameOfLife(Color fallback)
        {
            var life = _bindings?.GameOfLife;
            if (life == null) return;

            PrepareBackdropFallback(fallback);
            life.gameObject.SetActive(true);
            PinProceduralAboveBackground(life.transform);
            life.PlayBackground();
        }

        private void ShowMandelbrot(Color fallback)
        {
            var fractal = _bindings?.Mandelbrot;
            if (fractal == null) return;

            PrepareBackdropFallback(fallback);
            fractal.gameObject.SetActive(true);
            PinProceduralAboveBackground(fractal.transform);
            fractal.PlayBackground();
        }

        private void ShowJulia(Color fallback)
        {
            var julia = _bindings?.Julia;
            if (julia == null) return;

            PrepareBackdropFallback(fallback);
            julia.gameObject.SetActive(true);
            PinProceduralAboveBackground(julia.transform);
            julia.PlayBackground();
        }

        private void HideProceduralBackgrounds()
        {
            HideGameOfLife();
            HideMandelbrot();
            HideJulia();
        }

        private void HideGameOfLife()
        {
            var life = _bindings?.GameOfLife;
            if (life == null) return;
            life.StopBackground();
            life.gameObject.SetActive(false);
        }

        private void HideMandelbrot()
        {
            var fractal = _bindings?.Mandelbrot;
            if (fractal == null) return;
            fractal.StopBackground();
            fractal.gameObject.SetActive(false);
        }

        private void HideJulia()
        {
            var julia = _bindings?.Julia;
            if (julia == null) return;
            julia.StopBackground();
            julia.gameObject.SetActive(false);
        }

        private void ApplyOverlay(MasuThemeGameOverlayConfig overlay, MasuThemeDefinition definition)
        {
            if (definition?.ThemeId == "buddha")
            {
                ShowBuddhaMandalaOrbit(overlay.Kind == MasuThemeGameOverlayKind.None
                    ? new MasuThemeGameOverlayConfig(
                        MasuThemeGameOverlayKind.GifBytes,
                        "",
                        layout: MasuThemeGameOverlayLayout.BuddhaMandalaOnGameBackground)
                    : overlay);
                return;
            }

            switch (overlay.Kind)
            {
                case MasuThemeGameOverlayKind.None:
                    return;
                case MasuThemeGameOverlayKind.StaticTexture:
                    ShowStaticOverlay(overlay);
                    return;
                case MasuThemeGameOverlayKind.GifBytes:
                    ShowGifOverlay(overlay);
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
            SetBuddhaMandalaOrbitActive(false);
            SetVideoLayerActive(false);
            MasuThemeBackdropApplier.ApplyOverlay(img, overlay.ResourceKey);
            img.gameObject.SetActive(true);
            PinLayerAboveBackground(img.transform);

            var scroll = img.GetComponent<MasuThemeOverlayScrollMotion>();
            if (scroll == null)
                scroll = img.gameObject.AddComponent<MasuThemeOverlayScrollMotion>();
            if (overlay.ScrollSpeedPixelsPerSecond > 0.01f)
                scroll.Begin(overlay.ScrollSpeedPixelsPerSecond);
            else
                scroll.Stop();
        }

        private void ShowGifOverlay(MasuThemeGameOverlayConfig overlay)
        {
            var player = _bindings?.GameOverlayGif;
            if (player == null) return;

            StopStaticScroll();
            SetStaticLayerActive(false);
            SetVideoLayerActive(false);
            SetBuddhaMandalaOrbitActive(false);
            var gifFileName = overlay.ResourceKey;
            if (!MasuThemeResourceCache.TryGetGifAsset(gifFileName, out _))
            {
                Debug.LogWarning($"[MasuTheme] Game overlay GIF not found: TitleArt/{gifFileName}");
                return;
            }

            var rt = player.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            rt.localEulerAngles = Vector3.zero;
            MasuThemeOverlayRectLayout.ApplyGifOverlay(rt, overlay.Layout);
            player.PreserveAspect = overlay.Layout is MasuThemeGameOverlayLayout.CenterFit
                or MasuThemeGameOverlayLayout.BottomRightFit;
            var path = MasuThemeResourceCache.TitleArtPath(gifFileName);
            player.gameObject.SetActive(true);
            player.Play(path);
            MasuThemeOverlayRectLayout.ApplyGifOverlay(rt, overlay.Layout);
            PinLayerAboveBackground(player.transform);
        }

        private void ShowBuddhaMandalaOrbit(MasuThemeGameOverlayConfig overlay)
        {
            var view = _bindings?.GameBuddhaMandalaOrbit;
            if (view == null) return;

            StopStaticScroll();
            SetStaticLayerActive(false);
            SetVideoLayerActive(false);
            SetGifLayerActive(false);
            _bindings?.GameOverlayGif?.StopPlayback();

            PinMandalaAboveGameBackground(view.transform);
            view.Show(overlay.Layout);
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
            SetBuddhaMandalaOrbitActive(false);
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

        private void SetBuddhaMandalaOrbitActive(bool active)
        {
            if (_bindings?.GameBuddhaMandalaOrbit == null) return;
            if (active)
                _bindings.GameBuddhaMandalaOrbit.gameObject.SetActive(true);
            else
                _bindings.GameBuddhaMandalaOrbit.Hide();
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

        private void PinProceduralAboveBackground(Transform layer)
        {
            var bg = _bindings?.GameBackground;
            if (bg == null || layer == null) return;
            layer.SetSiblingIndex(bg.transform.GetSiblingIndex() + 1);
        }

        /// <summary>涅槃背景の直上（パン・ズーム一体）。盤面 UI より背面。</summary>
        private void PinMandalaAboveGameBackground(Transform layer)
        {
            var bg = _bindings?.GameBackground;
            if (bg == null || layer == null) return;

            if (layer.parent != bg.transform)
                layer.SetParent(bg.transform, false);

            layer.SetAsLastSibling();
        }

        /// <summary>背景（静止画 or プロシージャル）の真上・100マス盤面より下。</summary>
        private void PinLayerAboveBackground(Transform layer)
        {
            var bg = _bindings?.GameBackground;
            if (bg == null || layer == null) return;
            var anchor = GetActiveProceduralTransform() ?? bg.transform;
            layer.SetSiblingIndex(anchor.GetSiblingIndex() + 1);
        }

        private Transform GetActiveProceduralTransform()
        {
            var life = _bindings?.GameOfLife;
            if (life != null && life.gameObject.activeSelf)
                return life.transform;
            var fractal = _bindings?.Mandelbrot;
            if (fractal != null && fractal.gameObject.activeSelf)
                return fractal.transform;
            var julia = _bindings?.Julia;
            if (julia != null && julia.gameObject.activeSelf)
                return julia.transform;
            return null;
        }
    }
}
