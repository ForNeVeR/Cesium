// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Cesium.Core.Warnings;
using Cesium.Preprocessor;
using TruePath;
using Yoakke.SynKit.Lexer;

namespace Cesium.TestFramework;

public static class PreprocessorUtil
{
    public static async Task<string> DoPreprocess(
        AbsolutePath sourceFileName,
        [StringSyntax("cpp")] string source,
        Dictionary<LocalPath, string>? standardHeaders = null,
        Dictionary<string, IList<IToken<CPreprocessorTokenType>>>? defines = null,
        Action<PreprocessorWarning>? onWarning = null)
    {
        var lexer = new CPreprocessorLexer(sourceFileName.Value, source);
        var includeContext = new IncludeContextMock(standardHeaders ?? new Dictionary<LocalPath, string>());
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
