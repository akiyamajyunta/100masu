using System;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// テーマの読み込み・永続化。
    /// タイトル／森メニュー等のメイン画面は常に和風。プレイ画面のみ選択テーマを反映する。
    /// </summary>
    public static class MasuThemeManager
    {
        private const string PrefKeyThemeId = "masu_visual_theme_id";
        private const string MainMenuThemeId = "wafuu";

        public static MasuThemeDefinition ActiveDefinition { get; private set; }
        public static event Action<MasuThemeDefinition> ThemeChanged;

        private static MasuThemeSpriteBindings _bindings;
        private static readonly MasuThemePlayfieldVisualController Playfield = new();

        public static void Initialize()
        {
            MasuThemeCatalog.EnsureLoaded();
            var id = PlayerPrefs.GetString(PrefKeyThemeId, MainMenuThemeId);
            ActiveDefinition = MasuThemeCatalog.GetById(id);
            ApplyMainMenuLook();
        }

        public static void RegisterBindings(MasuThemeSpriteBindings bindings)
        {
            _bindings = bindings;
            Playfield.Bind(bindings);
        }

        public static void RegisterInstructionBannerArt(Image bannerArt)
        {
            if (bannerArt == null) return;
            _bindings ??= new MasuThemeSpriteBindings();
            if (!_bindings.InstructionBannerArts.Contains(bannerArt))
                _bindings.InstructionBannerArts.Add(bannerArt);
        }

        /// <summary>ビジュアル選択用。保存のみ（メイン画面の見た目は変えない）。</summary>
        public static void SetActiveTheme(string themeId, bool persist = true)
        {
            var def = MasuThemeCatalog.GetById(themeId);
            if (def == null) return;
            SelectTheme(def, persist);
        }

        public static void SetActiveTheme(MasuThemeDefinition definition, bool persist = true)
        {
            if (definition == null) return;
            SelectTheme(definition, persist);
        }

        /// <summary>UI 構築直後：タイトル／メニュー背景を和風で固定。</summary>
        public static void ApplyMainMenuLook()
        {
            Playfield.StopGameplayOverlays();
            MasuThemeResourceCache.ReleaseGameplayCaches();

            var wafuu = MasuThemeCatalog.GetById(MainMenuThemeId);
            MasuTheme.ApplyPalette(wafuu.Palette);
            ApplyMainMenuSprites(wafuu);
        }

        /// <summary>ゲーム画面へ入るとき：選択テーマの色とプレイ背景を適用。</summary>
        public static void ApplyGameplayLook()
        {
            var def = ActiveDefinition ?? MasuThemeCatalog.GetById(MainMenuThemeId);
            MasuTheme.ApplyPalette(def.Palette);
            Playfield.ApplyGameplay(def);
        }

        private static void SelectTheme(MasuThemeDefinition def, bool persist)
        {
            ActiveDefinition = def;
            if (persist)
            {
                PlayerPrefs.SetString(PrefKeyThemeId, def.ThemeId);
                PlayerPrefs.Save();
            }

            MasuThemeResourceCache.WarmPlayfield(def);
            ThemeChanged?.Invoke(def);
        }

        private static void ApplyMainMenuSprites(MasuThemeDefinition wafuu)
        {
            if (_bindings == null || wafuu == null) return;

            MasuThemeBackdropApplier.ApplyBackdrop(
                _bindings.TitleBackdropPhoto, wafuu.MenuPhotoResource, wafuu.MenuBackdropFallback);
            MasuThemeBackdropApplier.ApplyBackdrop(
                _bindings.ForestBackdropPhoto, wafuu.MenuPhotoResource, wafuu.MenuBackdropFallback);
            MasuThemeBackdropApplier.ApplySprite(_bindings.TitleSidePanel, wafuu.TitleSidePanelResource);

            var scrimKey = wafuu.MenuScrimResource;
            MasuThemeBackdropApplier.ApplyBackdrop(
                _bindings.TitleBackdropScrim, scrimKey, new Color(0, 0, 0, 0));
            MasuThemeBackdropApplier.ApplyBackdrop(
                _bindings.ForestBackdropScrim, scrimKey, new Color(0, 0, 0, 0));

            foreach (var art in _bindings.InstructionBannerArts)
                MasuThemeBackdropApplier.ApplySprite(art, wafuu.InstructionBannerResource);
        }
    }
}
