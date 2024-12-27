using System.Diagnostics.Contracts;
using System.Text;

namespace ClojureSharp.Transpiler;

public class Prettifier(char indentationCharacter = '\t', uint numberOfIndentationCharacters = 1)
{
    [Pure]
    public string Prettify(string transpiledCode)
    {
        StringBuilder output = new StringBuilder();

        int numberOfOpenBrackets = 0;
        
        foreach (char c in transpiledCode.AsSpan())
        {
            numberOfOpenBrackets += c switch { '(' => 1, ')' => -1, _ => 0 };
            
            while (c is ')' && char.IsWhiteSpace(output[^1]))
                output.Remove(output.Length - 1, 1);
            
            output.Append(c);
            
            if (c is '\n' && numberOfOpenBrackets > 0)
                output.Append(indentationCharacter, (int)(numberOfIndentationCharacters * numberOfOpenBrackets));
        }

        return output.ToString();
    }
}