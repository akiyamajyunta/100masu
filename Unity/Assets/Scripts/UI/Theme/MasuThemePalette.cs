using System;
using UnityEngine;

namespace Masu.UI
{
    /// <summary>テーマごとの色セット。MasuTheme の実体。</summary>
    [Serializable]
    public struct MasuThemePalette
    {
        public Color Bg;
        public Color ResultBg;
        public Color ResultText;
        public Color ResultArrow;
        public Color MainPanel;
        public Color MainInner;
        public Color StatusInner;
        public Color GaugeTimer;
        public Color GaugeBg;
        public Color BorderDark;
        public Color Text;
        public Color TextDim;
        public Color CursorGold;
        public Color BorderGold;
        public Color GameFrameBorder;
        public Color GameFrameInner;
        public Color CellEmpty;
        public Color CellQuickCorrect;
        public Color CellQuickWrong;
        public Color CellBorder;
        public Color HeaderSlot;
        public Color HeaderNum;
        public Color PlusBg;
        public Color GridCellText;
        public Color CorrectMark;
        public Color WrongMark;
        public Color DopamineCharredBg;
        public Color DopamineCharredEdge;
        public Color FocusRing;
        public Color FocusRingBright;
        public Color FocusRingDim;
        public Color MenuTitleText;
        public Color MenuTitleTextIdle;
        public Color ThemeCardTint;
        public Color ThemeCardSelectedBorder;

        public static MasuThemePalette Wafuu() => new()
        {
            Bg = Hex("#162030"),
            ResultBg = Hex("#ffffff"),
            ResultText = Hex("#1a1a1a"),
            ResultArrow = Hex("#2563eb"),
            MainPanel = Hex("#2a1620"),
            MainInner = Hex("#221e20"),
            StatusInner = Hex("#241c1e"),
            GaugeTimer = Hex("#5a8cc8"),
            GaugeBg = Hex("#141820"),
            BorderDark = Hex("#6e2a30"),
            Text = Hex("#f5e6c8"),
            TextDim = Hex("#b4a894"),
            CursorGold = Hex("#e6c85a"),
            BorderGold = Hex("#ccac4e"),
            GameFrameBorder = Color.white,
            GameFrameInner = new Color(0f, 0f, 0f, 0.4f),
            CellEmpty = Hex("#1e2838"),
            CellQuickCorrect = Hex("#1a4030"),
            CellQuickWrong = Hex("#4a2028"),
            CellBorder = Hex("#3a4558"),
            HeaderSlot = Hex("#2a3548"),
            HeaderNum = Hex("#3d4a62"),
            PlusBg = Hex("#F4B183"),
            GridCellText = Hex("#FFFFFF"),
            CorrectMark = Hex("#6ecf8a"),
            WrongMark = Hex("#e86a6a"),
            DopamineCharredBg = Hex("#0d0d0d"),
            DopamineCharredEdge = Hex("#2a1810"),
            FocusRing = Hex("#e6c85a"),
            FocusRingBright = Hex("#5cff9a"),
            FocusRingDim = Hex("#2eb86a"),
            MenuTitleText = new Color(0.06f, 0.06f, 0.06f, 1f),
            MenuTitleTextIdle = new Color(0.06f, 0.06f, 0.06f, 0.55f),
            ThemeCardTint = new Color(0.55f, 0.72f, 0.85f, 0.92f),
            ThemeCardSelectedBorder = Hex("#e6c85a"),
        };

        /// <summary>電気ブルー背景（theme_cool_game_bg）向けパレット。</summary>
        public static MasuThemePalette Cool()
        {
            var p = Wafuu();
            p.Bg = Hex("#081428");
            p.MainPanel = Hex("#0f2848");
            p.MainInner = new Color(0.04f, 0.12f, 0.28f, 0.85f);
            p.StatusInner = new Color(0.05f, 0.14f, 0.32f, 0.88f);
            p.Text = Hex("#e8f4ff");
            p.TextDim = Hex("#94c5f0");
            p.CursorGold = Hex("#7dd3fc");
            p.BorderGold = Hex("#38bdf8");
            p.GameFrameBorder = Color.white;
            p.GameFrameInner = new Color(0.02f, 0.08f, 0.2f, 0.5f);
            p.CellEmpty = new Color(0.04f, 0.14f, 0.32f, 0.78f);
            p.CellQuickCorrect = new Color(0.05f, 0.28f, 0.38f, 0.85f);
            p.CellQuickWrong = new Color(0.35f, 0.08f, 0.22f, 0.85f);
            p.HeaderSlot = new Color(0.06f, 0.2f, 0.42f, 0.82f);
            p.HeaderNum = new Color(0.12f, 0.32f, 0.55f, 0.88f);
            p.PlusBg = Hex("#38bdf8");
            p.GridCellText = Color.white;
            p.FocusRing = Hex("#bae6fd");
            p.FocusRingBright = Color.white;
            p.FocusRingDim = Hex("#0ea5e9");
            p.ThemeCardTint = new Color(0.15f, 0.42f, 0.82f, 0.92f);
            p.ThemeCardSelectedBorder = Hex("#7dd3fc");
            return p;
        }

