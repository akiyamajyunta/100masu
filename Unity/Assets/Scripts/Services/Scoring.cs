using Masu.Core;

namespace Masu.Services
{
    public static class Scoring
    {
        private const int ScoreBase = 10000;
        private const int ScoreMin = 100;
        private const float SpeedMinFactor = 0.25f;
        private const int SpeedTierEndSec = 600;

        public static float SpeedFactor(double elapsedSec)
        {
            var elapsed = System.Math.Max(0, elapsedSec);
            var sec = (int)elapsed;
            if (sec >= SpeedTierEndSec) return SpeedMinFactor;
            var frac = elapsed - sec;
            var f0 = FactorAtWholeSecond(sec);
            var f1 = FactorAtWholeSecond(sec + 1);
            return (float)(f0 + (f1 - f0) * frac);
        }

        private static float FactorAtWholeSecond(int second)
        {
            second = UnityEngine.Mathf.Clamp(second, 0, SpeedTierEndSec);
            var t = second / (float)SpeedTierEndSec;
            return SpeedMinFactor + (1f - SpeedMinFactor) * (1f - t);
        }

        public static float AccuracyPercent(int correct, int total) =>
            total <= 0 ? 0f : UnityEngine.Mathf.Clamp(100f * correct / total, 0f, 100f);

        public static int ComputeSessionScore(double elapsedSec, int correct, int total, Operation op)
        {
            var acc = total > 0 ? correct / (float)total : 0f;
            var mult = op switch
            {
                Operation.Add => 1.0f,
                Operation.Sub => 1.2f,
                Operation.Mul => 1.5f,
                Operation.Div => 1.8f,
                _ => 1f
            };
            var raw = ScoreBase * SpeedFactor(elapsedSec) * mult * (0.25f + 0.75f * acc);
            return UnityEngine.Mathf.Max(ScoreMin, (int)raw);
        }
    }
}
