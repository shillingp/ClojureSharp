using ClojureSharp.SyntaxTreeParser;
using ClojureSharp.Tokenizer;
using ClojureSharp.Transpiler;

string inputCodeText = await File.ReadAllTextAsync("../../../input/source.cs");

Tokenizer tokenizer = new Tokenizer(inputCodeText);
Token[] sourceCodeTokens = tokenizer.Tokenize();

SyntaxTreeBuilder syntaxTreeBuilder = new SyntaxTreeBuilder(sourceCodeTokens);
SyntaxTreeNode abstractSyntaxTree = syntaxTreeBuilder.Parse();

Transpiler transpiler = new Transpiler(abstractSyntaxTree);
string transpiledCode = transpiler.Transpile();

Prettifier prettifier = new Prettifier(' ', 4);
string prettyTranspiledCode = prettifier.Prettify(transpiledCode);

await File.WriteAllTextAsync("../../../output/source.clj", prettyTranspiledCode);