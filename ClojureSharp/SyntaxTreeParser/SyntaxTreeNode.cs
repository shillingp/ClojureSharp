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
    Class,
    Method,
    MethodArgument,
    Literal,
    Expression,
    Assignment,
    EqualityCheck,
    Branch
}