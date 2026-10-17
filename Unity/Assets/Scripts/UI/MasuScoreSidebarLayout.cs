namespace Masu.UI
{
    /// <summary>右スコア枠（なぞなぞ大作戦 / Python StatusSidebar 幅 250px 基準）。</summary>
    public static class MasuScoreSidebarLayout
    {
        public const float PanelDesignWidth = 250f * 1.1f * 1.2f;
        public const float MarginRight = 16f;
        public const float GapFromMain = 8f;

        public static float ColumnSplitAnchorX =>
            1f - (PanelDesignWidth + MarginRight + GapFromMain) / MasuDisplaySettings.DesignWidth;

        public const float FrameRegionScale = 0.92f;
        /// <summary>枠の横幅（配置スロット内）。0.92×1.1 を 1.0 にクランプ。</summary>
        public const float FrameRegionScaleX = 1f;
        public const float FrameRegionScaleY = FrameRegionScale * 1.1f;
        public const float ContentPadX = 12f;

        public const int ModeFontSize = 44;
        public const int GridFontSize = 28;
        public const int StatLabelFontSize = 24;
        public const int StatValueFontSize = 26;
        public const int CorrectValueFontSize = 30;
        public const int StarFontSize = 20;
        public const int TimerFontSize = 38;
        public const float TimerBarHeight = 12f;
    }
}
