using System.Collections.Generic;
using System.Linq;

namespace Masu.Services
{
    public sealed class LeaderboardEntry
    {
        public string Name = "";
        public int Score;
        public string PlayedAt = "";
        public float Accuracy;

        public Dictionary<string, object> ToDict() => new()
        {
            ["name"] = Name,
            ["score"] = Score,
            ["played_at"] = PlayedAt,
            ["accuracy"] = System.Math.Round(Accuracy, 1)
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
            $"{UnityEngine.Mathf.Clamp(accuracy, 0f, 100f):F1}%";

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
            float accuracy)
        {
            var clean = (name ?? "").Trim();
            var merged = new List<LeaderboardEntry>(entries)
            {
                new LeaderboardEntry
                {
                    Name = clean,
                    Score = UnityEngine.Mathf.Max(0, score),
                    PlayedAt = playedAt ?? "",
                    Accuracy = UnityEngine.Mathf.Clamp(accuracy, 0f, 100f)
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
            float accuracy)
        {
            var clean = (name ?? "").Trim();
            var merged = Insert(entries, clean, score, playedAt, accuracy);
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
