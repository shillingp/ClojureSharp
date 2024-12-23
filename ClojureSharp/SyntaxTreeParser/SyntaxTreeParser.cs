using ClojureSharp.Tokenizer;
using System.Linq;
using System.Linq.Expressions;
using ClojureSharp.Extensions;

namespace ClojureSharp.SyntaxTreeParser;

internal class SyntaxTreeParser(Token[] abstractSyntaxTree)
{
    internal SyntaxTreeNode Parse()
    {
        if (abstractSyntaxTree[0] is not { Type: TokenType.NamespaceToken }
            && abstractSyntaxTree[1] is not { Type: TokenType.NameIdentifierToken})
            throw new Exception("Namespace not found");
        
        Token[] methodTokens = RetrieveAllTokensInInnerScope(abstractSyntaxTree);
        
        SyntaxTreeNode firstMethod = ParseMethod(methodTokens);

        return new SyntaxTreeNode
        {
            Value = abstractSyntaxTree[1].Value!,
            Type = SyntaxTreeNodeType.Namespace,
            Children = [firstMethod]
        };
    }

    private Token[] RetrieveAllTokensInInnerScope(Token[] outerScopeTokens)
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

    private SyntaxTreeNode ParseMethod(Token[] methodTokens)
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
    
    private IEnumerable<SyntaxTreeNode> ParseMethodArguments(Token[] methodArgumentTokens)
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

    private IEnumerable<SyntaxTreeNode> ParseMethodBody(Token[] methodBodyTokens)
    {
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
                
            yield return ParseExpression(methodBodyTokens[tokenIndex..locationOfNextSemicolon]);
            
            tokenIndex = locationOfNextSemicolon + 1;
        }
    }

    private SyntaxTreeNode ParseExpression(Token[] expressionTokens)
    {
        if (expressionTokens.Length == 1)
            return new SyntaxTreeNode
            {
                Type = SyntaxTreeNodeType.Literal,
                Value = expressionTokens[0].Value!
            };

        if (expressionTokens[1] is { Type: TokenType.NumericOperationToken } && expressionTokens.Length >= 3)
            return new SyntaxTreeNode
            {
                Value = expressionTokens[1].Value!,
                Type = SyntaxTreeNodeType.Expression,
                Children =
                [
                    ParseExpression([expressionTokens[0]]),
                    ParseExpression(expressionTokens[2..expressionTokens.Length])
                ]
            };

        if (expressionTokens[0] is { Type: TokenType.TypeDeclarationToken }
            && expressionTokens[1] is { Type: TokenType.NameIdentifierToken }
            && expressionTokens[2] is { Type: TokenType.AssignmentOperatorToken })
            return new SyntaxTreeNode
            {
                Value = expressionTokens[1].Value!,
                Type = SyntaxTreeNodeType.Assignment,
                Children = [ParseExpression(expressionTokens[3..])],
            };
        
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