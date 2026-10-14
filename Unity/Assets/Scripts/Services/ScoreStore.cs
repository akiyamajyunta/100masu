using System;
using System.Collections.Generic;
using System.IO;
using Masu.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Masu.Services
{
    public sealed class ScoreStore
    {
        public Dictionary<string, int> HighByDifficulty { get; } = new();
        public Dictionary<string, List<LeaderboardEntry>> Leaderboards { get; } = new();

        public static string ScoresPath
        {
            get
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".100masu", "scores.json");
            }
        }

        public static ScoreStore Load()
        {
            var store = Empty();
            if (!File.Exists(ScoresPath)) return store;
            try
            {
                var json = File.ReadAllText(ScoresPath);
                var root = JObject.Parse(json);
                if (root["high_by_difficulty"] is JObject high)
                {
                    foreach (var prop in high.Properties())
                    {
                        if (int.TryParse(prop.Value?.ToString(), out var v))
                            store.HighByDifficulty[prop.Name] = Math.Max(0, v);
                    }
                }
                if (root["leaderboards"] is JObject boards)
                {
                    foreach (var prop in boards.Properties())
                    {
                        if (prop.Value is not JArray arr) continue;
                        var list = new List<LeaderboardEntry>();
                        foreach (var item in arr)
                        {
                            var entry = ParseEntry(item as JObject);
                            if (entry != null) list.Add(entry);
                        }
                        store.Leaderboards[prop.Name] = list;
                    }
                }
            }
            catch
            {
                return Empty();
            }
            return store;
        }

        private static LeaderboardEntry ParseEntry(JObject raw)
        {
            if (raw == null) return null;
            var name = raw["name"]?.ToString()?.Trim() ?? "";
            if (!int.TryParse(raw["score"]?.ToString(), out var score)) return null;
            var playedAt = raw["played_at"]?.ToString()?.Trim() ?? "";
            float.TryParse(raw["accuracy"]?.ToString(), out var acc);
            double.TryParse(raw["elapsed_sec"]?.ToString(), out var elapsed);
            return new LeaderboardEntry
            {
                Name = name,
                Score = Math.Max(0, score),
                PlayedAt = playedAt,
                Accuracy = UnityEngine.Mathf.Clamp(acc, 0f, 100f),
                ElapsedSeconds = Math.Max(0, elapsed)
            };
        }

        public static ScoreStore Empty()
        {
            var store = new ScoreStore();
            foreach (var key in AllBoardKeys())
            {
                store.HighByDifficulty[key] = 0;
                store.Leaderboards[key] = new List<LeaderboardEntry>();
            }
            return store;
        }

        public static IEnumerable<string> AllBoardKeys()
        {
            foreach (Operation op in Enum.GetValues(typeof(Operation)))
            foreach (var g in GameConstants.GridSizes)
                yield return GridConfig.BoardKey(op, g);
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(ScoresPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var payload = new JObject
            {
                ["high_by_difficulty"] = JObject.FromObject(HighByDifficulty),
                ["leaderboards"] = new JObject()
            };
            var boardsObj = (JObject)payload["leaderboards"];
            foreach (var kv in Leaderboards)
            {
                var arr = new JArray();
                foreach (var e in kv.Value)
                {
                    arr.Add(new JObject
                    {
                        ["name"] = e.Name,
                        ["score"] = e.Score,
                        ["played_at"] = e.PlayedAt,
                        ["accuracy"] = Math.Round(e.Accuracy, 1),
                        ["elapsed_sec"] = Math.Round(e.ElapsedSeconds, 2)
                    });
                }
                boardsObj[kv.Key] = arr;
            }
            File.WriteAllText(ScoresPath, payload.ToString(Formatting.Indented));
        }

        public int HighScore(Operation op, int gridSize) =>
            HighByDifficulty.GetValueOrDefault(GridConfig.BoardKey(op, gridSize), 0);

        public int RecordSession(Operation op, int gridSize, int sessionScore)
        {
            var key = GridConfig.BoardKey(op, gridSize);
            var prev = HighByDifficulty.GetValueOrDefault(key, 0);
            var next = Math.Max(prev, sessionScore);
            if (next != prev)
            {
                HighByDifficulty[key] = next;
                Save();
            }
            return next;
        }

        public List<LeaderboardEntry> GetLeaderboard(Operation op, int gridSize) =>
            new(Leaderboards.GetValueOrDefault(GridConfig.BoardKey(op, gridSize), new List<LeaderboardEntry>()));

        public void AddLeaderboardEntry(
            Operation op,
            int gridSize,
            string name,
            int score,
            string playedAt,
            float accuracy,
            double elapsedSeconds = 0)
        {
            var key = GridConfig.BoardKey(op, gridSize);
            var current = GetLeaderboard(op, gridSize);
            Leaderboards[key] = Leaderboard.Insert(current, name, score, playedAt, accuracy, elapsedSeconds);
            Save();
        }
    }
}
