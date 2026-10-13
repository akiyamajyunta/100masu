namespace Masu.Core
{
    public static class GameConstants
    {
        public const int PenaltyWrongSeconds = 10;
        public const int PenaltyEmptySeconds = 20;
        public const string EmptyPenaltyMark = "__empty__";
        public const int InitialFocusRow = 1;
        public const int InitialFocusCol = 1;
        public const int StarGaugeMax = 8;

        public static readonly int[] GridSizes = { 4, 6, 8, 9, 12 };
    }
}
