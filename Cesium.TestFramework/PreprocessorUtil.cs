using System.Diagnostics.CodeAnalysis;
using Cesium.Core.Warnings;
using Cesium.Preprocessor;
using Yoakke.SynKit.Lexer;

namespace Cesium.TestFramework;

public static class PreprocessorUtil
{
    public static async Task<string> DoPreprocess(
        string sourceFileName,
        [StringSyntax("cpp")] string source,
        Dictionary<string, string>? standardHeaders = null,
        Dictionary<string, IList<IToken<CPreprocessorTokenType>>>? defines = null,
        Action<PreprocessorWarning>? onWarning = null)
    {
        var lexer = new CPreprocessorLexer(sourceFileName, source);
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
                sourceFileName,
                lexer,
                includeContext,
                definesContext,
                warningProcessor);
            var result = await preprocessor.ProcessSource();
            return result;
        }
    }
}
