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
    public bool IsEnd => _nextTokenToReturn >= _allTokens.Count || Peek() is { Kind: CPreprocessorTokenType.End };

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

        // TODO: Test for \ and then whitespace on same line.
        var spaceEater = false;
        var wasWarningIssued = false;
        var spaceEaterBuffer = new List<IToken<CPreprocessorTokenType>>();

        while (!lexer.IsEnd)
        {
            var nextToken = lexer.Next();
            switch (spaceEater)
            {
                case true when nextToken is { Kind: CPreprocessorTokenType.WhiteSpace }:
                    if (!wasWarningIssued)
                    {
                        EmitSpaceEaterWarning(nextToken);
                        wasWarningIssued = true;
                    }
                    spaceEaterBuffer.Add(nextToken);
                    continue;
                case true when nextToken is { Kind: CPreprocessorTokenType.NewLine }:
                    // Skip all the allocated buffer.
                    ClearSpaceEaterState();
                    continue;
                default:
                    result.AddRange(spaceEaterBuffer);
                    ClearSpaceEaterState();

                    if (nextToken is { Kind: CPreprocessorTokenType.NextLine })
                    {
                        spaceEaterBuffer.Add(nextToken);
                        spaceEater = true;
                        continue;
                    }

                    result.Add(nextToken);
                    break;
            }
        }

        return result;

        void ClearSpaceEaterState()
        {
            spaceEater = false;
            wasWarningIssued = false;
            spaceEaterBuffer.Clear();
        }

        static void EmitSpaceEaterWarning(IToken<CPreprocessorTokenType> token)
        {
            CPreprocessor.EmitWarning($"Whitespace after backslash but before newline at {token.Location}.");
        }
    }
}


