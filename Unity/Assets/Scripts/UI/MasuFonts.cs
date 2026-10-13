using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// Windows の日本語 OS フォント（uGUI Text 用。TMP の LiberationSans では日本語不可）。
    /// </summary>
    public static class MasuFonts
    {
        private static Font _uiFont;

        public static Font UiFont
        {
            get
            {
                if (_uiFont != null) return _uiFont;
                _uiFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo", "MS Gothic", "Segoe UI" },
                    36);
                return _uiFont;
            }
        }

        public static void Apply(Text text)
        {
            if (text == null) return;
            var font = UiFont;
            if (font != null) text.font = font;
        }
    }
}
