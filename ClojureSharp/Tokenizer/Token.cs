namespace ClojureSharp.Tokenizer;

internal class Token(TokenType tokenType)
{
    internal TokenType Type = tokenType;
    internal string? Value;

    internal Token(TokenType tokenType, string value)
        : this(tokenType)
    {
        Value = value;
    }
}

internal enum TokenType
{
    NamespaceToken,
    
    TypeDeclarationToken,
    NameIdentifierToken,
    
    OpenParenthesisToken,
    CloseParenthesisToken,
    
    OpenScopeToken,
    CloseScopeToken,
    
    SemicolonToken,
    CommaToken,
    
    ReturnToken,
    
    NullLiteralToken,
    NumericLiteralToken,
    BooleanLiteralToken,
    
    NumericOperationToken,
    BooleanOperationToken,
    
    AssignmentOperatorToken,
    EqualityOperatorToken,
}