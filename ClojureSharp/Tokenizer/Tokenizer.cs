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
                    "true" or "false" 
                        => new Token(TokenType.BooleanLiteralToken, parsedIdentifier),
                    "null"
                        => new Token(TokenType.NullLiteralToken, parsedIdentifier),
                    "return" 
                        => new Token(TokenType.ReturnToken),
                    "if" or "else"
                        => new Token(TokenType.BranchingOperatorToken, parsedIdentifier),
                    _ when char.IsDigit(parsedIdentifier[0])
                        => new Token(TokenType.NumericLiteralToken, parsedIdentifier),
                    _ => new Token(TokenType.NameIdentifierToken, parsedIdentifier)
                });
                
                continue;
            }

            if (char.IsSymbol(character) || char.IsPunctuation(character))
            {
                int peekCounter = 1;
                string parsedSymbolSequence = "";
                while (Peek(peekCounter++) is { } peekedSymbol && (char.IsSymbol(peekedSymbol) || char.IsPunctuation(peekedSymbol)))
                    parsedSymbolSequence += peekedSymbol;

                Token? matchingToken = parsedSymbolSequence switch
                {
                    "==" => new Token(TokenType.EqualityOperatorToken),
                    "&&" or "||" => new Token(TokenType.BooleanOperationToken, parsedSymbolSequence),
                    _ => null
                };

                if (matchingToken is not null)
                {
                    tokenBuffer.Enqueue(matchingToken.Value);
                    Consume(parsedSymbolSequence.Length);
                    continue;
                }
                
                tokenBuffer.Enqueue(character switch
                {
                    '=' => new Token(TokenType.AssignmentOperatorToken),
                    '(' => new Token(TokenType.OpenParenthesisToken),
                    ')' => new Token(TokenType.CloseParenthesisToken),
                    '{' => new Token(TokenType.OpenScopeToken),
                    '}' => new Token(TokenType.CloseScopeToken),
                    ';' => new Token(TokenType.SemicolonToken),
                    '+' or '-' or '*' or '/' or '%'
                        => new Token(TokenType.NumericOperationToken, character.ToString()),
                    '|' or '&'
                        => new Token(TokenType.BooleanOperationToken, character.ToString()),
                    ',' => new Token(TokenType.CommaToken),
                    _ => throw new FormatException($"Unrecognized character '{character}'.")
                });

                Consume();
                continue;
            }
            
            throw new Exception($"Unknown character '{character}'");
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