using System.Text;
using ClojureSharp.Extensions;
using ClojureSharp.SyntaxTreeParser;

namespace ClojureSharp.Transpiler;

internal class Transpiler(SyntaxTreeNode abstractSyntaxTree)
{

    internal string Transpile()
    {
        StringBuilder output = new StringBuilder();
        
        output.Append(ConvertAbstractSyntaxTreeToCode(abstractSyntaxTree));
        
        foreach (SyntaxTreeNode child in abstractSyntaxTree.Children ?? [])
            output.Append(ConvertAbstractSyntaxTreeToCode(child));
        
        return output.ToString();
    }

    private string ConvertAbstractSyntaxTreeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return syntaxTreeNode.Type switch
        {
            SyntaxTreeNodeType.Namespace => ConvertNamespaceSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Method => ConvertMethodSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Expression => ConvertExpressionSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Assignment => ConvertAssignmentSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.EqualityCheck => ConvertEqualityCheckSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Literal => syntaxTreeNode.Value,
            _ => throw new Exception($"Unable to convert abstract syntax tree node {syntaxTreeNode.Type} to code"),
        };
    }

    private string ConvertNamespaceSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return new StringBuilder()
            .AppendLine($"(ns {syntaxTreeNode.Value})")
            .AppendLine()
            .ToString();
    }
    
    private string ConvertMethodSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
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

        output.AppendLine(")");
        
        return output.ToString();
    }

    private string ConvertExpressionSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
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
    
    private string ConvertAssignmentSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return new StringBuilder()
            .Append("(let [")
            .Append(syntaxTreeNode.Value)
            .Append(' ')
            .Append(ConvertAbstractSyntaxTreeToCode(syntaxTreeNode.Children![0]))
            .Append("]")
            .ToString();
    }

    private string ConvertEqualityCheckSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return new StringBuilder()
            .Append("(= ")
            .AppendJoin(' ', (syntaxTreeNode.Children ?? [])
                .Select(ConvertAbstractSyntaxTreeToCode))
            .Append(')')
            .ToString();
    }
}