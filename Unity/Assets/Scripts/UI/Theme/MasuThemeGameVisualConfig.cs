using UnityEngine;

namespace Masu.UI
{
    /// <summary>1 テーマのプレイ画面ビジュアル定義（データのみ。適用は <see cref="MasuThemePlayfieldVisualController"/>）。</summary>
    public readonly struct MasuThemeGameOverlayConfig
    {
        public MasuThemeGameOverlayKind Kind { get; }
        public string ResourceKey { get; }
        public float ScrollSpeedPixelsPerSecond { get; }

        public MasuThemeGameOverlayConfig(
            MasuThemeGameOverlayKind kind,
            string resourceKey,
            float scrollSpeedPixelsPerSecond = 0f)
        {
            Kind = kind;
            ResourceKey = resourceKey ?? "";
            ScrollSpeedPixelsPerSecond = scrollSpeedPixelsPerSecond;
        }

        public static MasuThemeGameOverlayConfig None => new(MasuThemeGameOverlayKind.None, "");
    }

    public readonly struct MasuThemeGameVisualConfig
    {
        public string BackgroundResourceKey { get; }
        public Color BackgroundFallback { get; }
        public MasuThemeGameOverlayConfig Overlay { get; }

        public MasuThemeGameVisualConfig(
            string backgroundResourceKey,
            Color backgroundFallback,
            MasuThemeGameOverlayConfig overlay)
        {
            BackgroundResourceKey = backgroundResourceKey ?? "";
            BackgroundFallback = backgroundFallback;
            Overlay = overlay;
        }

        public static MasuThemeGameVisualConfig FromDefinition(MasuThemeDefinition def)
        {
            if (def == null)
                return new MasuThemeGameVisualConfig("", Color.black, MasuThemeGameOverlayConfig.None);

            var overlay = ResolveOverlay(def);
            return new MasuThemeGameVisualConfig(
                def.GameBackgroundResource,
                def.GameBackdropFallback,
                overlay);
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
                        def.GameOverlayGifResource);

                var staticFallback = StripGifExtension(def.GameOverlayGifResource);
                if (MasuThemeResourceCache.TryGetTexture(staticFallback, out _))
                {
                    Debug.LogWarning(
                        $"[MasuTheme] GIF not found ({def.GameOverlayGifResource}); using static '{staticFallback}'.");
                    return new MasuThemeGameOverlayConfig(
                        MasuThemeGameOverlayKind.StaticTexture,
                        staticFallback,
                        def.GameOverlayScrollSpeed);
                }
            }

            return MasuThemeGameOverlayConfig.None;
        }

        private static MasuThemeGameOverlayConfig Static(MasuThemeDefinition def) =>
            new(MasuThemeGameOverlayKind.StaticTexture, def.GameOverlayTextureResource, def.GameOverlayScrollSpeed);

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
