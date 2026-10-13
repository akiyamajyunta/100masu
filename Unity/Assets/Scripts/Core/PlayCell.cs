namespace Masu.Core
{
    public enum CellState
    {
        Empty,
        Correct
    }

    public sealed class PlayCell
    {
        public string Value = "";
        public CellState State = CellState.Empty;
        public string LastPenalizedValue;
    }
}
