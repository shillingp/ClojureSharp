namespace ClojureSharp.SyntaxTreeParser;

internal readonly record struct SyntaxTreeNode
{
    internal string Value { get; init; }
    internal SyntaxTreeNodeType Type { get; init; }
    internal SyntaxTreeNode[]? Children { get; init; }
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
    Comment
}