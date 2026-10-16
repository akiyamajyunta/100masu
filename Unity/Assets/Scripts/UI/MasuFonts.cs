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

        private static readonly string[] KeyboardFontFamilies =
        {
            "Century Gothic", "Century", "Century Schoolbook", "Segoe UI", "Yu Gothic UI"
        };

        private static readonly string[] MinchoFontFamilies =
        {
            "Yu Mincho", "游明朝", "MS Mincho", "Hiragino Mincho ProN", "Yu Mincho Demibold", "MS PMincho"
        };

        private static readonly Dictionary<int, Font> Cache = new();
        private static readonly Dictionary<int, Font> MinchoCache = new();
        private static readonly Dictionary<int, Font> TimerCache = new();
        private static readonly Dictionary<int, Font> KeyboardCache = new();

        /// <summary>記録画面の日本語（明朝体E 系）。</summary>
        private static readonly string[] RankingMinchoFontFamilies =
        {
            "游明朝体E", "Yu Mincho", "游明朝", "MS Mincho", "ＭＳ 明朝", "MS PMincho", "Hiragino Mincho ProN"
        };

        /// <summary>記録画面の英数字（Century 系）。</summary>
        private static readonly string[] RankingLatinFontFamilies =
        {
            "Century", "Century Gothic", "Century Schoolbook", "Segoe UI"
        };

        private static readonly Dictionary<int, Font> RankingMinchoCache = new();
        private static readonly Dictionary<int, Font> RankingLatinCache = new();

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

        /// <summary>タイトル画面・森メニューの大見出し用（明朝）。</summary>
        public static void ApplyMincho(Text text, int fontSize, bool bold = false)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            var bucket = BucketSize(fontSize);
            if (!MinchoCache.TryGetValue(bucket, out var font) || font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(MinchoFontFamilies, bucket);
                MinchoCache[bucket] = font;
            }
            text.font = font;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            if (!string.IsNullOrEmpty(text.text))
                font.RequestCharactersInTexture(text.text, fontSize, text.fontStyle);
        }

        public static void ApplyKeyboard(Text text, int fontSize)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            var bucket = BucketSize(fontSize);
            if (!KeyboardCache.TryGetValue(bucket, out var font) || font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(KeyboardFontFamilies, bucket);
                KeyboardCache[bucket] = font;
            }
            text.font = font;
            if (!string.IsNullOrEmpty(text.text))
                font.RequestCharactersInTexture(text.text, fontSize, text.fontStyle);
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

        public static void ApplyRankingMincho(Text text, int fontSize, bool bold = false)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            var bucket = BucketSize(fontSize);
            if (!RankingMinchoCache.TryGetValue(bucket, out var font) || font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(RankingMinchoFontFamilies, bucket);
                RankingMinchoCache[bucket] = font;
            }
            text.font = font;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            if (!string.IsNullOrEmpty(text.text))
                font.RequestCharactersInTexture(text.text, fontSize, text.fontStyle);
        }

        public static void ApplyRankingLatin(Text text, int fontSize, bool bold = false)
        {
            if (text == null) return;
            text.fontSize = fontSize;
            var bucket = BucketSize(fontSize);
            if (!RankingLatinCache.TryGetValue(bucket, out var font) || font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(RankingLatinFontFamilies, bucket);
                RankingLatinCache[bucket] = font;
            }
            text.font = font;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            if (!string.IsNullOrEmpty(text.text))
                font.RequestCharactersInTexture(text.text, fontSize, text.fontStyle);
        }

        private static int BucketSize(int fontSize)
        {
            var s = Mathf.Clamp(fontSize, 10, 128);
            return Mathf.CeilToInt(s / 4f) * 4;
        }
    }
}
