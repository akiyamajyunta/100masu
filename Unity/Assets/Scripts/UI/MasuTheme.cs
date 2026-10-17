using UnityEngine;

namespace Masu.UI
{
    public static class MasuTheme
    {
        public static readonly Color Bg = Hex("#162030");
        public static readonly Color ResultBg = Hex("#ffffff");
        public static readonly Color ResultText = Hex("#1a1a1a");
        public static readonly Color ResultArrow = Hex("#2563eb");
        public static readonly Color MainPanel = Hex("#2a1620");
        public static readonly Color MainInner = Hex("#221e20");
        public static readonly Color StatusInner = Hex("#241c1e");
        public static readonly Color GaugeTimer = Hex("#5a8cc8");
        public static readonly Color GaugeBg = Hex("#141820");
        public static readonly Color BorderDark = Hex("#6e2a30");
        public static readonly Color Text = Hex("#f5e6c8");
        public static readonly Color TextDim = Hex("#b4a894");
        public static readonly Color CursorGold = Hex("#e6c85a");
        public static readonly Color BorderGold = Hex("#ccac4e");
        /// <summary>プレイ中の盤面・スコア枠（朱色）。</summary>
        public static readonly Color GameFrameBorder = Hex("#c23c3c");
        /// <summary>プレイ中枠の内側（黒 40%）。</summary>
        public static readonly Color GameFrameInner = new Color(0f, 0f, 0f, 0.4f);
        public static readonly Color CellEmpty = Hex("#1e2838");
        public static readonly Color CellQuickCorrect = Hex("#1a4030");
        public static readonly Color CellQuickWrong = Hex("#4a2028");
        public static readonly Color CellBorder = Hex("#3a4558");
        public static readonly Color HeaderSlot = Hex("#2a3548");
        public static readonly Color HeaderNum = Hex("#3d4a62");
        public static readonly Color PlusBg = Hex("#ccac4e");
        public static readonly Color CorrectMark = Hex("#6ecf8a");
        public static readonly Color WrongMark = Hex("#e86a6a");
        public static readonly Color FocusRing = Hex("#e6c85a");
        public static readonly Color FocusRingBright = Hex("#5cff9a");
        public static readonly Color FocusRingDim = Hex("#2eb86a");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
