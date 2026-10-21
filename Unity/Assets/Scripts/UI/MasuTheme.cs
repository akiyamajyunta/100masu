using UnityEngine;

namespace Masu.UI
{
    /// <summary>現在アクティブなテーマ色。MasuThemeManager が <see cref="ApplyPalette"/> で更新する。</summary>
    public static class MasuTheme
    {
        private static MasuThemePalette _palette = MasuThemePalette.Wafuu();

        public static void ApplyPalette(MasuThemePalette palette) => _palette = palette;

        public static Color Bg => _palette.Bg;
        public static Color ResultBg => _palette.ResultBg;
        public static Color ResultText => _palette.ResultText;
        public static Color ResultArrow => _palette.ResultArrow;
        public static Color MainPanel => _palette.MainPanel;
        public static Color MainInner => _palette.MainInner;
        public static Color StatusInner => _palette.StatusInner;
        public static Color GaugeTimer => _palette.GaugeTimer;
        public static Color GaugeBg => _palette.GaugeBg;
        public static Color BorderDark => _palette.BorderDark;
        public static Color Text => _palette.Text;
        public static Color TextDim => _palette.TextDim;
        public static Color CursorGold => _palette.CursorGold;
        public static Color BorderGold => _palette.BorderGold;
        public static Color GameFrameBorder => _palette.GameFrameBorder;
        public static Color GameFrameInner => _palette.GameFrameInner;
        public static Color CellEmpty => _palette.CellEmpty;
        public static Color CellQuickCorrect => _palette.CellQuickCorrect;
        public static Color CellQuickWrong => _palette.CellQuickWrong;
        public static Color CellBorder => _palette.CellBorder;
        public static Color HeaderSlot => _palette.HeaderSlot;
        public static Color HeaderNum => _palette.HeaderNum;
        public static Color PlusBg => _palette.PlusBg;
        public static Color GridCellText => _palette.GridCellText;
        public static Color CorrectMark => _palette.CorrectMark;
        public static Color WrongMark => _palette.WrongMark;
        public static Color DopamineCharredBg => _palette.DopamineCharredBg;
        public static Color DopamineCharredEdge => _palette.DopamineCharredEdge;
        public static Color FocusRing => _palette.FocusRing;
        public static Color FocusRingBright => _palette.FocusRingBright;
        public static Color FocusRingDim => _palette.FocusRingDim;
        public static Color MenuTitleText => _palette.MenuTitleText;
        public static Color MenuTitleTextIdle => _palette.MenuTitleTextIdle;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
