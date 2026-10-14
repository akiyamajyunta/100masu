namespace Masu.UI
{
    /// <summary>
    /// 名前キーボードは最長行（16キー）を、英大文字2行（13キー）と同じ横幅に収める。
    /// </summary>
    public static class MasuNameKeyboardLayout
    {
        /// <summary>赤枠相当：英大文字行13キー分の見かけ幅（1280 基準）。</summary>
        public const float KeyboardWidth = 676f;
        public const float KeyFontSize = 18f;
        public const float RowStep = 38f;
        public const float TopOffset = -118f;

        public static float ColumnStep => KeyboardWidth / MasuNameKeyboard.GridColumns;
    }
}
