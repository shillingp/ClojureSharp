using System.Buffers;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using ClojureSharp.Extensions.Span;
using ClojureSharp.Tokenizer;

namespace ClojureSharp.SyntaxTreeParser;

internal static class SyntaxTreeBuilder
{
    [Pure]
    internal static SyntaxTreeNode Parse(ReadOnlySpan<Token> sourceTokens)
    { 
        int currentIndex = 0;
        
        if (sourceTokens[0] is not { Type: TokenType.NamespaceToken }
            && sourceTokens[1] is not { Type: TokenType.NameIdentifierToken})
            throw new Exception("Namespace not found");
        
        Queue<SyntaxTreeNode> internalNodes = new Queue<SyntaxTreeNode>();
        
        while (currentIndex < sourceTokens.Length)
        {
            if (sourceTokens[currentIndex] is { Type: TokenType.TypeDeclarationToken }
                && sourceTokens[currentIndex + 1] is { Type: TokenType.NameIdentifierToken }
                && sourceTokens[currentIndex + 2] is { Type: TokenType.OpenParenthesisToken })
            {
                internalNodes.Enqueue(ParseMethod(sourceTokens[currentIndex..]));
                
                currentIndex = FindIndexOfLastClosingScope(sourceTokens, currentIndex);
            }
            else if (sourceTokens[currentIndex] is { Type: TokenType.ClassToken }
                && sourceTokens[currentIndex + 1] is { Type: TokenType.NameIdentifierToken })
            {
                int endIndex = FindIndexOfLastClosingScope(sourceTokens, currentIndex);
                
                internalNodes.Enqueue(new SyntaxTreeNode
                {
                    Value = sourceTokens[currentIndex+1].Value!,
                    Type = SyntaxTreeNodeType.Class,
                    Children = ParseInternalScope(sourceTokens[(currentIndex+2)..endIndex]).ToArray(),
                });
                
                currentIndex = endIndex;
            }
            else if (sourceTokens[currentIndex] is { Type: TokenType.NameIdentifierToken }
                && sourceTokens[currentIndex + 1] is { Type: TokenType.OpenParenthesisToken })
            {
                int semiColonIndex = currentIndex + sourceTokens[currentIndex..]
                    .IndexOf(token => token.Type == TokenType.SemicolonToken);
                
                internalNodes.Enqueue(ParseExpression(sourceTokens[currentIndex..semiColonIndex]));
                currentIndex = semiColonIndex;
            }
            
            currentIndex++;
        }

        return new SyntaxTreeNode
        {
            Value = sourceTokens[1].Value!,
            Type = SyntaxTreeNodeType.Namespace,
            Children = internalNodes.ToArray(),
        };
    }

    [Pure]
    private static int FindIndexOfLastClosingScope(ReadOnlySpan<Token> tokens, int startingPosition)
    {
        uint openScopeCount = 0;
        
        for (int i = startingPosition; i < tokens.Length; i++)
        {
            Token token = tokens[i];
            switch (token)
            {
                case { Type: TokenType.CloseScopeToken } when --openScopeCount == 0:
                    return i;
                case { Type: TokenType.OpenScopeToken }:
                    openScopeCount++;
                    break;
            }
        }

        return -1;
    }

    [Pure]
    private static ReadOnlySpan<Token> RetrieveAllTokensInInnerScope(ReadOnlySpan<Token> outerScopeTokens)
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
    private static SyntaxTreeNode ParseMethod(ReadOnlySpan<Token> methodTokens)
    {
        int argumentOpenParenthesisIndex = methodTokens.IndexOf(token => token is { Type: TokenType.OpenParenthesisToken }); 
        int argumentCloseParenthesisIndex = methodTokens.IndexOf(token => token is { Type: TokenType.CloseParenthesisToken });
        ReadOnlySpan<Token> methodArgumentTokens = methodTokens[(argumentOpenParenthesisIndex+1)..argumentCloseParenthesisIndex];
        
        ReadOnlySpan<Token> methodBodyTokens = RetrieveAllTokensInInnerScope(methodTokens);
        
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
    private static ReadOnlySpan<SyntaxTreeNode> ParseMethodArguments(ReadOnlySpan<Token> methodArgumentTokens)
    {
        SyntaxTreeNode[] argumentSyntaxTreeNodes = ArrayPool<SyntaxTreeNode>.Shared.Rent(methodArgumentTokens.Length / 2);
        
        int syntaxNodeCounter = 0;
        for (int i = 1; i < methodArgumentTokens.Length; i += 3)
        {
            argumentSyntaxTreeNodes[syntaxNodeCounter++] = new SyntaxTreeNode
            {
                Value = methodArgumentTokens[i].Value!,
                Type = SyntaxTreeNodeType.MethodArgument
            };
        }

        ArrayPool<SyntaxTreeNode>.Shared.Return(argumentSyntaxTreeNodes);
        return argumentSyntaxTreeNodes[..syntaxNodeCounter];
    }

    [Pure]
    private static ReadOnlySpan<SyntaxTreeNode> ParseInternalScope(ReadOnlySpan<Token> methodBodyTokens)
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
                methodBodyTokens[tokenIndex..]
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

        Span<SyntaxTreeNode> bodyNodesSpan = CollectionsMarshal.AsSpan(bodyNodes);
        return GroupConsecutiveAssignments(bodyNodesSpan);
    }