        public static MasuThemePalette Modern()
        {
            var p = Wafuu();
            p.Bg = Hex("#0f1419");
            p.MainPanel = Hex("#1a2332");
            p.Text = Hex("#e8eef5");
            p.TextDim = Hex("#8b9aab");
            p.CursorGold = Hex("#3b82f6");
            p.BorderGold = Hex("#60a5fa");
            p.CellEmpty = Hex("#243044");
            p.HeaderSlot = Hex("#2d3a4f");
            p.HeaderNum = Hex("#4a5d78");
            p.PlusBg = Hex("#64748b");
            p.FocusRing = Hex("#60a5fa");
            p.ThemeCardTint = new Color(0.35f, 0.45f, 0.62f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#60a5fa");
            return p;
        }

        public static MasuThemePalette Cyber()
        {
            var p = Wafuu();
            p.Bg = Hex("#050810");
            p.MainPanel = Hex("#0d1520");
            p.Text = Hex("#c8fff0");
            p.TextDim = Hex("#5a9e8c");
            p.CursorGold = Hex("#00ffc8");
            p.BorderGold = Hex("#00e5b0");
            p.GameFrameBorder = Hex("#00ffc8");
            p.CellEmpty = Hex("#0a1820");
            p.HeaderSlot = Hex("#102830");
            p.HeaderNum = Hex("#1a4050");
            p.PlusBg = Hex("#ff00aa");
            p.GridCellText = Hex("#e0fff8");
            p.FocusRing = Hex("#00ffc8");
            p.FocusRingBright = Hex("#7affde");
            p.FocusRingDim = Hex("#00a080");
            p.ThemeCardTint = new Color(0.05f, 0.25f, 0.22f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#00ffc8");
            return p;
        }

        public static MasuThemePalette Cosmo()
        {
            var p = Wafuu();
            p.Bg = Hex("#060a12");
            p.MainPanel = Hex("#0c1424");
            p.Text = Hex("#e8f8ff");
            p.TextDim = Hex("#6a9eb8");
            p.CursorGold = Hex("#22e0ff");
            p.BorderGold = Hex("#1ab8e8");
            p.GameFrameBorder = Hex("#22e0ff");
            p.CellEmpty = Hex("#0a1018");
            p.HeaderSlot = Hex("#121c2e");
            p.HeaderNum = Hex("#1e3050");
            p.PlusBg = Hex("#4488ff");
            p.GridCellText = Hex("#f4fcff");
            p.FocusRing = Hex("#22e0ff");
            p.FocusRingBright = Hex("#a8f0ff");
            p.FocusRingDim = Hex("#0a7090");
            p.ThemeCardTint = new Color(0.08f, 0.22f, 0.38f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#22e0ff");
            return p;
        }

        /// <summary>Game &amp; Watch LCD 花火背景向け（白ドット／黒画面）。</summary>
        public static MasuThemePalette Cosmic()
        {
            var p = Wafuu();
            p.Bg = Hex("#000000");
            p.MainPanel = Hex("#0a0a0c");
            p.Text = Hex("#f0f0f0");
            p.TextDim = Hex("#888890");
            p.CursorGold = Hex("#ffffff");
            p.BorderGold = Hex("#cccccc");
            p.GameFrameBorder = Hex("#e8e8e8");
            p.CellEmpty = Hex("#121214");
            p.HeaderSlot = Hex("#1a1a1e");
            p.HeaderNum = Hex("#2a2a30");
            p.PlusBg = Hex("#505058");
            p.GridCellText = Hex("#fafafa");
            p.FocusRing = Hex("#ffffff");
            p.FocusRingBright = Hex("#ffffff");
            p.FocusRingDim = Hex("#707078");
            p.ThemeCardTint = new Color(0.12f, 0.12f, 0.14f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#ffffff");
            return p;
        }

        /// <summary>赤グリッド＋レインボーパルス背景向け。</summary>
        public static MasuThemePalette Grid()
        {
            var p = Wafuu();
            p.Bg = Hex("#000000");
            p.MainPanel = Hex("#120808");
            p.Text = Hex("#ffe8e8");
            p.TextDim = Hex("#a85858");
            p.CursorGold = Hex("#ef4444");
            p.BorderGold = Hex("#dc2626");
            p.GameFrameBorder = Hex("#f87171");
            p.CellEmpty = Hex("#1a0a0a");
            p.HeaderSlot = Hex("#2a1010");
            p.HeaderNum = Hex("#4a1818");
            p.PlusBg = Hex("#b91c1c");
            p.GridCellText = Hex("#fff5f5");
            p.FocusRing = Hex("#ef4444");
            p.FocusRingBright = Hex("#fca5a5");
            p.FocusRingDim = Hex("#7f1d1d");
            p.ThemeCardTint = new Color(0.18f, 0.06f, 0.06f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#ef4444");
            return p;
        }

        public static MasuThemePalette Corona()
        {
            var p = Wafuu();
            p.Bg = Hex("#0a0402");
            p.MainPanel = Hex("#1a0c06");
            p.Text = Hex("#ffeedd");
            p.TextDim = Hex("#c49a6c");
            p.CursorGold = Hex("#ffb347");
            p.BorderGold = Hex("#ff8c1a");
            p.GameFrameBorder = Hex("#ffa040");
            p.CellEmpty = Hex("#1a0e08");
            p.HeaderSlot = Hex("#2a1408");
            p.HeaderNum = Hex("#4a2810");
            p.PlusBg = Hex("#ff6b18");
            p.GridCellText = Hex("#fff0e0");
            p.FocusRing = Hex("#ffcc66");
            p.FocusRingBright = Hex("#ffe8aa");
            p.FocusRingDim = Hex("#cc6600");
            p.ThemeCardTint = new Color(0.45f, 0.18f, 0.06f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#ffb347");
            return p;
        }

        public static MasuThemePalette Buddha()
        {
            var p = Wafuu();
            p.Bg = Hex("#0c0a08");
            p.MainPanel = Hex("#1a1410");
            p.Text = Hex("#f5efe6");
            p.TextDim = Hex("#a89478");
            p.CursorGold = Hex("#d4af37");
            p.BorderGold = Hex("#8b6914");
            p.GameFrameBorder = Hex("#c9a227");
            p.CellEmpty = Hex("#14100c");
            p.HeaderSlot = Hex("#221a14");
            p.HeaderNum = Hex("#3d2e1f");
            p.PlusBg = Hex("#b8860b");
            p.GridCellText = Hex("#fff8ee");
            p.FocusRing = Hex("#e8c547");
            p.FocusRingBright = Hex("#fff0a8");
            p.FocusRingDim = Hex("#6b4f12");
            p.ThemeCardTint = new Color(0.22f, 0.16f, 0.08f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#d4af37");
            return p;
        }

        public static MasuThemePalette Neo()
        {
            var p = Wafuu();
            p.Bg = Hex("#000000");
            p.MainPanel = Hex("#081008");
            p.MainInner = new Color(0.04f, 0.1f, 0.06f, 0.9f);
            p.StatusInner = new Color(0.05f, 0.12f, 0.08f, 0.92f);
            p.Text = Hex("#d5ffdc");
            p.TextDim = Hex("#5a9870");
            p.CursorGold = Hex("#a8ffc0");
            p.BorderGold = Hex("#00cc66");
            p.GameFrameBorder = Hex("#00e878");
            p.GameFrameInner = new Color(0f, 0f, 0f, 0.52f);
            p.CellEmpty = Hex("#0a140c");
            p.CellQuickCorrect = Hex("#143828");
            p.CellQuickWrong = Hex("#381820");
            p.CellBorder = Hex("#1a3828");
            p.HeaderSlot = Hex("#0c1810");
            p.HeaderNum = Hex("#142820");
            p.PlusBg = Hex("#00a858");
            p.GridCellText = Hex("#e8fff0");
            p.FocusRing = Hex("#00e878");
            p.FocusRingBright = Hex("#d5ffdc");
            p.FocusRingDim = Hex("#007840");
            p.ThemeCardTint = new Color(0.08f, 0.22f, 0.12f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#00e878");
            return p;
        }

        public static MasuThemePalette Pipe()
        {
            var p = Wafuu();
            p.Bg = Hex("#000000");
            p.MainPanel = Hex("#121218");
            p.MainInner = new Color(0.06f, 0.06f, 0.1f, 0.9f);
            p.StatusInner = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            p.Text = Hex("#f0f4ff");
            p.TextDim = Hex("#7a88a8");
            p.CursorGold = Hex("#ffd54a");
            p.BorderGold = Hex("#40e0c8");
            p.GameFrameBorder = Hex("#5ce0ff");
            p.GameFrameInner = new Color(0f, 0f, 0f, 0.55f);
            p.CellEmpty = Hex("#141820");
            p.CellQuickCorrect = Hex("#1a4038");
            p.CellQuickWrong = Hex("#4a1830");
            p.CellBorder = Hex("#2a3848");
            p.HeaderSlot = Hex("#101820");
            p.HeaderNum = Hex("#1a2838");
            p.PlusBg = Hex("#e878c8");
            p.GridCellText = Hex("#f8fcff");
            p.FocusRing = Hex("#5ce0ff");
            p.FocusRingBright = Hex("#ffffff");
            p.FocusRingDim = Hex("#2878a8");
            p.ThemeCardTint = new Color(0.12f, 0.2f, 0.32f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#40e0c8");
            return p;
        }

        public static MasuThemePalette Saimin()
        {
            var p = Wafuu();
            p.Bg = Hex("#1e1e26");
            p.MainPanel = Hex("#2a2a34");
            p.MainInner = new Color(0.12f, 0.12f, 0.16f, 0.9f);
            p.StatusInner = new Color(0.14f, 0.14f, 0.18f, 0.92f);
            p.Text = Hex("#e8e8ee");
            p.TextDim = Hex("#8a8a96");
            p.CursorGold = Hex("#d8d8e0");
            p.BorderGold = Hex("#b4b4be");
            p.GameFrameBorder = Hex("#e0e0e8");
            p.GameFrameInner = new Color(0.1f, 0.1f, 0.14f, 0.55f);
            p.CellEmpty = new Color(0.16f, 0.16f, 0.22f, 0.88f);
            p.CellQuickCorrect = new Color(0.22f, 0.28f, 0.32f, 0.9f);
            p.CellQuickWrong = new Color(0.32f, 0.18f, 0.22f, 0.9f);
            p.HeaderSlot = new Color(0.18f, 0.18f, 0.24f, 0.9f);
            p.HeaderNum = new Color(0.28f, 0.28f, 0.36f, 0.92f);
            p.PlusBg = Hex("#9a9aa8");
            p.GridCellText = Hex("#f4f4f8");
            p.FocusRing = Hex("#e8e8f0");
            p.FocusRingBright = Color.white;
            p.FocusRingDim = Hex("#6a6a78");
            p.ThemeCardTint = new Color(0.35f, 0.35f, 0.42f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#e0e0e8");
            return p;
        }

        public static MasuThemePalette Data()
        {
            var p = Wafuu();
            p.Bg = Hex("#0a1022");
            p.MainPanel = Hex("#0e1830");
            p.MainInner = new Color(0.04f, 0.08f, 0.16f, 0.9f);
            p.StatusInner = new Color(0.05f, 0.1f, 0.2f, 0.92f);
            p.Text = Hex("#e8f0ff");
            p.TextDim = Hex("#6a88b8");
            p.CursorGold = Hex("#38e8c8");
            p.BorderGold = Hex("#28a8e8");
            p.GameFrameBorder = Hex("#38e8c8");
            p.GameFrameInner = new Color(0.04f, 0.08f, 0.16f, 0.5f);
            p.CellEmpty = new Color(0.06f, 0.12f, 0.24f, 0.85f);
            p.CellQuickCorrect = new Color(0.06f, 0.32f, 0.38f, 0.9f);
            p.CellQuickWrong = new Color(0.38f, 0.12f, 0.22f, 0.9f);
            p.HeaderSlot = new Color(0.08f, 0.16f, 0.32f, 0.88f);
            p.HeaderNum = new Color(0.12f, 0.28f, 0.48f, 0.9f);
            p.PlusBg = Hex("#ffc828");
            p.GridCellText = Hex("#f8fcff");
            p.FocusRing = Hex("#38e8c8");
            p.FocusRingBright = Hex("#a8fff0");
            p.FocusRingDim = Hex("#188878");
            p.ThemeCardTint = new Color(0.1f, 0.22f, 0.42f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#38e8c8");
            return p;
        }

        public static MasuThemePalette Live()
        {
            var p = Wafuu();
            p.Bg = Hex("#0a1022");
            p.MainPanel = Hex("#121a32");
            p.MainInner = new Color(0.05f, 0.08f, 0.18f, 0.88f);
            p.StatusInner = new Color(0.06f, 0.1f, 0.22f, 0.9f);
            p.Text = Hex("#f0f4ff");
            p.TextDim = Hex("#7a8cb8");
            p.CursorGold = Hex("#ff64a0");
            p.BorderGold = Hex("#ff40a0");
            p.GameFrameBorder = Hex("#ff50b0");
            p.GameFrameInner = new Color(0.04f, 0.06f, 0.14f, 0.45f);
            p.CellEmpty = new Color(0.06f, 0.1f, 0.22f, 0.82f);
            p.CellQuickCorrect = new Color(0.08f, 0.28f, 0.38f, 0.88f);
            p.CellQuickWrong = new Color(0.42f, 0.1f, 0.28f, 0.88f);
            p.HeaderSlot = new Color(0.08f, 0.12f, 0.28f, 0.85f);
            p.HeaderNum = new Color(0.14f, 0.2f, 0.42f, 0.9f);
            p.PlusBg = Hex("#ff64a0");
            p.GridCellText = Hex("#ffffff");
            p.FocusRing = Hex("#ff64a0");
            p.FocusRingBright = Hex("#ffc0e0");
            p.FocusRingDim = Hex("#b03078");
            p.ThemeCardTint = new Color(0.12f, 0.18f, 0.42f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#ff64a0");
            return p;
        }

        /// <summary>オーロラ GIF 背景向け（シアン／深い夜空）。</summary>
        public static MasuThemePalette Aurora()
        {
            var p = Wafuu();
            p.Bg = Hex("#0a0c10");
            p.MainPanel = Hex("#101820");
            p.Text = Hex("#e8f4f8");
            p.TextDim = Hex("#6a8898");
            p.CursorGold = Hex("#40e0d0");
            p.BorderGold = Hex("#2ab8a8");
            p.GameFrameBorder = Hex("#5eead4");
            p.CellEmpty = Hex("#121820");
            p.HeaderSlot = Hex("#1a2430");
            p.HeaderNum = Hex("#2a3848");
            p.PlusBg = Hex("#14b8a6");
            p.GridCellText = Hex("#f0faf8");
            p.FocusRing = Hex("#5eead4");
            p.FocusRingBright = Hex("#a7f3ec");
            p.FocusRingDim = Hex("#0d9488");
            p.ThemeCardTint = new Color(0.08f, 0.22f, 0.24f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#2dd4bf");
            return p;
        }

        public static MasuThemePalette Dessert()
        {
            var p = Wafuu();
            p.Bg = Hex("#f5ebe0");
            p.MainPanel = new Color(1f, 0.97f, 0.93f, 0.92f);
            p.MainInner = new Color(1f, 0.96f, 0.9f, 0.85f);
            p.StatusInner = new Color(1f, 0.94f, 0.88f, 0.88f);
            p.Text = Hex("#4a3228");
            p.TextDim = Hex("#8a6a58");
            p.CursorGold = Hex("#c87848");
            p.BorderGold = Hex("#d4a574");
            p.GameFrameBorder = Hex("#e8c8a8");
            p.GameFrameInner = new Color(0.04f, 0.03f, 0.03f, 0.82f);
            p.CellEmpty = Hex("#141210");
            p.CellQuickCorrect = Hex("#1e4a32");
            p.CellQuickWrong = Hex("#5a2028");
            p.CellBorder = Hex("#3a322c");
            p.HeaderSlot = Hex("#1a1614");
            p.HeaderNum = Hex("#282220");
            p.PlusBg = Hex("#e8a0c0");
            p.GridCellText = Hex("#f5f0ea");
            p.FocusRing = Hex("#d4a574");
            p.FocusRingBright = Hex("#ffe8d0");
            p.FocusRingDim = Hex("#a06840");
            p.ThemeCardTint = new Color(1f, 0.88f, 0.78f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#c87848");
            return p;
        }

        public static MasuThemePalette Pop()
        {
            var p = Wafuu();
            p.Bg = Hex("#fff5f8");
            p.MainPanel = Hex("#ffe4ec");
            p.Text = Hex("#2d1b2e");
            p.TextDim = Hex("#7a5a6e");
            p.CursorGold = Hex("#ff4d8d");
            p.BorderGold = Hex("#ffb347");
            p.GameFrameBorder = Hex("#ff4d8d");
            p.CellEmpty = Hex("#ffd6e8");
            p.HeaderSlot = Hex("#ffc2dd");
            p.HeaderNum = Hex("#ff9ec9");
            p.PlusBg = Hex("#ffb347");
            p.GridCellText = Hex("#2d1b2e");
            p.FocusRing = Hex("#ff4d8d");
            p.ThemeCardTint = new Color(1f, 0.75f, 0.85f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#ff4d8d");
            return p;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
