using System.Diagnostics.Contracts;
using ClojureSharp.Tokenizer;
using ClojureSharp.Extensions;
using ClojureSharp.Extensions.Array;

namespace ClojureSharp.SyntaxTreeParser;

internal static class SyntaxTreeBuilder
{
    [Pure]
    internal static SyntaxTreeNode Parse(Token[] sourceTokens)
    { 
        int currentIndex = 0;
        
        if (sourceTokens[0] is not { Type: TokenType.NamespaceToken }
            && sourceTokens[1] is not { Type: TokenType.NameIdentifierToken})
            throw new Exception("Namespace not found");
        
        List<SyntaxTreeNode> namespaceNodes = new List<SyntaxTreeNode>();
        
        while (currentIndex < sourceTokens.Length)
        {
            if (sourceTokens[currentIndex] is { Type: TokenType.TypeDeclarationToken }
                && sourceTokens[currentIndex + 1] is { Type: TokenType.NameIdentifierToken }
                && sourceTokens[currentIndex + 2] is { Type: TokenType.OpenParenthesisToken })
            {
                namespaceNodes.Add(ParseMethod(sourceTokens[currentIndex..]));
                
                currentIndex = FindIndexOfLastClosingScope(sourceTokens, currentIndex);
            }
            else if (sourceTokens[currentIndex] is { Type: TokenType.ClassToken }
                && sourceTokens[currentIndex + 1] is { Type: TokenType.NameIdentifierToken })
            {
                int endIndex = FindIndexOfLastClosingScope(sourceTokens, currentIndex);
                
                namespaceNodes.Add(new SyntaxTreeNode
                {
                    Value = sourceTokens[currentIndex+1].Value!,
                    Type = SyntaxTreeNodeType.Class,
                    Children = ParseInternalScope(sourceTokens[(currentIndex+2)..endIndex]),
                });
                
                currentIndex = endIndex;
            }
            else if (sourceTokens[currentIndex] is { Type: TokenType.NameIdentifierToken }
                && sourceTokens[currentIndex + 1] is { Type: TokenType.OpenParenthesisToken })
            {
                int semiColonIndex = currentIndex + sourceTokens[currentIndex..]
                    .IndexOf(token => token.Type == TokenType.SemicolonToken);
                
                namespaceNodes.Add(ParseExpression(sourceTokens[currentIndex..semiColonIndex]));
                currentIndex = semiColonIndex;
            }
            
            currentIndex++;
        }

        return new SyntaxTreeNode
        {
            Value = sourceTokens[1].Value!,
            Type = SyntaxTreeNodeType.Namespace,
            Children = namespaceNodes.ToArray(),
        };
    }

    [Pure]
    private static int FindIndexOfLastClosingScope(Token[] tokens, int startingPosition)
    {
        uint openScopeCount = 0;
        int? firstOpeningScope = null;

        for (int i = startingPosition; i < tokens.Length; i++)
        {
            Token token = tokens[i];
            if (token is { Type: TokenType.CloseScopeToken } && --openScopeCount == 0)
                return i;
            
            if (token is { Type: TokenType.OpenScopeToken })
            {
                firstOpeningScope ??= i + 1;
                openScopeCount++;
            }
        }

        return -1;
    }

    [Pure]
    private static Token[] RetrieveAllTokensInInnerScope(params Token[] outerScopeTokens)
    {
        uint openScopeCount = 0;
        int? firstOpeningScope = null;
        
        for (int i = 0; i < outerScopeTokens.Length; i++)
        {
            Token token = outerScopeTokens[i];
            if (token is { Type: TokenType.OpenScopeToken })
            {
                firstOpeningScope ??= i + 1;
                openScopeCount++;
            }
            else if (token is { Type: TokenType.CloseScopeToken } && --openScopeCount == 0)
                return outerScopeTokens[firstOpeningScope!.Value..(i+1)];
        }
        
        throw new Exception("Failed to find valid scope");
    }

    [Pure]
    private static SyntaxTreeNode ParseMethod(params Token[] methodTokens)
    {
        Token[] methodArgumentTokens = methodTokens
            .SkipWhile(token => token.Type is not TokenType.OpenParenthesisToken)
            .Skip(1)
            .TakeWhile(token => token.Type is not TokenType.CloseParenthesisToken)
            .ToArray();
        
        Token[] methodBodyTokens = RetrieveAllTokensInInnerScope(methodTokens);
        
        return new SyntaxTreeNode
        {
            Value = methodTokens[1].Value!,
            Type = SyntaxTreeNodeType.Method,
            Children = [
                ..ParseMethodArguments(methodArgumentTokens),
                ..ParseInternalScope(methodBodyTokens)
            ]
        };
    }
    
