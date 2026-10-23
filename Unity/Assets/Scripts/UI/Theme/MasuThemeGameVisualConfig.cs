using UnityEngine;

namespace Masu.UI
{
    /// <summary>1 テーマのプレイ画面ビジュアル定義（データのみ。適用は <see cref="MasuThemePlayfieldVisualController"/>）。</summary>
    public readonly struct MasuThemeGameOverlayConfig
    {
        public MasuThemeGameOverlayKind Kind { get; }
        public string ResourceKey { get; }
        public float ScrollSpeedPixelsPerSecond { get; }
        public MasuThemeGameOverlayLayout Layout { get; }

        public MasuThemeGameOverlayConfig(
            MasuThemeGameOverlayKind kind,
            string resourceKey,
            float scrollSpeedPixelsPerSecond = 0f,
            MasuThemeGameOverlayLayout layout = MasuThemeGameOverlayLayout.FullBleed)
        {
            Kind = kind;
            ResourceKey = resourceKey ?? "";
            ScrollSpeedPixelsPerSecond = scrollSpeedPixelsPerSecond;
            Layout = layout;
        }

        public static MasuThemeGameOverlayConfig None => new(MasuThemeGameOverlayKind.None, "");
    }

    public readonly struct MasuThemeGameVisualConfig
    {
        public MasuThemeGameBackgroundKind BackgroundKind { get; }
        public string BackgroundResourceKey { get; }
        public Color BackgroundFallback { get; }
        public MasuThemeGameOverlayConfig Overlay { get; }

        public MasuThemeGameVisualConfig(
            MasuThemeGameBackgroundKind backgroundKind,
            string backgroundResourceKey,
            Color backgroundFallback,
            MasuThemeGameOverlayConfig overlay)
        {
            BackgroundKind = backgroundKind;
            BackgroundResourceKey = backgroundResourceKey ?? "";
            BackgroundFallback = backgroundFallback;
            Overlay = overlay;
        }

        public static MasuThemeGameVisualConfig FromDefinition(MasuThemeDefinition def)
        {
            if (def == null)
                return new MasuThemeGameVisualConfig(
                    MasuThemeGameBackgroundKind.TitleArtTexture, "", Color.black, MasuThemeGameOverlayConfig.None);

            var overlay = ResolveOverlay(def);
            return new MasuThemeGameVisualConfig(
                ResolveBackgroundKind(def),
                def.GameBackgroundResource,
                def.GameBackdropFallback,
                overlay);
        }

        private static MasuThemeGameBackgroundKind ResolveBackgroundKind(MasuThemeDefinition def)
        {
            if (def.GameBackgroundKind != MasuThemeGameBackgroundKind.Auto)
                return def.GameBackgroundKind;
            if (!string.IsNullOrEmpty(def.GameBackgroundResource))
                return MasuThemeGameBackgroundKind.TitleArtTexture;
            return MasuThemeGameBackgroundKind.TitleArtTexture;
        }

        private static MasuThemeGameOverlayConfig ResolveOverlay(MasuThemeDefinition def)
        {
            if (def.GameOverlayKind != MasuThemeGameOverlayKind.Auto)
                return FromExplicitKind(def.GameOverlayKind, def.GameOverlayResource, def.GameOverlayScrollSpeed);

            if (!string.IsNullOrEmpty(def.GameOverlayTextureResource))
                return Static(def);

            if (!string.IsNullOrEmpty(def.GameOverlayGifResource))
            {
                if (MasuThemeResourceCache.TryGetGifAsset(def.GameOverlayGifResource, out _))
                    return new MasuThemeGameOverlayConfig(
                        MasuThemeGameOverlayKind.GifBytes,
                        def.GameOverlayGifResource,
                        layout: def.GameOverlayLayout);

                var staticFallback = StripGifExtension(def.GameOverlayGifResource);
                if (MasuThemeResourceCache.TryGetTexture(staticFallback, out _))
                {
                    Debug.LogWarning(
                        $"[MasuTheme] GIF not found ({def.GameOverlayGifResource}); using static '{staticFallback}'.");
                    return new MasuThemeGameOverlayConfig(
                        MasuThemeGameOverlayKind.StaticTexture,
                        staticFallback,
                        def.GameOverlayScrollSpeed,
                        def.GameOverlayLayout);
                }
            }

            return MasuThemeGameOverlayConfig.None;
        }

        private static MasuThemeGameOverlayConfig Static(MasuThemeDefinition def) =>
            new(
                MasuThemeGameOverlayKind.StaticTexture,
                def.GameOverlayTextureResource,
                def.GameOverlayScrollSpeed,
                def.GameOverlayLayout);

        private static MasuThemeGameOverlayConfig FromExplicitKind(
            MasuThemeGameOverlayKind kind,
            string resourceKey,
            float scrollSpeed)
        {
            if (kind == MasuThemeGameOverlayKind.None || string.IsNullOrEmpty(resourceKey))
                return MasuThemeGameOverlayConfig.None;
            return new MasuThemeGameOverlayConfig(kind, resourceKey, scrollSpeed);
        }

        private static string StripGifExtension(string gifFileName)
        {
            if (string.IsNullOrEmpty(gifFileName)) return "";
            return gifFileName.EndsWith(".gif")
                ? gifFileName.Substring(0, gifFileName.Length - 4)
                : gifFileName;
        }
    }
}
