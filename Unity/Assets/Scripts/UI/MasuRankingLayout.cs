namespace Masu.UI
{
    public static class MasuRankingLayout
    {
        public const int TitleFontSize = 72;
        public const int OperationTabFontSize = 38;
        public const int GridSizeFontSize = 40;
        public const int GridModeFontSize = 36;
        public const int GridArrowFontSize = 44;
        public const int RowFontSize = 36;
        public const int HintFontSize = 24;

        public const float RowLineHeight = 44f;
        public const float RankingRowHeight = 40f;
        /// <summary>竹の説明帯と記録本体の間隔（設計座標）。</summary>
        public const float BelowInstructionBanner = 68f;
        public const float OperationTabsTopY = -104f - BelowInstructionBanner;

        /// <summary>マス目選択（▲・4×4 クラシック・▼）ブロックの上端。</summary>
        public const float GridSelectorTopY = -150f - BelowInstructionBanner;
        public const float GridSelectorWidth = 440f;
        public const float GridSelectorArrowWidth = 72f;
        public const float GridSelectorArrowHeight = 40f;
        public const float GridSelectorRowHeight = 48f;
        /// <summary>三角とモード表記の間隔（上下とも同じ）。</summary>
        public const float GridSelectorVerticalGap = 8f;
        /// <summary>マス目選択ブロックと記録一覧の間隔。</summary>
        public const float GridSelectorToRowsGap = 16f;

        public static float GridSelectorBlockHeight =>
            GridSelectorArrowHeight + GridSelectorVerticalGap + GridSelectorRowHeight + GridSelectorVerticalGap
            + GridSelectorArrowHeight;

        public static float RowStartY =>
            GridSelectorTopY - GridSelectorBlockHeight - GridSelectorToRowsGap;

        /// <summary>下部操作ガイドの下端からの余白（記録一覧と重ならないよう確保）。</summary>
        public const float HintBottomInset = 48f;
        public const float HintHeight = 36f;

        public const float RowWidth = 980f;
    }
}
