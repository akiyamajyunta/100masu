using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Masu.UI
{
    public static class MasuThemeCatalog
    {
        private static List<MasuThemeDefinition> _all;
        private static bool _loaded;

        public static IReadOnlyList<MasuThemeDefinition> All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        public static MasuThemeDefinition GetById(string themeId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(themeId)) return _all[0];
            return _all.FirstOrDefault(t => t != null && t.ThemeId == themeId) ?? _all[0];
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _all = new List<MasuThemeDefinition>();
            foreach (var builtIn in CreateBuiltInThemes())
                _all.Add(builtIn);

            var assets = Resources.LoadAll<MasuThemeDefinition>("ThemeCatalog");
            foreach (var asset in assets)
            {
                if (asset == null || string.IsNullOrEmpty(asset.ThemeId)) continue;
                var idx = _all.FindIndex(t => t.ThemeId == asset.ThemeId);
                if (idx >= 0)
                    _all[idx] = asset;
                else
                    _all.Add(asset);
            }
        }

        private static IEnumerable<MasuThemeDefinition> CreateBuiltInThemes()
        {
            yield return CreateRuntime("wafuu", "和風", MasuThemePalette.Wafuu(),
                menuPhoto: "title_photo", gameBg: "title_game_bg", overlayGif: "title_game_overlay.gif");
            yield return CreateRuntime("cool", "クール", MasuThemePalette.Cool(),
                menuPhoto: "", gameBg: "theme_cool_game_bg",
                overlayTex: "theme_cool_overlay", overlayScrollSpeed: 42f);
            yield return CreateRuntime("modern", "モダン", MasuThemePalette.Modern(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.1f, 0.14f, 0.2f));
            yield return CreateRuntime("cyber", "サイバー", MasuThemePalette.Cyber(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.02f, 0.06f, 0.08f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.GameOfLife);
            yield return CreateRuntime("corona", "コロナ", MasuThemePalette.Corona(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.08f, 0.03f, 0.01f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.Mandelbrot);
            yield return CreateRuntime("cosmo", "コスモ", MasuThemePalette.Cosmo(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.02f, 0.04f, 0.07f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.Julia);
            yield return CreateRuntime("pop", "ポップ", MasuThemePalette.Pop(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(1f, 0.92f, 0.96f));
            yield return CreateRuntime("buddha", "仏", MasuThemePalette.Buddha(),
                menuPhoto: "", gameBg: "theme_buddha_game_bg",
                overlayGif: "theme_buddha_overlay.gif",
                overlayLayout: MasuThemeGameOverlayLayout.BuddhaMandalaOnGameBackground,
                menuFallback: new Color(0.06f, 0.05f, 0.04f));
        }

        private static MasuThemeDefinition CreateRuntime(
            string id,
            string nameJa,
            MasuThemePalette palette,
            string menuPhoto,
            string gameBg,
            Color? menuFallback = null,
            string overlayGif = "",
            string overlayTex = "",
            float overlayScrollSpeed = 0f,
            MasuThemeGameBackgroundKind gameBackgroundKind = MasuThemeGameBackgroundKind.Auto,
            Color? gameBackdropFallback = null,
            MasuThemeGameOverlayLayout overlayLayout = MasuThemeGameOverlayLayout.FullBleed)
        {
            var def = ScriptableObject.CreateInstance<MasuThemeDefinition>();
            def.name = $"Builtin_{id}";
            def.ThemeId = id;
            def.DisplayNameJa = nameJa;
            def.Palette = palette;
            def.MenuPhotoResource = menuPhoto ?? "";
            def.GameBackgroundResource = gameBg ?? "";
            def.GameBackgroundKind = gameBackgroundKind;
            def.GameOverlayGifResource = overlayGif ?? "";
            def.GameOverlayTextureResource = overlayTex ?? "";
            def.GameOverlayScrollSpeed = overlayScrollSpeed;
            def.GameOverlayLayout = overlayLayout;
            if (menuFallback.HasValue)
                def.MenuBackdropFallback = menuFallback.Value;
            if (gameBackdropFallback.HasValue)
                def.GameBackdropFallback = gameBackdropFallback.Value;
            return def;
        }
    }
}
