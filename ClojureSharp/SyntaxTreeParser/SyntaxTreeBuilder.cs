using System.Diagnostics.Contracts;
using ClojureSharp.Tokenizer;
using ClojureSharp.Extensions;

namespace ClojureSharp.SyntaxTreeParser;

internal class SyntaxTreeBuilder(Token[] abstractSyntaxTree)
{
    internal SyntaxTreeNode Parse()
    { 
        int currentIndex = 0;
        
        if (abstractSyntaxTree[0] is not { Type: TokenType.NamespaceToken }
            && abstractSyntaxTree[1] is not { Type: TokenType.NameIdentifierToken})
            throw new Exception("Namespace not found");
        
        List<SyntaxTreeNode> namespaceNodes = new List<SyntaxTreeNode>();
            
        while (currentIndex < abstractSyntaxTree.Length)
        {
            if (abstractSyntaxTree[currentIndex] is { Type: TokenType.TypeDeclarationToken }
                && abstractSyntaxTree[currentIndex + 1] is { Type: TokenType.NameIdentifierToken }
                && abstractSyntaxTree[currentIndex + 2] is { Type: TokenType.OpenParenthesisToken })
            {
                namespaceNodes.Add(ParseMethod(abstractSyntaxTree[currentIndex..]));
                
                currentIndex = FindIndexOfLastClosingScope(currentIndex);
            }
            else if (abstractSyntaxTree[currentIndex] is { Type: TokenType.NameIdentifierToken }
                && abstractSyntaxTree[currentIndex + 1] is { Type: TokenType.OpenParenthesisToken })
            {
                int semiColonIndex = currentIndex + abstractSyntaxTree[currentIndex..]
                    .IndexOf(token => token.Type == TokenType.SemicolonToken);
                
                namespaceNodes.Add(ParseExpression(abstractSyntaxTree[currentIndex..semiColonIndex]));
                currentIndex = semiColonIndex;
            }
            
            currentIndex++;
        }

        return new SyntaxTreeNode
        {
            Value = abstractSyntaxTree[1].Value!,
            Type = SyntaxTreeNodeType.Namespace,
            Children = namespaceNodes.ToArray(),
        };
    }

    [Pure]
    private int FindIndexOfLastClosingScope(int currentIndex)
    {
        uint openScopeCount = 0;
        int? firstOpeningScope = null;

        for (int i = currentIndex; i < abstractSyntaxTree.Length; i++)
        {
            Token token = abstractSyntaxTree[i];
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
    private static Token[] RetrieveAllTokensInInnerScope(Token[] outerScopeTokens)
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
                return outerScopeTokens[firstOpeningScope!.Value..i];
        }
        
        throw new Exception("Failed to find valid scope");
    }

    [Pure]
    private static SyntaxTreeNode ParseMethod(Token[] methodTokens)
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
                ..ParseMethodBody(methodBodyTokens)
            ]
        };
    }
    
    [Pure]
    private static IEnumerable<SyntaxTreeNode> ParseMethodArguments(Token[] methodArgumentTokens)
    {
        for (int i = 0; i < methodArgumentTokens.Length; i += 3)
        {
            yield return new SyntaxTreeNode
            {
                Value = methodArgumentTokens[i + 1].Value!,
                Type = SyntaxTreeNodeType.MethodArgument
            };
        }
    }

    [Pure]
    private static SyntaxTreeNode[] ParseMethodBody(Token[] methodBodyTokens)
    {
        List<SyntaxTreeNode> bodyNodes = new List<SyntaxTreeNode>();
        
        int tokenIndex = 0;
        while (tokenIndex < methodBodyTokens.Length)
        {
            if (methodBodyTokens[tokenIndex] is { Type: TokenType.ReturnToken })
            {
                tokenIndex++;
                continue;
            }
            
            int locationOfNextSemicolon = tokenIndex +
                methodBodyTokens[tokenIndex..methodBodyTokens.Length]
                    .IndexOf(token => token is { Type: TokenType.SemicolonToken });
                
            bodyNodes.Add(ParseExpression(methodBodyTokens[tokenIndex..locationOfNextSemicolon]));
            
            tokenIndex = locationOfNextSemicolon + 1;
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
    private static SyntaxTreeNode ParseExpression(Token[] expressionTokens)
    {
        if (expressionTokens.Length == 1)
            return new SyntaxTreeNode
            {
                Type = SyntaxTreeNodeType.Literal,
                Value = expressionTokens[0].Value!
            };

        if (expressionTokens[0] is { Type: TokenType.OpenParenthesisToken }
            && Array.FindLastIndex(expressionTokens, token => token is { Type: TokenType.CloseParenthesisToken})
                is int closeParenthesisIndex and >= 0)
            return ParseExpression(expressionTokens[1..closeParenthesisIndex]);

        if (expressionTokens.IndexOf(token => token is { Type: TokenType.AssignmentOperatorToken }) is int assignmentOperatorIndex and > 0
            && expressionTokens[assignmentOperatorIndex - 1] is { Type: TokenType.NameIdentifierToken} thing)
            return new SyntaxTreeNode
            {
                Value = thing.Value!,
                Type = SyntaxTreeNodeType.Assignment,
                Children = [ParseExpression(expressionTokens[(assignmentOperatorIndex+1)..])]
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
        
        if (expressionTokens.IndexOf(token => token is {Type: TokenType.NumericOperationToken}) is int numericOperationIndex and >= 0)
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
        
        if (expressionTokens.IndexOf(token => token is { Type: TokenType.EqualityOperatorToken }) is int equalityIndex and >= 0)
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
        
        throw new Exception("Failed to parse expression");
    }
}