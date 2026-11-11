using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Masu.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Masu.Services
{
    public sealed class ScoreStore
    {
        public Dictionary<string, int> HighByDifficulty { get; } = new();
        public Dictionary<string, List<LeaderboardEntry>> Leaderboards { get; } = new();
        public Dictionary<string, DailySheet> Daily { get; } = new();

        public static string ScoresPath
        {
            get
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".100masu", "scores.json");
            }
        }

        private static string ScoresBackupPath => ScoresPath + ".bak";

        public static ScoreStore Load()
        {
            if (!File.Exists(ScoresPath) && !File.Exists(ScoresBackupPath))
                return Empty();

            if (TryLoadFromFile(ScoresPath, out var store))
            {
                TryWriteBackup(ScoresPath);
                FinalizeLoadedStore(store);
                return store;
            }

            if (TryLoadFromFile(ScoresBackupPath, out store))
            {
                Debug.LogWarning("[100masu] scores.json の読み込みに失敗したため scores.json.bak から復元しました。");
                FinalizeLoadedStore(store);
                return store;
            }

            ArchiveCorruptScoreFiles();
            Debug.LogWarning("[100masu] スコアファイルを読み込めませんでした。記録を初期化します。");
            return Empty();
        }

        private static void ArchiveCorruptScoreFiles()
        {
            try
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (File.Exists(ScoresPath))
                    File.Move(ScoresPath, ScoresPath + $".corrupt.{stamp}");
                if (File.Exists(ScoresBackupPath))
                    File.Move(ScoresBackupPath, ScoresBackupPath + $".corrupt.{stamp}");
            }
            catch
            {
                // ignore archive failures
            }
        }

        private static bool TryLoadFromFile(string path, out ScoreStore store)
        {
            store = null;
            if (!File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                var root = JObject.Parse(json);
                store = new ScoreStore();
                EnsureAllBoardKeys(store);
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
                if (root["daily"] is JObject daily)
                {
                    foreach (var prop in daily.Properties())
                    {
                        if (!DailySheet.TryParseKey(prop.Name, out _)) continue;
                        if (prop.Value is not JObject day) continue;
                        var sheet = ParseDailySheet(day);
                        if (sheet != null)
                            store.Daily[prop.Name] = sheet;
                    }
                }
                return true;
            }
            catch
            {
                store = null;
                return false;
            }
        }

        private static void TryWriteBackup(string sourcePath)
        {
            try
            {
                if (!File.Exists(sourcePath)) return;
                File.Copy(sourcePath, ScoresBackupPath, overwrite: true);
            }
            catch
            {
                // ignore backup failures
            }
        }

        private static void FinalizeLoadedStore(ScoreStore store)
        {
            MigrateLegacyBoardKeys(store);
            PurgeLegacyBoardKeys(store);
            EnsureAllBoardKeys(store);
        }

        private static void EnsureAllBoardKeys(ScoreStore store)
        {
            foreach (var key in AllBoardKeys())
            {
                if (!store.HighByDifficulty.ContainsKey(key))
                    store.HighByDifficulty[key] = 0;
                if (!store.Leaderboards.ContainsKey(key))
                    store.Leaderboards[key] = new List<LeaderboardEntry>();
            }
        }

        private static void MigrateLegacyBoardKeys(ScoreStore store)
        {
            var legacyHigh = new List<KeyValuePair<string, int>>(store.HighByDifficulty);
            foreach (var kv in legacyHigh)
            {
                if (!TryParseLegacyBoardKey(kv.Key, out var op, out var size)) continue;
                var modern = GridConfig.BoardKey(op, size, GamePlayMode.Classic);
                if (!store.HighByDifficulty.ContainsKey(modern))
                    store.HighByDifficulty[modern] = kv.Value;
            }

            var legacyBoards = new List<KeyValuePair<string, List<LeaderboardEntry>>>(store.Leaderboards);
            foreach (var kv in legacyBoards)
            {
                if (!TryParseLegacyBoardKey(kv.Key, out var op, out var size)) continue;
                var modern = GridConfig.BoardKey(op, size, GamePlayMode.Classic);
                if (!store.Leaderboards.ContainsKey(modern))
                    store.Leaderboards[modern] = new List<LeaderboardEntry>(kv.Value);
            }
        }

        private static void PurgeLegacyBoardKeys(ScoreStore store)
        {
            foreach (var key in store.HighByDifficulty.Keys.ToList())
            {
                if (TryParseLegacyBoardKey(key, out _, out _))
                    store.HighByDifficulty.Remove(key);
            }
            foreach (var key in store.Leaderboards.Keys.ToList())
            {
                if (TryParseLegacyBoardKey(key, out _, out _))
                    store.Leaderboards.Remove(key);
            }
        }

        private static bool TryParseLegacyBoardKey(string key, out Operation op, out int playSize)
        {
            op = Operation.Add;
            playSize = 0;
            if (string.IsNullOrEmpty(key)) return false;
            var parts = key.Split(':');
            if (parts.Length != 2) return false;
            if (parts[0] is not ("add" or "sub" or "mul" or "div")) return false;
            op = parts[0] switch
            {
                "add" => Operation.Add,
                "sub" => Operation.Sub,
                "mul" => Operation.Mul,
                "div" => Operation.Div,
                _ => Operation.Add
            };
            return int.TryParse(parts[1], out playSize);
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
            EnsureAllBoardKeys(store);
            return store;
        }

        public static IEnumerable<string> AllBoardKeys()
        {
            foreach (Operation op in Enum.GetValues(typeof(Operation)))
            foreach (var size in GameConstants.GridSizes)
            foreach (GamePlayMode mode in Enum.GetValues(typeof(GamePlayMode)))
                yield return GridConfig.BoardKey(op, size, mode);
        }

        public void Save()
        {
            PurgeLegacyBoardKeys(this);
            var dir = Path.GetDirectoryName(ScoresPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var highObj = new JObject();
            var boardsObj = new JObject();
            foreach (var key in AllBoardKeys())
            {
                highObj[key] = HighByDifficulty.GetValueOrDefault(key, 0);
                var arr = new JArray();
                if (Leaderboards.TryGetValue(key, out var list))
                {
                    foreach (var e in list)
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
                }
                boardsObj[key] = arr;
            }

            var dailyObj = new JObject();
            foreach (var kv in Daily)
            {
                if (kv.Value == null || kv.Value.Plays <= 0) continue;
                dailyObj[kv.Key] = WriteDailySheet(kv.Value);
            }

            var payload = new JObject
            {
                ["high_by_difficulty"] = highObj,
                ["leaderboards"] = boardsObj,
                ["daily"] = dailyObj
            };
            File.WriteAllText(ScoresPath, payload.ToString(Formatting.Indented));
            TryWriteBackup(ScoresPath);
        }

        public int HighScore(Operation op, int gridSize, GamePlayMode mode) =>
            HighByDifficulty.GetValueOrDefault(GridConfig.BoardKey(op, gridSize, mode), 0);

        public int RecordSession(Operation op, int gridSize, GamePlayMode mode, int sessionScore)
        {
            var key = GridConfig.BoardKey(op, gridSize, mode);
            var prev = HighByDifficulty.GetValueOrDefault(key, 0);
            var next = Math.Max(prev, sessionScore);
            if (next != prev)
            {
                HighByDifficulty[key] = next;
                Save();
            }
            return next;
        }

        public DailySheet GetDaily(DateTime localDate)
        {
            var key = DailySheet.KeyFor(localDate.Date);
            return Daily.TryGetValue(key, out var sheet) ? sheet : null;
        }

        /// <summary>採点完了のたびに、その日の回数と最高記録を残す。名前の記録とは別。</summary>
        public void RecordDaily(Operation op, int gridSize, GamePlayMode mode, int score, double elapsed, float accuracy)
        {
            var now = DateTime.Now;
            var key = DailySheet.KeyFor(now);
            if (!Daily.TryGetValue(key, out var sheet) || sheet == null)
            {
                sheet = new DailySheet();
                Daily[key] = sheet;
            }
            sheet.Record(op, gridSize, mode, score, elapsed, accuracy, now);
            Save();
        }

        static DailySheet ParseDailySheet(JObject raw)
        {
            if (raw == null) return null;
            var sheet = new DailySheet();
            int.TryParse(raw["plays"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out sheet.Plays);
            sheet.Plays = Math.Max(0, sheet.Plays);
            sheet.Sample = raw["sample"]?.Value<bool>() ?? false;
            sheet.BestScore = ParseDailyBest(raw["best_score"] as JObject);
            sheet.BestTime = ParseDailyBest(raw["best_time"] as JObject);
            if (sheet.Plays <= 0 && sheet.BestScore == null && sheet.BestTime == null) return null;
            return sheet;
        }

        static DailyBest ParseDailyBest(JObject raw)
        {
            if (raw == null) return null;
            if (!int.TryParse(raw["score"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var score))
                return null;
            double.TryParse(raw["elapsed_sec"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var elapsed);
            float.TryParse(raw["accuracy"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var acc);
            int.TryParse(raw["grid"]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var grid);
            return new DailyBest
            {
                Score = Math.Max(0, score),
                ElapsedSeconds = Math.Max(0, elapsed),
                Accuracy = UnityEngine.Mathf.Clamp(acc, 0f, 100f),
                OperationKey = raw["op"]?.ToString() ?? "add",
                GridSize = grid,
                ModeKey = raw["mode"]?.ToString() ?? "classic",
                At = raw["at"]?.ToString() ?? ""
            };
        }

        static JObject WriteDailySheet(DailySheet sheet)
        {
            var obj = new JObject
            {
                ["plays"] = sheet.Plays,
                ["sample"] = sheet.Sample
            };
            if (sheet.BestScore != null) obj["best_score"] = WriteDailyBest(sheet.BestScore);
            if (sheet.BestTime != null) obj["best_time"] = WriteDailyBest(sheet.BestTime);
            return obj;
        }

        static JObject WriteDailyBest(DailyBest best) => new()
        {
            ["score"] = best.Score,
            ["elapsed_sec"] = Math.Round(best.ElapsedSeconds, 2),
            ["accuracy"] = Math.Round(best.Accuracy, 1),
            ["op"] = best.OperationKey,
            ["grid"] = best.GridSize,
            ["mode"] = best.ModeKey,
            ["at"] = best.At
        };

        public List<LeaderboardEntry> GetLeaderboard(Operation op, int gridSize, GamePlayMode mode) =>
            new(Leaderboards.GetValueOrDefault(GridConfig.BoardKey(op, gridSize, mode), new List<LeaderboardEntry>()));

        public void AddLeaderboardEntry(
            Operation op,
            int gridSize,
            GamePlayMode mode,
            string name,
            int score,
            string playedAt,
            float accuracy,
            double elapsedSeconds = 0)
        {
            var key = GridConfig.BoardKey(op, gridSize, mode);
            var current = GetLeaderboard(op, gridSize, mode);
            Leaderboards[key] = Leaderboard.Insert(current, name, score, playedAt, accuracy, elapsedSeconds);
            Save();
        }
    }
}
