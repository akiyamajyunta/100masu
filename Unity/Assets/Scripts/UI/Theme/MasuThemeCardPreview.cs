using System.Collections.Generic;
using UnityEngine;

namespace Masu.UI
{
    /// <summary>ビジュアル選択カード用の TitleArt プレビューキー（拡張子なし）を解決する。</summary>
    /// <remarks>
    /// theme_card_* は Unity/tools/bake_theme_card_previews.py でプレイ背景相当の静止画を生成。
    /// wafuu/cool/dessert/buddha/aurora は既存 TitleArt を直接参照。
    /// </remarks>
    public static class MasuThemeCardPreview
    {
        static readonly Dictionary<string, string> BuiltInKeys = new()
        {
            ["wafuu"] = "title_game_bg",
            ["cool"] = "theme_cool_game_bg",
            ["cyber"] = "theme_card_cyber",
            ["corona"] = "theme_card_corona",
            ["cosmo"] = "theme_card_cosmo",
            ["cosmic"] = "theme_card_cosmic",
            ["grid"] = "theme_card_grid",
            ["data"] = "theme_card_data",
            ["live"] = "theme_card_live",
            ["neo"] = "theme_card_neo",
            ["pipe"] = "theme_card_pipe",
            ["saimin"] = "theme_card_saimin",
            ["aurora"] = "theme_aurora_ribbon",
            ["suimyaku"] = "theme_card_suimyaku",
            ["ryusei"] = "theme_card_ryusei",
            ["komorebi"] = "theme_card_komorebi",
            ["holy"] = "theme_card_holy",
            ["marble"] = "theme_card_marble",
            ["energy"] = "theme_card_energy",
            ["disco"] = "theme_card_disco",
            ["tile"] = "theme_card_tile",
            ["bunkou"] = "theme_card_bunkou",
            ["toukousen"] = "theme_card_toukousen",
            ["quilt"] = "theme_card_quilt",
            ["dessert"] = "theme_dessert_game_bg",
            ["pop"] = "theme_card_pop",
            ["buddha"] = "theme_card_buddha",
        };

        public static string ResolveResourceKey(MasuThemeDefinition def)
        {
            if (def == null) return "";

            if (!string.IsNullOrEmpty(def.ThemeCardPreviewResource)
                && MasuThemeResourceCache.TryGetTexture(def.ThemeCardPreviewResource, out _))
                return def.ThemeCardPreviewResource;

            if (BuiltInKeys.TryGetValue(def.ThemeId, out var builtIn)
                && MasuThemeResourceCache.TryGetTexture(builtIn, out _))
                return builtIn;

            var auto = "theme_card_" + def.ThemeId;
            if (MasuThemeResourceCache.TryGetTexture(auto, out _))
                return auto;

            if (!string.IsNullOrEmpty(def.GameBackgroundResource)
                && MasuThemeResourceCache.TryGetTexture(def.GameBackgroundResource, out _))
                return def.GameBackgroundResource;

            if (!string.IsNullOrEmpty(def.MenuPhotoResource)
                && MasuThemeResourceCache.TryGetTexture(def.MenuPhotoResource, out _))
                return def.MenuPhotoResource;

            if (!string.IsNullOrEmpty(def.GameOverlayTextureResource)
                && MasuThemeResourceCache.TryGetTexture(def.GameOverlayTextureResource, out _))
                return def.GameOverlayTextureResource;

            return "";
        }
    }
}
