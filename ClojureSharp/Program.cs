using System.Diagnostics;
using ClojureSharp.SyntaxTreeParser;
using ClojureSharp.Tokenizer;
using ClojureSharp.Transpiler;

Stopwatch sw = Stopwatch.StartNew();

string inputCodeText = await File.ReadAllTextAsync("../../../input/source.cs");

Console.WriteLine("File Reader: {0}", sw.ElapsedMilliseconds);
sw.Restart();

Tokenizer tokenizer = new Tokenizer(inputCodeText);
Token[] sourceCodeTokens = tokenizer.Tokenize();

Console.WriteLine("Tokenizer: {0}", sw.ElapsedMilliseconds);
sw.Restart();

SyntaxTreeBuilder syntaxTreeBuilder = new SyntaxTreeBuilder(sourceCodeTokens);
SyntaxTreeNode abstractSyntaxTree = syntaxTreeBuilder.Parse();

Console.WriteLine("AST Builder: {0}", sw.ElapsedMilliseconds);
sw.Restart();

Transpiler transpiler = new Transpiler(abstractSyntaxTree);
string transpiledCode = transpiler.Transpile();

Console.WriteLine("Transpiler: {0}", sw.ElapsedMilliseconds);
sw.Restart();

Prettifier prettifier = new Prettifier(' ', 4);
string prettyTranspiledCode = prettifier.Prettify(transpiledCode);

Console.WriteLine("Prettifier: {0}", sw.ElapsedMilliseconds);
sw.Restart();

await File.WriteAllTextAsync("../../../output/source.clj", prettyTranspiledCode);

Console.WriteLine("File Writer: {0}", sw.ElapsedMilliseconds);
sw.Restart();