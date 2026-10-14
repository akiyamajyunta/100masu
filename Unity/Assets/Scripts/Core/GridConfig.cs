using System.Collections.Generic;
using UnityEngine;

namespace Masu.Core
{
    public sealed class GridConfig
    {
        public int PlaySize { get; private set; } = 4;
        public int GridSize => PlaySize + 1;
        public int PlayRowMin => 1;
        public int PlayColMin => 1;
        public int PlayRowMax => PlaySize;
        public int PlayColMax => PlaySize;
        public int PlayCellCount => PlaySize * PlaySize;

        public void Configure(int playSize)
        {
            PlaySize = Mathf.Clamp(playSize, 4, 12);
        }

        public bool IsPlayCell(int row, int col) =>
            row >= PlayRowMin && row <= PlayRowMax &&
            col >= PlayColMin && col <= PlayColMax;

        public List<(int row, int col)> HeaderRevealCells()
        {
            var cells = new List<(int, int)>();
            for (var row = 1; row < GridSize; row++)
                cells.Add((row, 0));
            for (var col = 1; col < GridSize; col++)
                cells.Add((0, col));
            return cells;
        }

        public List<(int row, int col)> PlayCellsScoreOrder()
        {
            var cells = new List<(int, int)>();
            for (var col = PlayColMin; col <= PlayColMax; col++)
            for (var row = PlayRowMin; row <= PlayRowMax; row++)
                cells.Add((row, col));
            return cells;
        }

        public static string GridLabel(int playSize) => $"{playSize}×{playSize}";

        public static string BoardLabel(int playSize, GamePlayMode mode) =>
            $"{GridLabel(playSize)} {mode.LabelJa()}";

        public static string BoardKey(Operation op, int playSize, GamePlayMode mode) =>
            $"{op.Key()}:{playSize}:{mode.Key()}";

        /// <summary>旧 scores.json（モードなし）用。</summary>
        public static string LegacyBoardKey(Operation op, int playSize) =>
            $"{op.Key()}:{playSize}";
    }
}
