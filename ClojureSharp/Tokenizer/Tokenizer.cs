using System.Diagnostics.Contracts;
using System.Text;
using ClojureSharp.Extensions.Queue;

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
                && _sourceCode.Peek() is '/'
                && _sourceCode.ElementAt(1) is '/')
            {
                string parsedCommentString = ParseWithPredicate(c => c is not ('\r' or '\n'));

                tokenBuffer.Enqueue(new Token(TokenType.CommentToken, parsedCommentString[2..]));
                continue;
            }
            
            if (char.IsLetterOrDigit(character))
            {
                string parsedIdentifier = ParseWithPredicate(char.IsLetter(character)
                    ? c => char.IsLetterOrDigit(c) || c is '<' or '>'
                    : c => char.IsDigit(c) || c is '.');
                
                tokenBuffer.Enqueue(parsedIdentifier switch
                {
                    "namespace"
                        => new Token(TokenType.NamespaceToken),
                    "class"
                        => new Token(TokenType.ClassToken),
                    "var" or "int" or "double" or "string" or "bool"
                        => new Token(TokenType.TypeDeclarationToken, parsedIdentifier),
                    _ when IsGenericType(parsedIdentifier)
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
                string parsedSymbolSequence =
                    ParseWithPredicateNonConsuming(c => char.IsSymbol(c) || char.IsPunctuation(c));
                
                Token? matchingToken = parsedSymbolSequence switch
                {
                    "==" 
                        => new Token(TokenType.EqualityOperatorToken, "="),
                    "&&" or "||" 
                        => new Token(TokenType.BooleanOperationToken, parsedSymbolSequence),
                    "+=" or "-=" or "*=" or "/="
                        => new Token(TokenType.AssignmentOperatorToken, parsedSymbolSequence),
                    _ 
                        => null
                };

                if (matchingToken is { })
                {
                    tokenBuffer.Enqueue(matchingToken.Value);
                    _sourceCode.Dequeue(parsedSymbolSequence.Length);
                    continue;
                }
                
                tokenBuffer.Enqueue(character switch
                {
                    '=' => new Token(TokenType.AssignmentOperatorToken),
                    '(' => new Token(TokenType.OpenParenthesisToken),
                    ')' => new Token(TokenType.CloseParenthesisToken),
                    '{' => new Token(TokenType.OpenScopeToken),
                    '}' => new Token(TokenType.CloseScopeToken),
                    '[' => new Token(TokenType.OpenCollectionToken),
                    ']' => new Token(TokenType.CloseCollectionToken),
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
        int genericTypeOpenBracket = textValue.IndexOf('<');
        int genericTypeCloseBracket = textValue.IndexOf('>');
        return genericTypeOpenBracket is not -1
            && genericTypeCloseBracket is not -1
            && genericTypeOpenBracket + 1 != genericTypeCloseBracket;
    }

    private string ParseWithPredicate(Func<char, bool> predicate)
    {
        StringBuilder parsedString = new StringBuilder();
        
        while (_sourceCode.TryPeek(out char peekedCharacter) && predicate(peekedCharacter))
            parsedString.Append(_sourceCode.Dequeue());
        
        return parsedString.ToString();
    }

    private string ParseWithPredicateNonConsuming(Func<char, bool> predicate)
    {
        StringBuilder parsedString = new StringBuilder();
        int peekCounter = 0;
        
        while (_sourceCode.ElementAtOrDefault(peekCounter++) is { } peekedCharacter && predicate(peekedCharacter))
            parsedString.Append(peekedCharacter);
        
        return parsedString.ToString();
    }
}