using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// Windows の日本語 OS フォント（uGUI Text 用。TMP の LiberationSans では日本語不可）。
    /// 表示サイズごとに Dynamic Font を用意し、fontSize 差が画面上でもはっきり出るようにする。
    /// </summary>
    public static class MasuFonts
    {
        private static readonly string[] FontFamilies =
        {
            "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo", "MS Gothic", "Segoe UI"
        };

        private static readonly string[] TimerFontFamilies =
        {
            "Consolas", "Courier New", "MS Gothic", "Yu Gothic UI"
        };

        private static readonly Dictionary<int, Font> Cache = new();
        private static readonly Dictionary<int, Font> TimerCache = new();

        public static Font UiFont => GetForSize(36);

        public static Font GetForSize(int fontSize)
        {
            var bucket = BucketSize(fontSize);
            if (Cache.TryGetValue(bucket, out var font) && font != null)
                return font;
            font = Font.CreateDynamicFontFromOSFont(FontFamilies, bucket);
            Cache[bucket] = font;
            return font;
        }

        public static void Apply(Text text, int? fontSize = null)
        {
            if (text == null) return;
            if (fontSize.HasValue)
                text.fontSize = fontSize.Value;
            var size = text.fontSize;
            text.font = GetForSize(size);
            if (!string.IsNullOrEmpty(text.text))
                text.font.RequestCharactersInTexture(text.text, size, text.fontStyle);
        }

        public static void ApplyTimer(Text text, int fontSize)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            var bucket = BucketSize(fontSize);
            if (!TimerCache.TryGetValue(bucket, out var font) || font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(TimerFontFamilies, bucket);
                TimerCache[bucket] = font;
            }
            text.font = font;
            text.resizeTextForBestFit = false;
            const string timerGlyphs = "0123456789:. ";
            font.RequestCharactersInTexture(timerGlyphs, fontSize, text.fontStyle);
        }

        private static int BucketSize(int fontSize)
        {
            var s = Mathf.Clamp(fontSize, 10, 72);
            return Mathf.CeilToInt(s / 4f) * 4;
        }
    }
}
