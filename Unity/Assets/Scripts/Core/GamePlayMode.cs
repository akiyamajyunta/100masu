namespace Masu.Core
{
    public enum GamePlayMode
    {
        Classic,
        Quick
    }

    public static class GamePlayModeMeta
    {
        public static string Key(this GamePlayMode mode) => mode switch
        {
            GamePlayMode.Classic => "classic",
            GamePlayMode.Quick => "quick",
            _ => "classic"
        };

        public static string LabelJa(this GamePlayMode mode) => mode switch
        {
            GamePlayMode.Classic => "クラシック",
            GamePlayMode.Quick => "クイック",
            _ => ""
        };
    }
}
