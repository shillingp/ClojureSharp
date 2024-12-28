using System.Diagnostics.Contracts;
using System.Text;

namespace ClojureSharp.Tokenizer;

internal class Tokenizer(string sourceCode)
{
    private readonly Queue<char> _sourceCode = new Queue<char>(sourceCode);
    
    internal Token[] Tokenize()
    {
        Queue<Token> tokenBuffer = new Queue<Token>();
        
        while (_sourceCode.TryPeek(out char character))
        {
            if (char.IsWhiteSpace(character))
            {
                _sourceCode.Dequeue();
                continue;
            }

            if (_sourceCode.Count > 1
                && _sourceCode.ElementAt(0) is '/'
                && _sourceCode.ElementAt(1) is '/')
            {
                StringBuilder commentStringBuilder = new StringBuilder();
                while (_sourceCode.Peek() is not ('\r' or '\n'))
                    commentStringBuilder.Append(_sourceCode.Dequeue());

                tokenBuffer.Enqueue(new Token(TokenType.CommentToken, commentStringBuilder.ToString()[2..]));
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
                        => new Token(TokenType.NamespaceToken),
                    "class"
                        => new Token(TokenType.ClassToken),
                    "new"
                        => new Token(TokenType.InvocationToken),
                    "var" or "int" or "double" or "string" or "bool"
                        => new Token(TokenType.TypeDeclarationToken, parsedIdentifier),
                    _ when IsGenericType(parsedIdentifier)
                        => new Token(TokenType.CollectionDeclarationToken, parsedIdentifier),
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
                StringBuilder parsedSymbolSequenceBuilder = new StringBuilder();
                int peekCounter = 0;
                while (_sourceCode.ElementAtOrDefault(peekCounter++) is { } peekedSymbol 
                    && (char.IsSymbol(peekedSymbol) || char.IsPunctuation(peekedSymbol)))
                    parsedSymbolSequenceBuilder.Append(peekedSymbol);
                string parsedSymbolSequence = parsedSymbolSequenceBuilder.ToString();
                
                Token? matchingToken = parsedSymbolSequence switch
                {
                    "==" 
                        => new Token(TokenType.EqualityOperatorToken),
                    "&&" or "||" 
                        => new Token(TokenType.BooleanOperationToken, parsedSymbolSequence),
                    "+=" or "-=" or "*=" or "/="
                        => new Token(TokenType.AssignmentOperatorToken, parsedSymbolSequence),
                    _ 
                        => null
                };

                if (matchingToken is not null)
                {
                    tokenBuffer.Enqueue(matchingToken.Value);
                    for (int i = parsedSymbolSequence.Length - 1; i >= 0; i--)
                        _sourceCode.Dequeue();
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
                    '.' => new Token(TokenType.DotMethodToken), 
                    _ => throw new FormatException($"Unrecognized character '{character}'.")
                });

                _sourceCode.Dequeue();
                continue;
            }
            
            throw new Exception($"Unknown character '{character}'");
        }

        return tokenBuffer.ToArray();
    }

    [Pure]
    private static bool IsGenericType(string textValue)
    {
        return textValue.StartsWith("List") 
            || textValue.StartsWith("Array")
            || textValue.StartsWith("Queue");
    }

    private string ParseIdentifier()
    {
        StringBuilder parsedCharacters = new StringBuilder();

        while (_sourceCode.TryPeek(out char character) && (char.IsLetterOrDigit(character) || character is '<' or '>'))
            parsedCharacters.Append(_sourceCode.Dequeue());

        return parsedCharacters.ToString();
    }

    private string ParseNumber()
    {
        StringBuilder parsedCharacters = new StringBuilder();

        while (_sourceCode.TryPeek(out char character) && (char.IsDigit(character) || character is '.' or 'f' or 'd'))
            parsedCharacters.Append(_sourceCode.Dequeue());
        
        return parsedCharacters
            .Replace("f", "")
            .Replace("d", "")
            .ToString();
    }
}