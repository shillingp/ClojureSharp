namespace ClojureSharp.SyntaxTreeParser;

internal readonly record struct SyntaxTreeNode
{
    internal string Value { get; init; }
    internal SyntaxTreeNodeType Type { get; init; }
    internal SyntaxTreeNode[] Children { get; init; }

    public SyntaxTreeNode()
    {
        Value = "";
        Children = new SyntaxTreeNode[0];
    }

    public override string ToString()
    {
        return $"{{Type: {Type}, Value: {Value}, Children: {Children.Length}}}";
    }
}

internal enum SyntaxTreeNodeType
{
    Namespace,
    Class,
    Method,
    MethodArgument,
    Literal,
    Expression,
    Assignment,
    EqualityCheck,
    Branch,
    Comment,
    Collection
}