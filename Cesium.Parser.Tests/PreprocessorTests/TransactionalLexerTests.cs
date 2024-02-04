using Cesium.Preprocessor;
using Cesium.TestFramework;
using Yoakke.SynKit.Parser;

namespace Cesium.Parser.Tests.PreprocessorTests;

public class TransactionalLexerTests
{
    [Fact]
    public void ReportsWarningsOnUnclosedTransactions()
    {
        var warningProcessor = new ListWarningProcessor();
        using (var lexer = new TransactionalLexer([], warningProcessor))
        {
            _ = lexer.BeginTransaction();
        }

        var w = Assert.Single(warningProcessor.Warnings);
        Assert.Equal($"Lexer was disposed while there were 1 open transactions.", w.Message);
    }

    [Fact]
    public void ReportsNoWarnings()
    {
        using var warningProcessor = new ListWarningProcessor();
        using var lexer = new TransactionalLexer([], warningProcessor);
        using var transaction = lexer.BeginTransaction();
        transaction.End<object?>(ParseResult.Ok<object?>(null, 0));
    }
}
