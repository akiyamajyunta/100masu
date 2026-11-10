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
            yield return CreateRuntime("cyber", "サイバー", MasuThemePalette.Cyber(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.02f, 0.06f, 0.08f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.GameOfLife);
            yield return CreateRuntime("corona", "コロナ", MasuThemePalette.Corona(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.08f, 0.03f, 0.01f),
                gameBackdropFallback: new Color32(0x08, 0x03, 0x01, 0xff),
                overlayVideo: "theme_mandelbrot_background",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("cosmo", "コスモ", MasuThemePalette.Cosmo(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.02f, 0.04f, 0.07f),
                gameBackdropFallback: new Color32(0x06, 0x0a, 0x12, 0xff),
                overlayVideo: "theme_julia_background",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("cosmic", "コズミック", MasuThemePalette.Cosmic(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.04f, 0.04f, 0.05f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.LcdFireworks,
                gameBackdropFallback: Color.black);
            yield return CreateRuntime("grid", "グリッド", MasuThemePalette.Grid(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.06f, 0.02f, 0.02f),
                gameBackdropFallback: Color.black,
                overlayVideo: "theme_red_grid_pulse_background",
                overlayGif: "theme_red_grid_pulse_background.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("data", "データ", MasuThemePalette.Data(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.04f, 0.06f, 0.13f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.BubbleSort,
                gameBackdropFallback: new Color32(0x0a, 0x10, 0x22, 0xff));
            yield return CreateRuntime("live", "ライブ", MasuThemePalette.Live(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.04f, 0.06f, 0.13f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.Equalizer,
                gameBackdropFallback: new Color32(0x0a, 0x10, 0x22, 0xff));
            yield return CreateRuntime("neo", "ネオ", MasuThemePalette.Neo(),
                menuPhoto: "", gameBg: "", menuFallback: Color.black,
                gameBackgroundKind: MasuThemeGameBackgroundKind.HannyaGreenStream,
                gameBackdropFallback: Color.black);
            yield return CreateRuntime("pipe", "パイプ", MasuThemePalette.Pipe(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.04f, 0.04f, 0.06f),
                gameBackdropFallback: Color.black,
                overlayVideo: "theme_pipe_screensaver",
                overlayGif: "theme_pipe_screensaver.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("saimin", "催眠", MasuThemePalette.Saimin(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.12f, 0.12f, 0.15f),
                gameBackdropFallback: new Color32(0x2a, 0x2a, 0x34, 0xff),
                overlayVideo: "theme_saimin_tunnel",
                overlayGif: "theme_saimin_tunnel.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("aurora", "オーロラ", MasuThemePalette.Aurora(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.04f, 0.06f, 0.08f),
                gameBackdropFallback: new Color32(0x0a, 0x0c, 0x10, 0xff),
                overlayGif: "theme_aurora_ribbon.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("suimyaku", "水脈", MasuThemePalette.Suimyaku(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.02f, 0.12f, 0.14f),
                gameBackdropFallback: new Color32(0x04, 0x28, 0x33, 0xff),
                overlayGif: "theme_suimyaku.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("ryusei", "流星", MasuThemePalette.Ryusei(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.02f, 0.03f, 0.08f),
                gameBackdropFallback: new Color32(0x05, 0x07, 0x14, 0xff),
                overlayGif: "theme_ryusei.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("komorebi", "木漏れ日", MasuThemePalette.Komorebi(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.12f, 0.09f, 0.04f),
                gameBackdropFallback: new Color32(0x1a, 0x14, 0x08, 0xff),
                overlayGif: "theme_komorebi.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("holy", "ホーリー", MasuThemePalette.Holy(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.10f, 0.06f, 0.16f),
                gameBackdropFallback: new Color32(0x1a, 0x10, 0x30, 0xff),
                overlayGif: "theme_holy.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("marble", "マーブル", MasuThemePalette.Marble(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.10f, 0.04f, 0.10f),
                gameBackdropFallback: new Color32(0x14, 0x08, 0x18, 0xff),
                overlayGif: "theme_marble.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("energy", "エナジー", MasuThemePalette.Energy(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.02f, 0.08f, 0.04f),
                gameBackdropFallback: new Color32(0x02, 0x14, 0x0c, 0xff),
                overlayGif: "theme_energy.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("disco", "ディスコ", MasuThemePalette.Disco(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.10f, 0.04f, 0.10f),
                gameBackdropFallback: new Color32(0x12, 0x08, 0x18, 0xff),
                overlayGif: "theme_disco.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("tile", "タイル", MasuThemePalette.Tile(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(0.08f, 0.08f, 0.08f),
                gameBackdropFallback: new Color32(0x10, 0x10, 0x10, 0xff),
                overlayGif: "theme_tile.gif",
                overlayLayout: MasuThemeGameOverlayLayout.FullBleed);
            yield return CreateRuntime("dessert", "デザート", MasuThemePalette.Dessert(),
                menuPhoto: "", gameBg: "theme_dessert_game_bg",
                menuFallback: new Color(0.96f, 0.92f, 0.88f));
            yield return CreateRuntime("pop", "ポップ", MasuThemePalette.Pop(),
                menuPhoto: "", gameBg: "", menuFallback: new Color(1f, 0.92f, 0.96f),
                gameBackgroundKind: MasuThemeGameBackgroundKind.RadialDotPattern,
                gameBackdropFallback: new Color32(0x1e, 0x1e, 0x1e, 0xff));
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
            string overlayVideo = "",
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
            if (!string.IsNullOrEmpty(overlayVideo))
            {
                def.GameOverlayKind = MasuThemeGameOverlayKind.Video;
                def.GameOverlayResource = overlayVideo;
            }

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
