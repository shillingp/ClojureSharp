namespace ClojureSharp.SyntaxTreeParser;

internal record struct SyntaxTreeNode
{
    internal string Value;
    internal SyntaxTreeNodeType Type;
    internal SyntaxTreeNode[]? Children;
}

internal enum SyntaxTreeNodeType
{
    Namespace,
    Method,
    MethodArgument,
    Literal,
    Expression,
    Assignment,
    EqualityCheck
}