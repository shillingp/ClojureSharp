namespace ClojureSharp.Tokenizer;

internal record struct Token
{
    internal TokenType Type;
    internal string? Value;

    internal Token(TokenType tokenType)
    {
        Type = tokenType;
    }
    
    internal Token(TokenType tokenType, string value)
        : this(tokenType)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"{{Type: {Type}, Value: {Value}}}";
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
    
    BranchingOperatorToken
}