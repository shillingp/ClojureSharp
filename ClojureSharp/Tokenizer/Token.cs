namespace ClojureSharp.Tokenizer;

internal readonly record struct Token
{
    internal readonly TokenType Type;
    internal readonly string? Value;

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
    ClassToken,
    
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
    
    BranchingOperatorToken,
    
    CommentToken
}
