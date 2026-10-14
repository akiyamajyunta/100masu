using System.Collections.Generic;
using System.Linq;
using Masu.Core;

namespace Masu.Services
{
    public sealed class LeaderboardEntry
    {
        public string Name = "";
        public int Score;
        public string PlayedAt = "";
        public float Accuracy;
        public double ElapsedSeconds;

        public Dictionary<string, object> ToDict() => new()
        {
            ["name"] = Name,
            ["score"] = Score,
            ["played_at"] = PlayedAt,
            ["accuracy"] = System.Math.Round(Accuracy, 1),
            ["elapsed_sec"] = System.Math.Round(ElapsedSeconds, 2)
        };
    }

    public static class Leaderboard
    {
        public const int Size = 10;
        public const string PlaceholderName = "-";
        public const string EmptyDate = "----/--/-- --:--";
        public const int ScorePadWidth = 8;

        public static LeaderboardEntry Placeholder() =>
            new() { Name = PlaceholderName, Score = 0, PlayedAt = "", Accuracy = 0f };

        public static bool IsPlaceholder(LeaderboardEntry e) =>
            e.Name == PlaceholderName && e.Score == 0 && string.IsNullOrEmpty(e.PlayedAt);

        public static string FormatScore(int score) =>
            System.Math.Max(0, score).ToString().PadLeft(ScorePadWidth, '0');

        public static string FormatAccuracy(float accuracy) =>
            $"{UnityEngine.Mathf.Clamp(accuracy, 0f, 100f):F1}";

        public const int NameDisplayMax = 10;

        public static string DisplayName(string name)
        {
            var n = (name ?? "").Trim();
            if (n.Length <= NameDisplayMax) return n;
            return n.Substring(0, NameDisplayMax);
        }

        public static string FormatPlayedDate(string playedAt)
        {
            if (string.IsNullOrWhiteSpace(playedAt)) return "----/--/--";
            var s = playedAt.Trim();
            var sp = s.IndexOf(' ');
            return sp > 0 ? s.Substring(0, sp) : s;
        }

        public static string FormatElapsedForRow(LeaderboardEntry e)
        {
            if (IsPlaceholder(e)) return "0:00.00";
            if (e.ElapsedSeconds > 0) return GameLogic.FormatTime(e.ElapsedSeconds);
            return "-:--.--";
        }

        public static string FormatRankPrefix(int rank) => $"No{rank}.";

        public static string FormatRankingRow(int rank, LeaderboardEntry e)
        {
            var rankLabel = FormatRankPrefix(rank);
            if (IsPlaceholder(e))
                return $"{rankLabel}-  -:--.--  ----/--/--  0.0";
            var name = DisplayName(e.Name);
            return
                $"{rankLabel}{name}  {FormatElapsedForRow(e)}  {FormatPlayedDate(e.PlayedAt)}  {FormatAccuracy(e.Accuracy)}";
        }

        public static List<LeaderboardEntry> Padded(List<LeaderboardEntry> entries)
        {
            var outList = entries.Take(Size).ToList();
            while (outList.Count < Size)
                outList.Add(Placeholder());
            return outList;
        }

        public static List<LeaderboardEntry> Insert(
            List<LeaderboardEntry> entries,
            string name,
            int score,
            string playedAt,
            float accuracy,
            double elapsedSeconds = 0)
        {
            var clean = DisplayName((name ?? "").Trim());
            var merged = new List<LeaderboardEntry>(entries)
            {
                new LeaderboardEntry
                {
                    Name = clean,
                    Score = UnityEngine.Mathf.Max(0, score),
                    PlayedAt = playedAt ?? "",
                    Accuracy = UnityEngine.Mathf.Clamp(accuracy, 0f, 100f),
                    ElapsedSeconds = System.Math.Max(0, elapsedSeconds)
                }
            };
            merged.Sort((a, b) => b.Score.CompareTo(a.Score));
            return merged.Take(Size).ToList();
        }

        public static (List<LeaderboardEntry> entries, int rank) PreviewAfterInsert(
            List<LeaderboardEntry> entries,
            string name,
            int score,
            string playedAt,
            float accuracy,
            double elapsedSeconds = 0)
        {
            var clean = DisplayName((name ?? "").Trim());
            var merged = Insert(entries, clean, score, playedAt, accuracy, elapsedSeconds);
            for (var i = 0; i < merged.Count; i++)
            {
                var e = merged[i];
                if (e.Name == clean && e.Score == UnityEngine.Mathf.Max(0, score) && e.PlayedAt == playedAt)
                    return (merged, i + 1);
            }
            return (merged, UnityEngine.Mathf.Min(merged.Count, Size));
        }
    }
}
