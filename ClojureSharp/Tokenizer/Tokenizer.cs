using System.Diagnostics.Contracts;
using System.Text;

namespace ClojureSharp.Tokenizer;

internal class Tokenizer(string sourceCode)
{
    private int _currentIndex;
    
    internal Token[] Tokenize()
    {
        Queue<Token> tokenBuffer = new Queue<Token>();
        
        _currentIndex = -1;
        
        while (Peek() is { } character)
        {
            if (char.IsWhiteSpace(character))
            {
                Consume();
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                string parsedIdentifier = char.IsLetter(character)
                    ? ParseIdentifier()
                    : ParseNumber();
                
                tokenBuffer.Enqueue(parsedIdentifier switch
                {
                    "namespace"
                        => new Token(TokenType.NamespaceToken, parsedIdentifier),
                    "int" or "double" or "string" or "bool" 
                        => new Token(TokenType.TypeDeclarationToken, parsedIdentifier),
                    "return" 
                        => new Token(TokenType.ReturnToken),
                    _ when char.IsDigit(parsedIdentifier[0])
                        => new Token(TokenType.NumericLiteralToken, parsedIdentifier),
                    _ => new Token(TokenType.NameIdentifierToken, parsedIdentifier)
                });
                
                continue;
            }

            if (character is '=')
            {
                if (Peek(2) is '=')
                {
                    tokenBuffer.Enqueue(new Token(TokenType.EqualityOperatorToken));
                    Consume(2);
                    continue;
                }
                
                tokenBuffer.Enqueue(new Token(TokenType.AssignmentOperatorToken));
                
                Consume();
                continue;
            }
            
            tokenBuffer.Enqueue(character switch
            {
                '(' => new Token(TokenType.OpenParenthesisToken),
                ')' => new Token(TokenType.CloseParenthesisToken),
                '{' => new Token(TokenType.OpenScopeToken),
                '}' => new Token(TokenType.CloseScopeToken),
                ';' => new Token(TokenType.SemicolonToken),
                '+' or '-' or '*' or '/'
                    => new Token(TokenType.NumericOperationToken, character.ToString()),
                ',' => new Token(TokenType.CommaToken),
                _ => throw new Exception("Valid token not found!")
            });
            
            Consume();
        }

        return tokenBuffer.ToArray();
    }
    
    private string ParseIdentifier()
    {
        StringBuilder parsedCharacters = new StringBuilder();

        while (Peek() is { } character && char.IsLetterOrDigit(character))
        {
            parsedCharacters.Append(character);
            Consume();
        }

        return parsedCharacters.ToString();
    }

    private string ParseNumber()
    {
        StringBuilder parsedCharacters = new StringBuilder();

        while (Peek() is { } character && (char.IsDigit(character) || character is '.' or 'f' or 'd'))
        {
            parsedCharacters.Append(character);
            Consume();
        }
        
        return parsedCharacters
            .Replace("f", "")
            .Replace("d", "")
            .ToString();
    }
    
    [Pure]
    private char? Peek(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (_currentIndex + count >= sourceCode.Length)
            return null;

        return sourceCode.AsSpan()[_currentIndex + count];
    }

    private void Consume(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        _currentIndex += count;
    }
}