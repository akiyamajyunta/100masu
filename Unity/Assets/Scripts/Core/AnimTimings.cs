using System;
using UnityEngine;

namespace Masu.Core
{
    public static class AnimTimings
    {
        private static readonly float[] IntroScaleLevels = { 1.85f, 1.55f, 1.28f, 1.1f, 1.0f };
        private static readonly float[] ScoreScaleLevels = { 1.85f, 1.55f, 1.28f, 1.1f, 1.0f };

        private const int IntroScaleMs = 20;
        private const int IntroStaggerMs = 30;
        private const int IntroScaleMsMin = 10;
        private const int IntroStaggerMsMin = 8;
        private const float IntroAnimTargetSec = 1.2f;
        private const int IntroRefHeaderCount = 8;
        private const int IntroMaxHeaderCount = 24;

        private const int ScoreScaleMs = 20;
        private const int ScoreStaggerMs = 30;
        private const int ScoreScaleMsMin = 6;
        private const int ScoreStaggerMsMin = 4;
        private const float ScoreAnimTargetSec = 4.0f;
        private const int ScoreRefCellCount = 16;
        private const int ScoreMaxCellCount = 144;

        public static (int scaleMs, int staggerMs) IntroAnimTimings(int headerCount) =>
            AnimMsFromTimeBudget(
                headerCount,
                IntroScaleLevels.Length,
                IntroScaleMs,
                IntroScaleMsMin,
                IntroStaggerMs,
                IntroStaggerMsMin,
                IntroAnimTargetSec,
                IntroRefHeaderCount,
                IntroMaxHeaderCount);

        public static (int scaleMs, int staggerMs) ScoreAnimTimings(int playCellCount) =>
            AnimMsFromTimeBudget(
                playCellCount,
                ScoreScaleLevels.Length,
                ScoreScaleMs,
                ScoreScaleMsMin,
                ScoreStaggerMs,
                ScoreStaggerMsMin,
                ScoreAnimTargetSec,
                ScoreRefCellCount,
                ScoreMaxCellCount);

        private static (int scaleMs, int staggerMs) AnimMsFromTimeBudget(
            int cellCount,
            int scaleLevelCount,
            int baseScaleMs,
            int minScaleMs,
            int baseStaggerMs,
            int minStaggerMs,
            float targetSec,
            int refCount,
            int maxCount)
        {
            var n = Math.Max(1, cellCount);
            var scaleMs = AnimScaleMsForCount(baseScaleMs, minScaleMs, n, refCount, maxCount);
            var staggerMs = AnimScaleMsForCount(baseStaggerMs, minStaggerMs, n, refCount, maxCount);
            var popMs = scaleLevelCount * scaleMs;
            var budgetMs = targetSec * 1000;
            var totalMs = n * (popMs + staggerMs);
            if (totalMs <= budgetMs)
                return (scaleMs, staggerMs);

            var perCellMs = budgetMs / n;
            if (perCellMs <= popMs)
            {
                scaleMs = Math.Max(minScaleMs, (int)(perCellMs / Math.Max(scaleLevelCount, 1)));
                popMs = scaleLevelCount * scaleMs;
            }
            staggerMs = Math.Max(minStaggerMs, (int)(perCellMs - popMs));
            return (scaleMs, staggerMs);
        }

        private static int AnimScaleMsForCount(
            int baseMs,
            int minMs,
            int count,
            int refCount,
            int maxCount)
        {
            if (count <= refCount) return baseMs;
            var span = Math.Max(1, maxCount - refCount);
            var t = Math.Min(1.0, (count - refCount) / (double)span);
            var scaled = (int)Math.Round(baseMs + (minMs - baseMs) * t);
            return Math.Max(minMs, scaled);
        }

        public static float[] IntroPopScales() => IntroScaleLevels;
        public static float[] ScorePopScales() => ScoreScaleLevels;
    }
}
