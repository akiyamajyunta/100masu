namespace Masu.UI
{
    /// <summary>
    /// 名前キーボードは最長行（16キー）を、英大文字2行（13キー）と同じ横幅に収める。
    /// </summary>
    public static class MasuNameKeyboardLayout
    {
        /// <summary>赤枠相当：英大文字行13キー分の見かけ幅（1280 基準）。</summary>
        public const float KeyboardWidth = 532f;
        public const float KeyFontSize = 36f;
        public const float RowStep = 34f;
        /// <summary>画面下端から最下段キー行までの余白。</summary>
        public const float BottomMargin = 100f;
        /// <summary>スライドイン/アウト時に画面外へ逃がす距離（キーボード全高より大きく）。</summary>
        public const float SlideHiddenOffset = 380f;
        public const float SlideInSeconds = 0.32f;
        public const float SlideOutSeconds = 0.26f;

        public static float ColumnStep => KeyboardWidth / MasuNameKeyboard.GridColumns;
    }
}
