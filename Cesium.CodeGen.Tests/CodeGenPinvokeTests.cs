using Cesium.Core.Warnings;
using Cesium.Preprocessor;
using Cesium.TestFramework;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;

namespace Cesium.CodeGen.Tests;

public class CodeGenPinvokeTests : CodeGenTestBase
{
    private const string _mainMockedFilePath = @"c:\a\b\c.c";

    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var processed = DoPreprocess(source, null, null, null);
        processed.Wait();
        var assembly = GenerateAssembly(default, processed.Result);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    private static async Task<string> DoPreprocess(
        [StringSyntax("cpp")] string source,
        Dictionary<string, string>? standardHeaders = null,
        Dictionary<string, IList<IToken<CPreprocessorTokenType>>>? defines = null,
        Action<PreprocessorWarning>? onWarning = null)
    {
        var lexer = new CPreprocessorLexer(_mainMockedFilePath, source);
        var includeContext = new IncludeContextMock(standardHeaders ?? new Dictionary<string, string>());
        var definesContext = new InMemoryDefinesContext();
        if (defines != null)
        {
            foreach (var (name, value) in defines)
            {
                definesContext.DefineMacro(name, null, value);
            }
        }

        IWarningProcessor warningProcessor = onWarning == null
            ? new ListWarningProcessor()
            : new LambdaWarningProcessor(onWarning);
        using (warningProcessor as IDisposable)
        {
            var preprocessor = new CPreprocessor(
                _mainMockedFilePath,
                lexer,
                includeContext,
                definesContext,
                warningProcessor);
            var result = await preprocessor.ProcessSource();
            return result;
        }
    }

    [Fact]
    public Task SinglePinvokePragma() => DoTest(@"
#pragma pinvoke(""mydll.dll"")
int not_pinvoke();
int foo_bar(int*);

int main() {
    return foo_bar(0);
}

int not_pinvoke() { return 1; }
");
}
