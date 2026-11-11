using System;
using System.Globalization;
using Masu.Core;

namespace Masu.Services
{
    /// <summary>1プレイ分の自己ベスト。名前の記録とは別。</summary>
    public sealed class DailyBest
    {
        public int Score;
        public double ElapsedSeconds;
        public float Accuracy;
        public string OperationKey = "";
        public int GridSize;
        public string ModeKey = "";
        public string At = "";

        public Operation Operation => OperationKey switch
        {
            "sub" => Operation.Sub,
            "mul" => Operation.Mul,
            "div" => Operation.Div,
            _ => Operation.Add
        };

        public GamePlayMode Mode =>
            ModeKey == "quick" ? GamePlayMode.Quick : GamePlayMode.Classic;

        public string BoardLabel =>
            $"{Operation.LabelJa()} {GridConfig.BoardLabel(GridSize, Mode)}";
    }

    /// <summary>暦日1日分。回数と、その日の最高得点・最短タイム。</summary>
    public sealed class DailySheet
    {
        public int Plays;
        public bool Sample;
        public DailyBest BestScore;
        public DailyBest BestTime;

        public static string KeyFor(DateTime local) => local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static bool TryParseKey(string key, out DateTime date) =>
            DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date);

        public void Record(Operation op, int gridSize, GamePlayMode mode, int score, double elapsed, float accuracy, DateTime localNow, bool keepSample = false)
        {
            if (Sample && !keepSample)
            {
                Plays = 0;
                BestScore = null;
                BestTime = null;
                Sample = false;
            }
            Plays++;
            var run = new DailyBest
            {
                Score = Math.Max(0, score),
                ElapsedSeconds = Math.Max(0, elapsed),
                Accuracy = UnityEngine.Mathf.Clamp(accuracy, 0f, 100f),
                OperationKey = op.Key(),
                GridSize = gridSize,
                ModeKey = mode.Key(),
                At = localNow.ToString("HH:mm", CultureInfo.InvariantCulture)
            };
            if (BestScore == null || IsBetterScore(run, BestScore))
                BestScore = Copy(run);
            if (BestTime == null || IsBetterTime(run, BestTime))
                BestTime = Copy(run);
        }

        static bool IsBetterScore(DailyBest run, DailyBest current)
        {
            if (run.Score != current.Score) return run.Score > current.Score;
            return run.ElapsedSeconds < current.ElapsedSeconds;
        }

        static bool IsBetterTime(DailyBest run, DailyBest current)
        {
            if (Math.Abs(run.ElapsedSeconds - current.ElapsedSeconds) > 0.01)
                return run.ElapsedSeconds < current.ElapsedSeconds;
            return run.Score > current.Score;
        }

        static DailyBest Copy(DailyBest src) => new()
        {
            Score = src.Score,
            ElapsedSeconds = src.ElapsedSeconds,
            Accuracy = src.Accuracy,
            OperationKey = src.OperationKey,
            GridSize = src.GridSize,
            ModeKey = src.ModeKey,
            At = src.At
        };
    }
}
