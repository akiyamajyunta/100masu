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

        public static MasuThemePalette Phonon()
        {
            var p = Wafuu();
            p.Bg = Hex("#0d1117");
            p.MainPanel = Hex("#141a24");
            p.Text = Hex("#e8f4ff");
            p.TextDim = Hex("#6a8aa8");
            p.CursorGold = Hex("#00e5ff");
            p.BorderGold = Hex("#00c8e0");
            p.GameFrameBorder = Hex("#00e5ff");
            p.CellEmpty = Hex("#0a0e14");
            p.HeaderSlot = Hex("#121820");
            p.HeaderNum = Hex("#1c2838");
            p.PlusBg = Hex("#7b5cff");
            p.GridCellText = Hex("#f0f8ff");
            p.FocusRing = Hex("#00e5ff");
            p.FocusRingBright = Hex("#88f0ff");
            p.FocusRingDim = Hex("#0088a0");
            p.ThemeCardTint = new Color(0.06f, 0.14f, 0.22f, 0.95f);
            p.ThemeCardSelectedBorder = Hex("#00e5ff");
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
