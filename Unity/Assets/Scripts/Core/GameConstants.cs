namespace Masu.Core
{
    public static class GameConstants
    {
        public const int PenaltyWrongSeconds = 10;
        public const int PenaltyEmptySeconds = 10;
        public const string EmptyPenaltyMark = "__empty__";
        public const int InitialFocusRow = 1;
        public const int InitialFocusCol = 1;
        public const int StarGaugeMax = 8;

        public static readonly int[] GridSizes = { 4, 6, 8, 9, 12 };
        public const int FreePlaySizeMin = 1;
        public const int FreePlaySizeMax = 15;

        public static int GridMenuCount => GridSizes.Length + 1;

        public static bool IsFreeGridMenuIndex(int index) => index == GridSizes.Length;
    }
}
