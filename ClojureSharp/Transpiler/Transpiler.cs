﻿using System.Diagnostics.Contracts;
using System.Text;
using ClojureSharp.SyntaxTreeParser;
using ClojureSharp.Extensions.Span;

namespace ClojureSharp.Transpiler;

internal static class Transpiler
{
    [Pure]
    internal static string Transpile(SyntaxTreeNode abstractSyntaxTree)
    {
        StringBuilder output = new StringBuilder();
        
        output.Append(ConvertAbstractSyntaxTreeToCode(abstractSyntaxTree));
        
        foreach (SyntaxTreeNode child in abstractSyntaxTree.Children)
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
            SyntaxTreeNodeType.EqualityCheck => ConvertExpressionSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Literal => ConvertLiteralSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Branch => ConvertBranchSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Class => ConvertClassSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Comment => ConvertCommentSyntaxTreeNodeToCode(syntaxTreeNode),
            SyntaxTreeNodeType.Collection => ConvertCollectionSyntaxTreeNodeToCode(syntaxTreeNode),
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
    private static string ConvertClassSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        throw new Exception($"Classes are not supported for {syntaxTreeNode.Value}");
        
        /*
        return new StringBuilder(
                $"""
                (gen-class
                   :name com.example.Demo
                   :state state
                   :init init
                   :prefix "{syntaxTreeNode.Value}-"
                   :main false)
                """)
            .AppendLine()
            .AppendLine($"(defn {syntaxTreeNode.Value}-init []")
            .Append("    [[] (atom {")
            .AppendJoin(' ', (syntaxTreeNode.Children ?? [])
                .Where(child => child is { Type: SyntaxTreeNodeType.Assignment })
                .Select(child => $":{child.Value} {ConvertAbstractSyntaxTreeToCode(child.Children[0])}"))
            .Append("})])")
            .AppendLine()
            .AppendLine()
            .ToString();
            */
    }
    
    [Pure]
    private static string ConvertMethodSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        StringBuilder output = new StringBuilder()
            .Append("(defn ")
            .Append(syntaxTreeNode.Value)
            .Append(" [")
            .AppendJoin(' ', syntaxTreeNode.Children
                    .Where(token => token is { Type: SyntaxTreeNodeType.MethodArgument })
                .Select(token => token.Value))
            .AppendLine("]")
            .AppendJoin(Environment.NewLine, syntaxTreeNode.Children
                    .Where(child => child is not { Type: SyntaxTreeNodeType.MethodArgument })
                .Select(ConvertAbstractSyntaxTreeToCode));

        ReadOnlySpan<char> outputCharacters = output.ToString().AsSpan();
        int numberOfMissingParenthesis = outputCharacters.Count('(') - outputCharacters.Count(')');
        
        return output
            .Append(')', numberOfMissingParenthesis)
            .AppendLine()
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
            .AppendJoin(' ', syntaxTreeNode.Children
                .Select(ConvertAbstractSyntaxTreeToCode))
            .Append(')')
            .ToString();
    }
    
    [Pure]
    private static string ConvertAssignmentSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        StringBuilder output = new StringBuilder()
            .Append("(let [");

        if (syntaxTreeNode.Children.AsSpan().IndexOf(child => child is { Type: SyntaxTreeNodeType.Assignment}) > -1)
        {
            output
                .AppendJoin(Environment.NewLine + "  ", syntaxTreeNode.Children
                    .Select(child => child.Value + ' ' + ConvertAbstractSyntaxTreeToCode(child.Children[0])));
        }
        else
        {
            output
                .Append(syntaxTreeNode.Value)
                .Append(' ')
                .Append(ConvertAbstractSyntaxTreeToCode(syntaxTreeNode.Children[0]));
        }
            
        return output
            .Append("]")
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

    [Pure]
    private static string ConvertBranchSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        StringBuilder output = new StringBuilder();
        int indexOffset = 0;
        
        if (syntaxTreeNode is { Type: SyntaxTreeNodeType.Branch, Value: "if" })
        {
            indexOffset = 1;
            output
                .Append("(if ")
                .AppendLine(ConvertAbstractSyntaxTreeToCode(syntaxTreeNode.Children[0]));
        }
        
        if (syntaxTreeNode.Children.Length == indexOffset + 1)
            output.Append(ConvertAbstractSyntaxTreeToCode(syntaxTreeNode.Children[indexOffset]));
        else
        {
            output
                .AppendLine("(do ")
                .AppendJoin(Environment.NewLine, syntaxTreeNode.Children.Skip(indexOffset)
                    .Select(ConvertAbstractSyntaxTreeToCode))
                .Append(")");
        }

        ReadOnlySpan<char> outputCharacters = output.ToString().AsSpan();
        int numberOfMissingParenthesis = outputCharacters.Count('(') - outputCharacters.Count(')');

        if (numberOfMissingParenthesis - 1 >= 0)
            output.Append(')', numberOfMissingParenthesis - 1);
        
        return output.ToString();
    }

    [Pure]
    private static string ConvertCommentSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        return syntaxTreeNode.Value.Insert(0, ";;");
    }

    private static string ConvertCollectionSyntaxTreeNodeToCode(SyntaxTreeNode syntaxTreeNode)
    {
        string collectionContents = string.Join(' ', syntaxTreeNode.Children
            .Select(ConvertAbstractSyntaxTreeToCode));
        
        return "[]".Insert(1, collectionContents);
    }
}