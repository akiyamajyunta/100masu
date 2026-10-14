using System;
using System.Text.RegularExpressions;

namespace Masu.Core
{
    public static class GameLogic
    {
        private static readonly Regex DivInputRe = new(@"^\d+(?:\.\d)?$", RegexOptions.Compiled);
        private static readonly Regex IntInputRe = new(@"^-?\d+$", RegexOptions.Compiled);

        public static double ComputeExpected(int a, int b, Operation operation)
        {
            return operation switch
            {
                Operation.Add => a + b,
                Operation.Sub => a - b,
                Operation.Mul => a * b,
                Operation.Div => Math.Round(a / (double)b, 1),
                _ => 0
            };
        }

        public static double? ParseAnswer(string raw, Operation operation)
        {
            var trimmed = raw.Trim();
            if (string.IsNullOrEmpty(trimmed)) return null;
            if (operation == Operation.Div)
            {
                if (!DivInputRe.IsMatch(trimmed)) return null;
                return Math.Round(double.Parse(trimmed), 1);
            }
            if (!IntInputRe.IsMatch(trimmed)) return null;
            return int.Parse(trimmed);
        }

        public static bool AnswersMatch(double expected, double parsed, Operation operation)
        {
            if (operation == Operation.Div)
                return Math.Round(parsed, 1) == expected;
            return Math.Abs(parsed - expected) < 0.0001;
        }

        public static string CanonicalValue(double parsed, Operation operation)
        {
            if (operation == Operation.Div)
                return $"{Math.Round(parsed, 1):F1}";
            return ((int)parsed).ToString();
        }

        public static string FormatTime(double seconds)
        {
            var centisTotal = (int)Math.Round(Math.Max(0, seconds) * 100);
            var minutes = centisTotal / 6000;
            var rem = centisTotal % 6000;
            var sec = rem / 100;
            var centis = rem % 100;
            return minutes > 0
                ? $"{minutes}:{sec:D2}.{centis:D2}"
                : $"0:{sec:D2}.{centis:D2}";
        }

        /// <summary>等幅タイマー表示用（桁ぶれを抑える）。</summary>
        public static string FormatTimeTabular(double seconds)
        {
            var centisTotal = (int)Math.Round(Math.Max(0, seconds) * 100);
            var minutes = Math.Min(99, centisTotal / 6000);
            var rem = centisTotal % 6000;
            var sec = rem / 100;
            var centis = rem % 100;
            return $"{minutes}:{sec:D2}.{centis:D2}";
        }
    }
}
