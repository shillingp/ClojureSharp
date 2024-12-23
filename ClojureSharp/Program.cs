using ClojureSharp;
using ClojureSharp.SyntaxTreeParser;
using ClojureSharp.Tokenizer;
using ClojureSharp.Transpiler;

string inputCodeText = await File.ReadAllTextAsync("../../../input/source.cs");

Tokenizer tokenizer = new Tokenizer(inputCodeText);
Token[] sourceCodeTokens = tokenizer.Tokenize();

SyntaxTreeParser syntaxTreeParser = new SyntaxTreeParser(sourceCodeTokens);
SyntaxTreeNode abstractSyntaxTree = syntaxTreeParser.Parse();

Transpiler transpiler = new Transpiler(abstractSyntaxTree);
string transpiledCode = transpiler.Transpile();

await File.WriteAllTextAsync("../../../output/source.clj", transpiledCode);