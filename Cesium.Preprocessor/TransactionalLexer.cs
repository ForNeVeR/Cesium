using Cesium.Core;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;

namespace Cesium.Preprocessor;

internal class TransactionalLexer(ILexer<IToken<CPreprocessorTokenType>> lexer) : IDisposable
{
    private readonly List<IToken<CPreprocessorTokenType>> _allTokens = ToList(lexer);
    private int _nextTokenToReturn;
    private int _openTransactions;

    public IToken<CPreprocessorTokenType> Next() => _allTokens[_nextTokenToReturn++];
    public IToken<CPreprocessorTokenType> Peek(int idx = 0) => _allTokens[_nextTokenToReturn + idx];
    public bool IsEnd => Peek() is { Kind: CPreprocessorTokenType.End };

    public LexerTransaction BeginTransaction()
    {
        ++_openTransactions;
        return new LexerTransaction(this, _nextTokenToReturn);
    }

    public void Dispose()
    {
        if (_openTransactions != 0)
        {
            throw new AssertException($"Lexer was disposed while there were {_openTransactions} open transactions.");
        }
    }

    public class LexerTransaction(TransactionalLexer lexer, int startPos) : IDisposable
    {
        private bool _endedWithSuccess;
        private bool _endedWithError;

        private bool IsCompleted => _endedWithSuccess || _endedWithError;

        public ParseResult<T> End<T>(ParseResult<T> result)
        {
            if (IsCompleted)
                throw new AssertException($"Double-completion of a lexer transaction started ad {startPos}, near {lexer.Peek()}.");

            _endedWithSuccess = result.IsOk;
            _endedWithError = result.IsError;

            return result;
        }

        public ParseError End(ParseError result)
        {
            if (IsCompleted)
                throw new AssertException($"Double-completion of a lexer transaction started ad {startPos}, near {lexer.Peek()}.");

            _endedWithError = true;

            return result;
        }

        public void Dispose()
        {
            if (!IsCompleted)
                throw new AssertException($"Lexer transaction started at {startPos} was not completed upon disposal, near {lexer.Peek()}.");

            if (_endedWithError)
            {
                // Roll back the transaction:
                if (lexer._nextTokenToReturn < startPos)
                {
                    throw new AssertException(
                        $"Transactional conflict: lexer's current position is {lexer._nextTokenToReturn} that's lesser " +
                        $"than the position we want to reset it to, {startPos}.");
                }

                lexer._nextTokenToReturn = startPos;
            }

            --lexer._openTransactions;
        }
    }

    private static List<IToken<CPreprocessorTokenType>> ToList(ILexer<IToken<CPreprocessorTokenType>> lexer)
    {
        var result = new List<IToken<CPreprocessorTokenType>>();
        while (!lexer.IsEnd)
        {
            result.Add(lexer.Next());
        }

        return result;
    }
}


