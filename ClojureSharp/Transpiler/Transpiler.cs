using System.Diagnostics.Contracts;
using System.Text;
using ClojureSharp.SyntaxTreeParser;

namespace ClojureSharp.Transpiler;

internal class Transpiler(SyntaxTreeNode abstractSyntaxTree)
{
    [Pure]
    internal string Transpile()
    {
        StringBuilder output = new StringBuilder();
        
        output.Append(ConvertAbstractSyntaxTreeToCode(abstractSyntaxTree));
        
        foreach (SyntaxTreeNode child in abstractSyntaxTree.Children ?? [])
            output.Append(ConvertAbstractSyntaxTreeToCode(child));
        
        return output.ToString();
    }

    [Pure]
    private static string ConvertAbstractSyntaxTreeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return syntaxTreeNode.Type switch
        {
            SyntaxTreeNodeType.Namespace => ConvertNamespaceSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Method => ConvertMethodSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Expression => ConvertExpressionSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Assignment => ConvertAssignmentSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.EqualityCheck => ConvertEqualityCheckSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Literal => ConvertLiteralSyntaxTreeNodeToCode(syntaxTreeNode),
            _ => throw new Exception($"Unable to convert abstract syntax tree node {syntaxTreeNode.Type} to code"),
        };
    }

    [Pure]
    private static string ConvertNamespaceSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return new StringBuilder()
            .AppendLine($"(ns {syntaxTreeNode.Value})")
            .AppendLine()
            .ToString();
    }
    
    [Pure]
    private static string ConvertMethodSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        StringBuilder output = new StringBuilder()
            .Append("(defn ")
            .Append(syntaxTreeNode.Value)
            .Append(" [")
            .AppendJoin(' ', (syntaxTreeNode.Children?
                    .Where(token => token is { Type: SyntaxTreeNodeType.MethodArgument }) ?? [])
                .Select(token => token.Value))
            .AppendLine("]");

        foreach (SyntaxTreeNode child in syntaxTreeNode.Children?
            .Where(treeNode => treeNode is not { Type: SyntaxTreeNodeType.MethodArgument }) ?? []) 
            output.Append(ConvertAbstractSyntaxTreeToCode(child));

        return output
            .AppendLine(")")
            .AppendLine()
            .ToString();
    }

    [Pure]
    private static string ConvertExpressionSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return new StringBuilder()
            .Append('(')
            .Append(syntaxTreeNode.Value)
            .Append(' ')
            .AppendJoin(' ', (syntaxTreeNode.Children ?? [])
                .Select(ConvertAbstractSyntaxTreeToCode))
            .Append(')')
            .ToString();
    }
    
    [Pure]
    private static string ConvertAssignmentSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        StringBuilder output = new StringBuilder()
            .Append("(let [");

        if (syntaxTreeNode.Children?.Any(child => child is { Type: SyntaxTreeNodeType.Assignment}) ?? false)
        {
            output
                .AppendJoin(Environment.NewLine, syntaxTreeNode.Children
                    .Select(child => child.Value + " " + ConvertAbstractSyntaxTreeToCode(child.Children![0])));
        }
        else
        {
            output
                .Append(syntaxTreeNode.Value)
                .Append(' ')
                .Append(ConvertAbstractSyntaxTreeToCode(syntaxTreeNode.Children![0]));
        }
            
        return output
            .AppendLine("]")
            .ToString();
    }

    [Pure]
    private static string ConvertEqualityCheckSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return new StringBuilder()
            .Append("(= ")
            .AppendJoin(' ', (syntaxTreeNode.Children ?? [])
                .Select(ConvertAbstractSyntaxTreeToCode))
            .Append(')')
            .ToString();
    }
    
    [Pure]
    private static string ConvertLiteralSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return syntaxTreeNode.Value switch
        {
            "null" => "nil",
            _ => syntaxTreeNode.Value
        };
    }
}