    [Pure]
    private static ReadOnlySpan<SyntaxTreeNode> GroupConsecutiveAssignments(Span<SyntaxTreeNode> bodyNodesSpan)
    {
        Queue<SyntaxTreeNode> bodyNodesWithCompoundAssignment = new Queue<SyntaxTreeNode>();
        
        for (int i = 0; i < bodyNodesSpan.Length; i++)
        {
            if (bodyNodesSpan[i] is not { Type: SyntaxTreeNodeType.Assignment })
            {
                bodyNodesWithCompoundAssignment.Enqueue(bodyNodesSpan[i]);
                continue;
            }

            int j = i + 1;
            while (j < bodyNodesSpan.Length && bodyNodesSpan[j] is { Type: SyntaxTreeNodeType.Assignment })
                j++;

            bodyNodesWithCompoundAssignment.Enqueue(
                i + 1 == j
                    ? bodyNodesSpan[i]
                    : new SyntaxTreeNode
                    {
                        Type = SyntaxTreeNodeType.Assignment,
                        Children = bodyNodesSpan[i..j].ToArray()
                    });

            i = j - 1;
        }

        return bodyNodesWithCompoundAssignment.ToArray();
    }

    [Pure]
    private static SyntaxTreeNode ParseExpression(ReadOnlySpan<Token> expressionTokens)
    {
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
            && expressionTokens.LastIndexOf(token => token is { Type: TokenType.CloseParenthesisToken})
                is { } closeParenthesisIndex and >= 0)
            return ParseExpression(expressionTokens[1..closeParenthesisIndex]);

        if (expressionTokens[0] is { Type: TokenType.BranchingOperatorToken, Value: "if" }
            && expressionTokens.IndexOf(token => token is { Type: TokenType.CloseParenthesisToken }) is { } branchCheckParenthesisIndex and > 0)
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

        if (expressionTokens[0] is { Type: TokenType.BranchingOperatorToken, Value: "else" })
            return new SyntaxTreeNode
            {
                Value = expressionTokens[0].Value!,
                Type = SyntaxTreeNodeType.Branch,
                Children = ParseInternalScope(expressionTokens[2..]).ToArray()
            };

        if (expressionTokens.IndexOf(token => token is { Type: TokenType.AssignmentOperatorToken }) is
                { } assignmentOperatorIndex and > 0
            && expressionTokens[assignmentOperatorIndex - 1] is
                { Type: TokenType.NameIdentifierToken } variableNameToken)
        {
            SyntaxTreeNode childAssignmentNode = expressionTokens[assignmentOperatorIndex] is { Value: null }
                ? ParseExpression(expressionTokens[(assignmentOperatorIndex + 1)..])
                : new SyntaxTreeNode
                {
                    Value = expressionTokens[assignmentOperatorIndex].Value![0].ToString(),
                    Type = SyntaxTreeNodeType.Expression,
                    Children =
                    [
                        ParseExpression([expressionTokens[assignmentOperatorIndex - 1]]),
                        ParseExpression(expressionTokens[(assignmentOperatorIndex + 1)..])
                    ]
                };
            return new SyntaxTreeNode
            {
                Value = variableNameToken.Value!,
                Type = SyntaxTreeNodeType.Assignment,
                Children = [childAssignmentNode]
            };
        }

        if (expressionTokens[0] is { Type: TokenType.NameIdentifierToken }
            && expressionTokens[1] is { Type: TokenType.OpenParenthesisToken }
            && expressionTokens[^1] is { Type: TokenType.CloseParenthesisToken })
            return new SyntaxTreeNode
            {
                Value = expressionTokens[0].Value!,
                Type = SyntaxTreeNodeType.Expression,
                Children = ParseCollection(expressionTokens[2..^1]).ToArray(),
            };
        
        if (expressionTokens[0] is { Type: TokenType.OpenCollectionToken }
            && expressionTokens[^1] is { Type: TokenType.CloseCollectionToken })
            return new SyntaxTreeNode
            {
                Type = SyntaxTreeNodeType.Collection,
                Children = ParseCollection(expressionTokens[1..^1]).ToArray()
            };
        
        if (expressionTokens.IndexOf(token => token is {Type: TokenType.NumericOperationToken}) is { } numericOperationIndex and >= 0)
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
        
        if (expressionTokens.IndexOf(token => token is { Type: TokenType.EqualityOperatorToken }) is { } equalityIndex and >= 0)
            return new SyntaxTreeNode
            {
                Value = "=",
                Type = SyntaxTreeNodeType.EqualityCheck,
                Children =
                [
                    ParseExpression(expressionTokens[..equalityIndex]),
                    ParseExpression(expressionTokens[(equalityIndex + 1)..])
                ]
            };

        if (expressionTokens[0] is { Type: TokenType.NameIdentifierToken }
            && expressionTokens[1] is { Type: TokenType.DotMethodToken }
            && expressionTokens[2] is { Type: TokenType.NameIdentifierToken })
            return new SyntaxTreeNode
            {
                Value = expressionTokens[2].Value!,
                Type = SyntaxTreeNodeType.Expression,
                Children =
                [
                    ParseExpression([expressionTokens[0]]),
                    ..ParseCollection(expressionTokens[4..^1])
                ]
            };
        
        throw new Exception($"Failed to parse expression {string.Join(';', expressionTokens.ToString())}");
    }

    [Pure]
    private static ReadOnlySpan<SyntaxTreeNode> ParseCollection(ReadOnlySpan<Token> collectionTokens)
    {
        int elementsCounter = 0;
        SyntaxTreeNode[] collectionElements = ArrayPool<SyntaxTreeNode>.Shared.Rent(collectionTokens.Length);

        int i = 0;
        for (int j = 1; j < collectionTokens.Length + 1; j++)
        {
            if (j < collectionTokens.Length && collectionTokens[j] is not { Type: TokenType.CommaToken }) 
                continue;
            
            collectionElements[elementsCounter++] = ParseExpression(collectionTokens[i..j]);
            i = j + 1;
        }

        ArrayPool<SyntaxTreeNode>.Shared.Return(collectionElements);
        return collectionElements[..elementsCounter];
    }
}