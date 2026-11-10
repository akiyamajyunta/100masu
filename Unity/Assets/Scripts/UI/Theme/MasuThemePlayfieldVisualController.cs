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

            StopOverlayPlayback();
            ApplyBackground(visual);
            if (UsesFullBleedOverlayPlayBackground(visual))
                PrepareBackdropFallback(visual.BackgroundFallback);
            ApplyOverlay(visual.Overlay, definition);
            ApplyBakedVideoFallbackIfNeeded(definition, visual);
        }

        /// <summary>MP4 未配置・再生失敗時、焼き込み前のプロシージャル実装へフォールバック。</summary>
        void ApplyBakedVideoFallbackIfNeeded(MasuThemeDefinition definition, MasuThemeGameVisualConfig visual)
        {
            if (definition == null) return;
            if (definition.GameOverlayKind != MasuThemeGameOverlayKind.Video) return;

            var videoPlaying = visual.Overlay.Kind == MasuThemeGameOverlayKind.Video
                               && _bindings?.GameOverlayVideo != null
                               && _bindings.GameOverlayVideo.gameObject.activeSelf;
            if (videoPlaying) return;

            switch (definition.ThemeId)
            {
                case "corona":
                    ShowMandelbrot(visual.BackgroundFallback);
                    break;
                case "cosmo":
                    ShowJulia(visual.BackgroundFallback);
                    break;
                case "grid":
                    ShowRedGridPulse(visual.BackgroundFallback);
                    break;
            }
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
                case MasuThemeGameBackgroundKind.Equalizer:
                    ShowEqualizer(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.BubbleSort:
                    ShowBubbleSort(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.TunnelIllusion:
                    ShowTunnelIllusion(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.HannyaGreenStream:
                    ShowHannyaGreenStream(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.RadialDotPattern:
                    ShowRadialDotPattern(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.LcdFireworks:
                    ShowLcdFireworks(visual.BackgroundFallback);
                    return;
                case MasuThemeGameBackgroundKind.RedGridPulse:
                    ShowRedGridPulse(visual.BackgroundFallback);
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

        private void ShowEqualizer(Color fallback)
        {
            var eq = _bindings?.Equalizer;
            if (eq == null) return;

            PrepareBackdropFallback(fallback);
            eq.gameObject.SetActive(true);
            PinProceduralAboveBackground(eq.transform);
            eq.PlayBackground();
        }

        private void ShowBubbleSort(Color fallback)
        {
            var sort = _bindings?.BubbleSort;
            if (sort == null) return;

            PrepareBackdropFallback(fallback);
            sort.gameObject.SetActive(true);
            PinProceduralAboveBackground(sort.transform);
            sort.PlayBackground();
        }

        private void ShowTunnelIllusion(Color fallback)
        {
            var tunnel = _bindings?.TunnelIllusion;
            if (tunnel == null) return;

            PrepareBackdropFallback(fallback);
            tunnel.gameObject.SetActive(true);
            PinProceduralAboveBackground(tunnel.transform);
            tunnel.PlayBackground();
        }

        private void ShowHannyaGreenStream(Color fallback)
        {
            var stream = _bindings?.HannyaGreenStream;
            if (stream == null) return;

            PrepareBackdropFallback(fallback);
            stream.gameObject.SetActive(true);
            PinProceduralAboveBackground(stream.transform);
            stream.PlayBackground();
        }

        private void ShowRadialDotPattern(Color fallback)
        {
            var radial = _bindings?.RadialDotPattern;
            if (radial == null) return;

            PrepareBackdropFallback(fallback);
            radial.gameObject.SetActive(true);
            PinProceduralAboveBackground(radial.transform);
            radial.PlayBackground();
        }

        private void ShowLcdFireworks(Color fallback)
        {
            var lcd = _bindings?.LcdFireworks;
            if (lcd == null) return;

            PrepareBackdropFallback(fallback);
            lcd.gameObject.SetActive(true);
            PinProceduralAboveBackground(lcd.transform);
            lcd.PlayBackground();
        }

        private void ShowRedGridPulse(Color fallback)
        {
            var grid = _bindings?.RedGridPulse;
            if (grid == null) return;

            PrepareBackdropFallback(fallback);
            grid.gameObject.SetActive(true);
            PinProceduralAboveBackground(grid.transform);
            grid.PlayBackground();
        }

        private void HideProceduralBackgrounds()
        {
            HideGameOfLife();
            HideMandelbrot();
            HideJulia();
            HideEqualizer();
            HideBubbleSort();
            HideTunnelIllusion();
            HideHannyaGreenStream();
            HideRadialDotPattern();
            HideLcdFireworks();
            HideRedGridPulse();
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

        private void HideEqualizer()
        {
            var eq = _bindings?.Equalizer;
            if (eq == null) return;
            eq.StopBackground();
            eq.gameObject.SetActive(false);
        }

        private void HideBubbleSort()
        {
            var sort = _bindings?.BubbleSort;
            if (sort == null) return;
            sort.StopBackground();
            sort.gameObject.SetActive(false);
        }

        private void HideTunnelIllusion()
        {
            var tunnel = _bindings?.TunnelIllusion;
            if (tunnel == null) return;
            tunnel.StopBackground();
            tunnel.gameObject.SetActive(false);
        }

        private void HideHannyaGreenStream()
        {
            var stream = _bindings?.HannyaGreenStream;
            if (stream == null) return;
            stream.StopBackground();
            stream.gameObject.SetActive(false);
        }

        private void HideRadialDotPattern()
        {
            var radial = _bindings?.RadialDotPattern;
            if (radial == null) return;
            radial.StopBackground();
            radial.gameObject.SetActive(false);
        }

        private void HideLcdFireworks()
        {
            var lcd = _bindings?.LcdFireworks;
            if (lcd == null) return;
            lcd.StopBackground();
            lcd.gameObject.SetActive(false);
        }

        private void HideRedGridPulse()
        {
            var grid = _bindings?.RedGridPulse;
            if (grid == null) return;
            grid.StopBackground();
            grid.gameObject.SetActive(false);
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
                    ShowVideoOverlay(overlay);
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
            player.Play(path);
            player.gameObject.SetActive(true);
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

        private void ShowVideoOverlay(MasuThemeGameOverlayConfig overlay)
        {
            var host = _bindings?.GameOverlayVideo;
            if (host == null)
            {
                Debug.LogWarning(
                    $"[MasuTheme] Video overlay requested ({overlay.ResourceKey}) but {nameof(MasuThemeOverlayVideoHost)} is not registered.");
                return;
            }

            StopStaticScroll();
            SetGifLayerActive(false);
            SetStaticLayerActive(false);
            SetBuddhaMandalaOrbitActive(false);

            var rt = host.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localEulerAngles = Vector3.zero;
                MasuThemeOverlayRectLayout.ApplyGifOverlay(rt, overlay.Layout);
            }

            host.gameObject.SetActive(true);
            if (!host.TryPlayResource(overlay.ResourceKey))
            {
                Debug.LogWarning($"[MasuTheme] Video overlay failed: TitleArt/{overlay.ResourceKey}");
                SetVideoLayerActive(false);
                return;
            }

            PinLayerAboveBackground(host.transform);
        }

        private void StopOverlayPlayback()
        {
            StopStaticScroll();
            SetStaticLayerActive(false);
            SetBuddhaMandalaOrbitActive(false);
            _bindings?.GameOverlayGif?.StopPlayback();
            _bindings?.GameOverlayVideo?.StopPlayback();
            SetGifLayerActive(false);
            SetVideoLayerActive(false);
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
            var eq = _bindings?.Equalizer;
            if (eq != null && eq.gameObject.activeSelf)
                return eq.transform;
            var sort = _bindings?.BubbleSort;
            if (sort != null && sort.gameObject.activeSelf)
                return sort.transform;
            var tunnel = _bindings?.TunnelIllusion;
            if (tunnel != null && tunnel.gameObject.activeSelf)
                return tunnel.transform;
            var stream = _bindings?.HannyaGreenStream;
            if (stream != null && stream.gameObject.activeSelf)
                return stream.transform;
            var radial = _bindings?.RadialDotPattern;
            if (radial != null && radial.gameObject.activeSelf)
                return radial.transform;
            var lcd = _bindings?.LcdFireworks;
            if (lcd != null && lcd.gameObject.activeSelf)
                return lcd.transform;
            var grid = _bindings?.RedGridPulse;
            if (grid != null && grid.gameObject.activeSelf)
                return grid.transform;
            return null;
        }

        /// <summary>静止 GameBg なしで全面 GIF / 動画をプレイ背景にするテーマ（催眠・パイプ等）。</summary>
        static bool UsesFullBleedOverlayPlayBackground(MasuThemeGameVisualConfig visual)
        {
            if (!string.IsNullOrEmpty(visual.BackgroundResourceKey)) return false;
            if (visual.Overlay.Layout != MasuThemeGameOverlayLayout.FullBleed) return false;
            return visual.Overlay.Kind is MasuThemeGameOverlayKind.GifBytes or MasuThemeGameOverlayKind.Video;
        }
    }
}
