namespace Masu.Core
{
    /// <summary>盤面（マス目 × クラシック/クイック）。</summary>
    public readonly struct PlayBoard
    {
        public int PlaySize { get; }
        public GamePlayMode Mode { get; }

        public PlayBoard(int playSize, GamePlayMode mode)
        {
            PlaySize = playSize;
            Mode = mode;
        }
    }

    public static class PlayBoardCatalog
    {
        /// <summary>RESULT 画面 ↑↓ の並び（クラシック全サイズ → クイック全サイズ）。</summary>
        public static readonly PlayBoard[] RankingBoards =
        {
            new(4, GamePlayMode.Classic),
            new(6, GamePlayMode.Classic),
            new(8, GamePlayMode.Classic),
            new(9, GamePlayMode.Classic),
            new(12, GamePlayMode.Classic),
            new(4, GamePlayMode.Quick),
            new(6, GamePlayMode.Quick),
            new(8, GamePlayMode.Quick),
            new(9, GamePlayMode.Quick),
            new(12, GamePlayMode.Quick),
        };

        public static string Label(in PlayBoard board) =>
            $"{board.PlaySize}×{board.PlaySize} {board.Mode.LabelJa()}";
    }
}
