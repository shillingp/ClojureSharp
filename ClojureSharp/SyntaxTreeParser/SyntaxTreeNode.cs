using System.ComponentModel;

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