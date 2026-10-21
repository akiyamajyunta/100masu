using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>Image への TitleArt テクスチャ適用（メニュー／プレイ共通）。</summary>
    public static class MasuThemeBackdropApplier
    {
        public static void ApplyBackdrop(Image target, string resourceKey, Color fallback)
        {
            if (target == null) return;
            if (string.IsNullOrEmpty(resourceKey))
            {
                ClearRuntimeSprite(target);
                target.sprite = null;
                target.color = fallback;
                target.enabled = true;
                return;
            }

            if (!MasuThemeResourceCache.TryGetTexture(resourceKey, out var tex) || tex == null)
            {
                Debug.LogWarning($"[MasuTheme] Texture not found: TitleArt/{resourceKey}");
                ClearRuntimeSprite(target);
                target.sprite = null;
                target.color = fallback;
                return;
            }

            ClearRuntimeSprite(target);
            var ppu = MasuThemePlayfieldVisualLayout.OverlaySpritePixelsPerUnit;
            target.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            target.type = Image.Type.Simple;
            target.preserveAspect = false;
            target.color = Color.white;
            target.enabled = true;
        }

        public static void ApplySprite(Image target, string resourceKey)
        {
            if (target == null || string.IsNullOrEmpty(resourceKey)) return;
            if (!MasuThemeResourceCache.TryGetTexture(resourceKey, out var tex) || tex == null) return;
            ClearRuntimeSprite(target);
            var ppu = MasuThemePlayfieldVisualLayout.OverlaySpritePixelsPerUnit;
            target.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            target.color = Color.white;
        }

        /// <summary>透過 PNG オーバーレイ（盤面の上に重ねる）。</summary>
        public static void ApplyOverlay(Image target, string resourceKey)
        {
            ApplyBackdrop(target, resourceKey, Color.clear);
            if (target == null) return;
            target.raycastTarget = false;
            if (target.canvasRenderer != null)
                target.canvasRenderer.cullTransparentMesh = false;
        }

        public static void ClearRuntimeSprite(Image target)
        {
            if (target == null || target.sprite == null) return;
            var sp = target.sprite;
            target.sprite = null;
            if (sp != null)
                UnityEngine.Object.Destroy(sp);
        }
    }
}
