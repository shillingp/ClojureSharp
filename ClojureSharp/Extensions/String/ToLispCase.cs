using System.Text.RegularExpressions;

namespace ClojureSharp.Extensions.String;

public static partial class StringExtensions
{
    public static string ToLispCase(this string s)
    {
        return Regex.Replace(s, "([a-z0-9])([A-Z])", "$1-$2").ToLower();
    }
}