    [Pure]
    private static SyntaxTreeNode[] ParseMethodArguments(params Token[] methodArgumentTokens)
    {
        ArgumentException.ThrowIfNullOrEmpty("Value cannot be an empty collection.", nameof(methodArgumentTokens));
        
        SyntaxTreeNode[] argumentSyntaxTreeNodes = new SyntaxTreeNode[methodArgumentTokens.Length / 2];

        int syntaxNodeCounter = 0;
        for (int i = 1; i < methodArgumentTokens.Length; i += 3)
        {
            argumentSyntaxTreeNodes[syntaxNodeCounter++] = new SyntaxTreeNode
            {
                Value = methodArgumentTokens[i].Value!,
                Type = SyntaxTreeNodeType.MethodArgument
            };
        }

        return argumentSyntaxTreeNodes;
    }

    [Pure]
    private static SyntaxTreeNode[] ParseInternalScope(params Token[] methodBodyTokens)
    {
        ArgumentException.ThrowIfNullOrEmpty("Value cannot be an empty collection.", nameof(methodBodyTokens));
        
        List<SyntaxTreeNode> bodyNodes = new List<SyntaxTreeNode>();
        
        int tokenIndex = 0;
        while (tokenIndex < methodBodyTokens.Length)
        {
            if (methodBodyTokens[tokenIndex] is { Type: TokenType.ReturnToken or TokenType.CloseScopeToken })
            {
                tokenIndex++;
                continue;
            }
            
            if (methodBodyTokens[tokenIndex] is { Type: TokenType.CommentToken })
            {
                bodyNodes.Add(new SyntaxTreeNode
                {
                    Value = methodBodyTokens[tokenIndex].Value!,
                    Type = SyntaxTreeNodeType.Comment,
                });
                
                tokenIndex++;
                continue;
            }
            
            int indexOfScopeEnding = tokenIndex +
                methodBodyTokens[tokenIndex..methodBodyTokens.Length]
                    .IndexOf(token => token is { Type: TokenType.SemicolonToken });
            
            if (tokenIndex + methodBodyTokens[tokenIndex..]
                        .IndexOf(token => token is { Type: TokenType.OpenScopeToken }) 
                    is { } openScopeTokenIndex
                && openScopeTokenIndex > tokenIndex
                && openScopeTokenIndex < indexOfScopeEnding)
                indexOfScopeEnding = FindIndexOfLastClosingScope(methodBodyTokens, tokenIndex);
            
            if (indexOfScopeEnding == -1)
                indexOfScopeEnding = tokenIndex;
            
            bodyNodes.Add(ParseExpression(methodBodyTokens[tokenIndex..(indexOfScopeEnding+1)]));
            
            tokenIndex = indexOfScopeEnding + 1;
        }
        
        return bodyNodes
            .GroupWhile((previous, next) => previous.Type == next.Type && next.Type is SyntaxTreeNodeType.Assignment)
            .Select(treeGroup =>
            {
                SyntaxTreeNode[] syntaxTreeNodes = treeGroup as SyntaxTreeNode[] ?? treeGroup.ToArray();
                return syntaxTreeNodes is { Length: 1 }
                    ? syntaxTreeNodes[0]
                    : new SyntaxTreeNode
                    {
                        Type = SyntaxTreeNodeType.Assignment,
                        Children = syntaxTreeNodes.ToArray()
                    };
            })
            .ToArray();
    }

