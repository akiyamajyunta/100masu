using UnityEngine;

namespace Masu.UI
{
    public static class MasuTheme
    {
        public static readonly Color Bg = Hex("#162030");
        public static readonly Color MainPanel = Hex("#2a1620");
        public static readonly Color Text = Hex("#f5e6c8");
        public static readonly Color TextDim = Hex("#b4a894");
        public static readonly Color CursorGold = Hex("#e6c85a");
        public static readonly Color BorderGold = Hex("#ccac4e");
        public static readonly Color CellEmpty = Hex("#1e2838");
        public static readonly Color CellBorder = Hex("#3a4558");
        public static readonly Color HeaderSlot = Hex("#2a3548");
        public static readonly Color HeaderNum = Hex("#3d4a62");
        public static readonly Color PlusBg = Hex("#ccac4e");
        public static readonly Color CorrectMark = Hex("#6ecf8a");
        public static readonly Color WrongMark = Hex("#e86a6a");
        public static readonly Color FocusRing = Hex("#e6c85a");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
