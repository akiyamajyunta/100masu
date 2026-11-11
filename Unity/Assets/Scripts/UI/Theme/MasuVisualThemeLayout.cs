using UnityEngine;

namespace Masu.UI
{
    public static class MasuVisualThemeLayout
    {
        public const int GridColumns = 6;
        public const float GridCellSize = 96f;
        public const float GridSpacingX = 14f;
        public const float GridSpacingY = 14f;

        /// <summary>1280×960 設計座標（テーマ名帯＋グリッドの中央揃え）。</summary>
        public const float DesignHeight = 960f;
        public const float ThemeNameBannerWidth = 440f;
        public const string ThemeNameBannerResource = "theme_visual_theme_name_banner";
        private const float ThemeNameBannerFallbackHeight = 80f;
        /// <summary>竹帯の表示高さだけ素材比率より抑える（幅は ThemeNameBannerWidth のまま）。</summary>
        public const float ThemeNameBannerHeightScale = 0.78f;
        public const float BannerVerticalGap = 8f;
        public const float BannerToGridGap = 18f;
        public const float ContentBottomReserve = 88f;
        /// <summary>テーマ名帯＋グリッド塊を上へ寄せる（設計 px）。</summary>
        public const float ContentBlockUpOffset = 52f;
        public const float ContentBlockCenterWeight = 0.36f;

        public const int SelectedNameFontSize = 48;
        public const float CardBlackBorderPx = 3f;
        public const float CardWhiteBorderPx = 3f;
        public const float CardSelectionRingOutsetPx = 3f;
        public const float BackHintBottom = 40f;
        public const int BackHintFontSize = 22;

        public static float ThemeNameBannerHeight(float width)
        {
            var tex = Resources.Load<Texture2D>("TitleArt/" + ThemeNameBannerResource);
            var natural = tex != null
                ? width * tex.height / (float)tex.width
                : ThemeNameBannerFallbackHeight * (width / ThemeNameBannerWidth);
            return natural * ThemeNameBannerHeightScale;
        }

        public static float GridHeight(int themeCount)
        {
            var rows = (themeCount + GridColumns - 1) / GridColumns;
            if (rows < 1) rows = 1;
            return rows * GridCellSize + (rows - 1) * GridSpacingY;
        }

        static float InstructionBlockBottom()
        {
            return MasuInstructionBanner.TopOffset
                   + MasuInstructionBanner.BannerHeightForWidth(MasuInstructionBanner.BannerWidth);
        }

        static float ContentBlockHeight(int themeCount)
        {
            var b2 = ThemeNameBannerHeight(ThemeNameBannerWidth);
            return b2 + BannerToGridGap + GridHeight(themeCount);
        }

        public static float ThemeNameBannerTop(int themeCount)
        {
            var instrBottom = InstructionBlockBottom();
            var available = DesignHeight - instrBottom - ContentBottomReserve;
            var blockH = ContentBlockHeight(themeCount);
            var blockTop = instrBottom
                           + System.Math.Max(0f, (available - blockH) * ContentBlockCenterWeight)
                           - ContentBlockUpOffset;
            return System.Math.Max(instrBottom + BannerVerticalGap, blockTop);
        }

        public static float GridFirstRowCenterY(int themeCount)
        {
            var nameTop = ThemeNameBannerTop(themeCount);
            var b2 = ThemeNameBannerHeight(ThemeNameBannerWidth);
            return -(nameTop + b2 + BannerToGridGap + GridCellSize * 0.5f);
        }
    }
}