    [Pure]
    private static SyntaxTreeNode ParseExpression(params Token[] expressionTokens)
    {
        ArgumentException.ThrowIfNullOrEmpty("Value cannot be an empty collection.", nameof(expressionTokens));
        
        if (expressionTokens[^1] is {Type: TokenType.SemicolonToken })
            expressionTokens = expressionTokens[..^1];
        
        if (expressionTokens.Length == 1)
            return new SyntaxTreeNode
            {
                Type = SyntaxTreeNodeType.Literal,
                Value = expressionTokens[0].Value!
            };
        
        if (expressionTokens.Length > 1
            && expressionTokens[0] is { Type: TokenType.ReturnToken })
            return ParseExpression(expressionTokens[1..]);
        
        if (expressionTokens[0] is { Type: TokenType.OpenParenthesisToken }
            && Array.FindLastIndex(expressionTokens, token => token is { Type: TokenType.CloseParenthesisToken})
                is { } closeParenthesisIndex and >= 0)
            return ParseExpression(expressionTokens[1..closeParenthesisIndex]);

        if (expressionTokens[0] is { Type: TokenType.BranchingOperatorToken, Value: "if" }
            && expressionTokens.IndexOf(token => token is { Type: TokenType.CloseParenthesisToken }) is { } branchCheckParenthesisIndex and > 0)
        {
            return new SyntaxTreeNode
            {
                Value = expressionTokens[0].Value!,
                Type = SyntaxTreeNodeType.Branch,
                Children =
                [
                    ParseExpression(expressionTokens[2..branchCheckParenthesisIndex]),
                    ..ParseInternalScope(expressionTokens[(branchCheckParenthesisIndex + 2)..])
                ]
            };
        }

        if (expressionTokens[0] is { Type: TokenType.BranchingOperatorToken, Value: "else" })
        {
            return new SyntaxTreeNode
            {
                Value = expressionTokens[0].Value!,
                Type = SyntaxTreeNodeType.Branch,
                Children = ParseInternalScope(expressionTokens[2..])
            };
        }
        
        if (expressionTokens.IndexOf(token => token is { Type: TokenType.AssignmentOperatorToken }) is { } assignmentOperatorIndex and > 0
            && expressionTokens[assignmentOperatorIndex - 1] is { Type: TokenType.NameIdentifierToken} variableNameToken)
            return new SyntaxTreeNode
            {
                Value = variableNameToken.Value!,
                Type = SyntaxTreeNodeType.Assignment,
                Children = expressionTokens[assignmentOperatorIndex] is { Value: null }
                    ? [ParseExpression(expressionTokens[(assignmentOperatorIndex+1)..])]
                    : [new SyntaxTreeNode
                    {
                        Value = expressionTokens[assignmentOperatorIndex].Value![0].ToString(),
                        Type = SyntaxTreeNodeType.Expression,
                        Children = [
                            ParseExpression(expressionTokens[assignmentOperatorIndex-1]),
                            ParseExpression(expressionTokens[(assignmentOperatorIndex+1)..])
                        ]
                    }]
            };
        
        if (expressionTokens[0] is { Type: TokenType.NameIdentifierToken}
            && expressionTokens[1] is { Type: TokenType.OpenParenthesisToken}
            && expressionTokens[^1] is { Type: TokenType.CloseParenthesisToken})
            return new SyntaxTreeNode
            {
                Value = expressionTokens[0].Value!,
                Type = SyntaxTreeNodeType.Expression,
                Children = expressionTokens[2..^1]
                    .GroupWhile((prev, next) =>
                        prev is not {Type: TokenType.CommaToken}
                        && next is not {Type: TokenType.CommaToken})
                    .Where(tokenGroup => tokenGroup.First().Type is not TokenType.CommaToken)
                    .Select(group => ParseExpression(group.ToArray()))
                    .ToArray()
            };
        
        if (expressionTokens.IndexOf(token => token is {Type: TokenType.NumericOperationToken}) is { } numericOperationIndex and >= 0)
        {
            return new SyntaxTreeNode
            {
                Value = expressionTokens[1].Value!,
                Type = SyntaxTreeNodeType.Expression,
                Children = numericOperationIndex + 1 >= expressionTokens.Length
                    ? [ParseExpression(expressionTokens[..numericOperationIndex])]
                    :
                    [
                        ParseExpression(expressionTokens[..numericOperationIndex]),
                        ParseExpression(expressionTokens[(numericOperationIndex + 1)..])
                    ]
            };
        }
        
        if (expressionTokens.IndexOf(token => token is { Type: TokenType.EqualityOperatorToken }) is { } equalityIndex and >= 0)
        {
            return new SyntaxTreeNode
            {
                Type = SyntaxTreeNodeType.EqualityCheck,
                Children =
                [
                    ParseExpression(expressionTokens[..equalityIndex]),
                    ParseExpression(expressionTokens[(equalityIndex + 1)..])
                ]
            };
        }

        // if (expressionTokens[0] is { Type: TokenType.InvocationToken }
        //     && expressionTokens[1] is not { Type: TokenType.CollectionDeclarationToken })
        // {
        //     return new SyntaxTreeNode
        //     {
        //         Value = expressionTokens[1].Value!,
        //         Type = SyntaxTreeNodeType.Invocation,
        //         Children = expressionTokens[3..^1] is { Length: > 0 }
        //             ? [ParseExpression(expressionTokens[3..^1])]
        //             : []
        //     };
        // }
        //
        // if (expressionTokens[0] is { Type: TokenType.InvocationToken })
        // {
        //     return new SyntaxTreeNode
        //     {
        //         Value = "[" + string.Join(' ', expressionTokens[3..^1]
        //             .Where(token => token is not { Type: TokenType.CommaToken })
        //             .Select(child => child.Value)) + "]",
        //         Type = SyntaxTreeNodeType.Literal,
        //     };
        // }
        
        throw new Exception($"Failed to parse expression {string.Join(';', expressionTokens.Select(token => token.ToString()))}");
    }
}