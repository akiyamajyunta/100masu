using System.Collections.Generic;
using UnityEngine;

namespace Masu.UI
{
    /// <summary>Resources/TitleArt の同期ロードとキャッシュ（Addressables 化時はここを差し替え）。</summary>
    public static class MasuThemeResourceCache
    {
        private static readonly Dictionary<string, TextAsset> GifAssets = new();
        private static readonly Dictionary<string, Texture2D> Textures = new();

        public static string TitleArtPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            return MasuThemePlayfieldVisualLayout.TitleArtFolder + "/" + fileName;
        }

        public static void WarmPlayfield(MasuThemeDefinition definition)
        {
            if (definition == null) return;
            var visual = MasuThemeGameVisualConfig.FromDefinition(definition);
            if (!string.IsNullOrEmpty(visual.BackgroundResourceKey))
                TryGetTexture(visual.BackgroundResourceKey, out _);

            if (definition.ThemeId == "buddha")
                TryGetTexture(MasuThemePlayfieldVisualLayout.BuddhaMandalaTextureResource, out _);

            switch (visual.Overlay.Kind)
            {
                case MasuThemeGameOverlayKind.GifBytes:
                    TryGetGifAsset(visual.Overlay.ResourceKey, out _);
                    break;
                case MasuThemeGameOverlayKind.StaticTexture:
                    TryGetTexture(visual.Overlay.ResourceKey, out _);
                    break;
                case MasuThemeGameOverlayKind.Video:
                    break;
            }
        }

        public static void ReleaseGameplayCaches()
        {
            GifAssets.Clear();
            Textures.Clear();
        }

        public static bool TryGetTexture(string resourceKeyWithoutFolder, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(resourceKeyWithoutFolder)) return false;
            if (Textures.TryGetValue(resourceKeyWithoutFolder, out texture) && texture != null)
                return true;

            texture = Resources.Load<Texture2D>(TitleArtPath(resourceKeyWithoutFolder));
            if (texture == null) return false;
            Textures[resourceKeyWithoutFolder] = texture;
            return true;
        }

        public static bool TryGetGifAsset(string gifFileName, out TextAsset asset)
        {
            asset = null;
            if (string.IsNullOrEmpty(gifFileName)) return false;
            if (GifAssets.TryGetValue(gifFileName, out asset) && asset != null)
                return true;

            asset = Resources.Load<TextAsset>(TitleArtPath(gifFileName));
            if (asset == null || asset.bytes == null || asset.bytes.Length == 0)
            {
                asset = null;
                return false;
            }

            GifAssets[gifFileName] = asset;
            return true;
        }
    }
}
