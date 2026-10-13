namespace Masu.Core
{
    public enum Operation
    {
        Add,
        Sub,
        Mul,
        Div
    }

    public static class OperationMeta
    {
        public static string Key(this Operation op) => op switch
        {
            Operation.Add => "add",
            Operation.Sub => "sub",
            Operation.Mul => "mul",
            Operation.Div => "div",
            _ => "add"
        };

        public static string LabelJa(this Operation op) => op switch
        {
            Operation.Add => "たし算",
            Operation.Sub => "ひき算",
            Operation.Mul => "かけ算",
            Operation.Div => "わり算",
            _ => ""
        };

        public static string Symbol(this Operation op) => op switch
        {
            Operation.Add => "＋",
            Operation.Sub => "−",
            Operation.Mul => "×",
            Operation.Div => "÷",
            _ => ""
        };
    }
}
