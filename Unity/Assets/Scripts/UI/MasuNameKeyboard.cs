using System;

namespace Masu.UI
{
    public sealed class MasuNameKeyboard
    {
        public const string KeyBack = "戻";
        public const string KeyEnd = "終";
        public const string KeySpace = "□";
        public const int GridColumns = 14;

        public static readonly string[][] Rows =
        {
            "ABCDEFGHIJKLM".ToCharArray().ArrayToStrings(),
            "NOPQRSTUVWXYZ".ToCharArray().ArrayToStrings(),
            "abcdefghijklmnopqrstuvwxyz".Substring(0, 13).ToCharArray().ArrayToStrings(),
            "abcdefghijklmnopqrstuvwxyz".Substring(13, 13).ToCharArray().ArrayToStrings(),
            new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "-", "×" },
            new[] { "÷", "□", "♡", "♢", "△", "☆", "？", "！", "＠", "『", "』", KeyBack, KeyEnd }
        };

        public int Row;
        public int Col;

        public string SelectedKey => Rows[Row][Col];

        public void Move(int dr, int dc)
        {
            if (Rows.Length == 0) return;
            var r = Row;
            var c = Col;
            if (dr != 0)
            {
                r = (r + dr + Rows.Length) % Rows.Length;
                c = Math.Min(c, Rows[r].Length - 1);
            }
            if (dc != 0)
            {
                var line = Rows[r];
                c = (c + dc + line.Length) % line.Length;
            }
            Row = r;
            Col = c;
        }

        public (string name, bool exitToMenu) ApplyKey(string name, int maxLen)
        {
            var key = SelectedKey;
            if (key == KeyEnd)
                return (name, true);
            if (key == KeyBack)
                return (name.Length > 0 ? name.Substring(0, name.Length - 1) : name, false);
            if (name.Length >= maxLen)
                return (name, false);
            if (key == KeySpace)
                return (name + " ", false);
            return (name + key, false);
        }

        public static string FormatNameSlots(string name, int maxLen)
        {
            var n = Math.Max(1, maxLen);
            var buf = (name ?? "").TrimEnd();
            if (buf.Length > n) buf = buf.Substring(0, n);
            return buf + new string('-', n - buf.Length);
        }
    }

    internal static class CharArrayExtensions
    {
        public static string[] ArrayToStrings(this char[] chars)
        {
            var a = new string[chars.Length];
            for (var i = 0; i < chars.Length; i++)
                a[i] = chars[i].ToString();
            return a;
        }
    }
}
