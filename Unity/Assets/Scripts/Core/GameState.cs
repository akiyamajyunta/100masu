using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Masu.Core
{
    public sealed class GameState
    {
        public readonly GridConfig Grid = new();
        public Operation Operation = Operation.Add;
        public GamePlayMode PlayMode = GamePlayMode.Classic;
        public int[][] Header;
        public Dictionary<(int row, int col), PlayCell> Cells = new();
        public int FocusRow = GameConstants.InitialFocusRow;
        public int FocusCol = GameConstants.InitialFocusCol;
        public string Buffer = "";
        public int PenaltyCount;
        public int PenaltySeconds;
        public double? StartRealtime;
        public double? ScoringPlayElapsed;
        public bool Finished;

        public struct QuickGradeFeedback
        {
            public bool HasValue;
            public int Row;
            public int Col;
            public bool Correct;
        }

        public QuickGradeFeedback PendingQuickGrade;

        public QuickGradeFeedback TakeQuickGradeFeedback()
        {
            var ev = PendingQuickGrade;
            PendingQuickGrade = default;
            return ev;
        }

        public void Reset(bool startClock)
        {
            var gs = Grid.GridSize;
            Header = new int[gs][];
            for (var i = 0; i < gs; i++)
                Header[i] = new int[gs];

            var colCount = gs - 1;
            var top = UniqueHeaderDigits(colCount);
            var left = UniqueHeaderDigits(colCount);
            for (var c = 1; c < gs; c++) Header[0][c] = top[c - 1];
            for (var r = 1; r < gs; r++) Header[r][0] = left[r - 1];

            Cells.Clear();
            for (var r = Grid.PlayRowMin; r <= Grid.PlayRowMax; r++)
            for (var c = Grid.PlayColMin; c <= Grid.PlayColMax; c++)
                Cells[(r, c)] = new PlayCell();

            FocusRow = GameConstants.InitialFocusRow;
            FocusCol = GameConstants.InitialFocusCol;
            Buffer = "";
            PenaltyCount = 0;
            PenaltySeconds = 0;
            StartRealtime = startClock ? Time.realtimeSinceStartupAsDouble : null;
            ScoringPlayElapsed = null;
            Finished = false;
            SyncBufferFromFocus();
        }

        private static List<int> UniqueHeaderDigits(int count)
        {
            var poolMax = Math.Max(9, count);
            var pool = Enumerable.Range(1, poolMax).ToList();
            if (count <= pool.Count)
                return pool.OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();
            var head = pool.OrderBy(_ => UnityEngine.Random.value).ToList();
            var tail = new List<int>();
            for (var i = 0; i < count - pool.Count; i++)
                tail.Add(pool[UnityEngine.Random.Range(0, pool.Count)]);
            head.AddRange(tail);
            return head;
        }

        public double ExpectedAnswer(int row, int col) =>
            GameLogic.ComputeExpected(Header[row][0], Header[0][col], Operation);

        public double ElapsedSeconds()
        {
            if (ScoringPlayElapsed.HasValue)
                return ScoringPlayElapsed.Value + PenaltySeconds;
            if (!StartRealtime.HasValue) return 0;
            return Time.realtimeSinceStartupAsDouble - StartRealtime.Value + PenaltySeconds;
        }

        public void FreezeScoringClock()
        {
            ScoringPlayElapsed = StartRealtime.HasValue
                ? Math.Max(0, Time.realtimeSinceStartupAsDouble - StartRealtime.Value)
                : 0;
        }

        public bool GradeCellAtScore(int row, int col)
        {
            if (!Cells.TryGetValue((row, col), out var cell)) return false;
            if (string.IsNullOrWhiteSpace(cell.Value))
            {
                cell.Value = "";
                cell.State = CellState.Empty;
                ApplyEmptyPenalty(cell);
                return false;
            }
            ValidateCell(row, col, cell.Value);
            return cell.State == CellState.Correct;
        }

        public int CorrectCount() => Cells.Values.Count(c => c.State == CellState.Correct);

        public int WrongCount() => Cells.Values.Count(c =>
            !string.IsNullOrWhiteSpace(c.Value) && c.State != CellState.Correct);

        public int EmptyCount() => Cells.Values.Count(c => string.IsNullOrWhiteSpace(c.Value));

        public string EffectiveValue(int row, int col)
        {
            if (row == FocusRow && col == FocusCol) return Buffer;
            return Cells.TryGetValue((row, col), out var cell) ? cell.Value : "";
        }

        public int FilledCount()
        {
            var n = 0;
            for (var r = Grid.PlayRowMin; r <= Grid.PlayRowMax; r++)
            for (var c = Grid.PlayColMin; c <= Grid.PlayColMax; c++)
                if (!string.IsNullOrWhiteSpace(EffectiveValue(r, c))) n++;
            return n;
        }

        public bool AllPlayCellsFilled() => FilledCount() == Grid.PlayCellCount;

        public void Finish() => Finished = true;

        public void SyncBufferFromFocus()
        {
            Buffer = Cells.TryGetValue((FocusRow, FocusCol), out var cell) ? cell.Value : "";
        }

        public void ApplyPenalty(PlayCell cell, string value)
        {
            if (cell.LastPenalizedValue == value) return;
            cell.LastPenalizedValue = value;
            PenaltyCount++;
            PenaltySeconds += GameConstants.PenaltyWrongSeconds;
        }

        public void ApplyEmptyPenalty(PlayCell cell)
        {
            if (cell.LastPenalizedValue == GameConstants.EmptyPenaltyMark) return;
            cell.LastPenalizedValue = GameConstants.EmptyPenaltyMark;
            PenaltyCount++;
            PenaltySeconds += GameConstants.PenaltyEmptySeconds;
        }

        public void ValidateCell(int row, int col, string rawValue)
        {
            if (!Cells.TryGetValue((row, col), out var cell)) return;
            var trimmed = rawValue.Trim();
            if (trimmed == "")
            {
                cell.Value = "";
                cell.State = CellState.Empty;
                cell.LastPenalizedValue = null;
                return;
            }
            var parsed = GameLogic.ParseAnswer(trimmed, Operation);
            if (!parsed.HasValue)
            {
                ApplyPenalty(cell, trimmed);
                cell.Value = trimmed;
                cell.State = CellState.Empty;
                return;
            }
            var expected = ExpectedAnswer(row, col);
            if (GameLogic.AnswersMatch(expected, parsed.Value, Operation))
            {
                cell.Value = GameLogic.CanonicalValue(parsed.Value, Operation);
                cell.State = CellState.Correct;
                cell.LastPenalizedValue = null;
                return;
            }
            var canon = GameLogic.CanonicalValue(parsed.Value, Operation);
            ApplyPenalty(cell, canon);
            cell.Value = canon;
            cell.State = CellState.Empty;
        }

        public void CommitFocus()
        {
            if (!Grid.IsPlayCell(FocusRow, FocusCol)) return;
            var cell = Cells[(FocusRow, FocusCol)];
            cell.Value = Buffer.Trim();
            if (string.IsNullOrEmpty(cell.Value))
            {
                cell.State = CellState.Empty;
                cell.LastPenalizedValue = null;
            }
            SyncBufferFromFocus();
        }

        public void MoveFocus(int dr, int dc)
        {
            if (Finished) return;
            var fromRow = FocusRow;
            var fromCol = FocusCol;
            CommitFocus();
            if (PlayMode == GamePlayMode.Quick && Grid.IsPlayCell(fromRow, fromCol))
                QuickGradeOnLeave(fromRow, fromCol);

            FocusRow = Mathf.Clamp(FocusRow + dr, Grid.PlayRowMin, Grid.PlayRowMax);
            FocusCol = Mathf.Clamp(FocusCol + dc, Grid.PlayColMin, Grid.PlayColMax);
            if (PlayMode == GamePlayMode.Quick && Grid.IsPlayCell(FocusRow, FocusCol))
                PrepareQuickCellForEdit(FocusRow, FocusCol);
            SyncBufferFromFocus();
        }

        public bool IsQuickClear() =>
            PlayMode == GamePlayMode.Quick &&
            CorrectCount() == Grid.PlayCellCount;

        private void QuickGradeOnLeave(int row, int col)
        {
            PendingQuickGrade = default;
            if (!Cells.TryGetValue((row, col), out var cell)) return;
            if (cell.State == CellState.Correct) return;
            var trimmed = cell.Value.Trim();
            if (trimmed == "") return;
            ValidateCell(row, col, trimmed);
            PendingQuickGrade = new QuickGradeFeedback
            {
                HasValue = true,
                Row = row,
                Col = col,
                Correct = cell.State == CellState.Correct
            };
        }

        private void PrepareQuickCellForEdit(int row, int col)
        {
            if (!Cells.TryGetValue((row, col), out var cell)) return;
            cell.LastPenalizedValue = null;
        }

        private void ClearQuickCorrectIfEditing(int row, int col)
        {
            if (PlayMode != GamePlayMode.Quick) return;
            if (!Cells.TryGetValue((row, col), out var cell)) return;
            if (cell.State != CellState.Correct) return;
            cell.State = CellState.Empty;
            cell.LastPenalizedValue = null;
        }

        public void HandleChar(char ch)
        {
            if (Finished || !Grid.IsPlayCell(FocusRow, FocusCol)) return;
            ClearQuickCorrectIfEditing(FocusRow, FocusCol);
            if (Operation == Operation.Sub && ch == '-' && Buffer == "")
            {
                Buffer = "-";
                return;
            }
            if (Operation == Operation.Div && ch == '.')
            {
                if (Buffer.Contains('.')) return;
                Buffer = string.IsNullOrEmpty(Buffer) ? "0." : Buffer + ".";
                return;
            }
            if (!char.IsDigit(ch)) return;
            var candidate = Buffer + ch;
            if (!BufferWouldBeValid(candidate) || !BufferWithinRange(candidate)) return;
            Buffer = candidate;
        }

        public void HandleBackspace()
        {
            if (Finished || !Grid.IsPlayCell(FocusRow, FocusCol)) return;
            ClearQuickCorrectIfEditing(FocusRow, FocusCol);
            var cell = Cells[(FocusRow, FocusCol)];
            Buffer = Buffer.Length > 0 ? Buffer[..^1] : "";
            cell.Value = Buffer;
            if (Buffer == "")
            {
                cell.State = CellState.Empty;
                cell.LastPenalizedValue = null;
            }
        }

        private bool BufferWouldBeValid(string candidate)
        {
            if (candidate == "") return true;
            if (Operation == Operation.Div)
                return DivInputRe.IsMatch(candidate) || candidate.EndsWith(".");
            if (Operation == Operation.Sub)
                return candidate == "-" || IntInputRe.IsMatch(candidate);
            return candidate.All(char.IsDigit);
        }

        private bool BufferWithinRange(string candidate)
        {
            if (string.IsNullOrEmpty(candidate) || candidate == "-" || candidate.EndsWith("."))
                return true;
            var parsed = GameLogic.ParseAnswer(candidate, Operation);
            if (!parsed.HasValue) return false;
            return Operation switch
            {
                // ヘッダーは 1〜9 のため、四則の理論上の最大値で判定（部分入力「1」→「11」も可）
                Operation.Add => parsed >= 0 && parsed <= 18,
                Operation.Mul => parsed >= 0 && parsed <= 81,
                Operation.Sub => parsed >= -9 && parsed <= 9,
                Operation.Div => parsed >= 0 && parsed <= 9.0,
                _ => false
            };
        }

        private static readonly System.Text.RegularExpressions.Regex DivInputRe =
            new(@"^\d+(?:\.\d)?$", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex IntInputRe =
            new(@"^-?\d+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        public string PlayCellDisplay(int row, int col)
        {
            var cell = Cells[(row, col)];
            var focused = row == FocusRow && col == FocusCol && !Finished;
            if (focused && !string.IsNullOrEmpty(Buffer)) return Buffer;
            if (!string.IsNullOrEmpty(cell.Value)) return cell.Value;
            return focused ? "｜" : "";
        }
    }
}
