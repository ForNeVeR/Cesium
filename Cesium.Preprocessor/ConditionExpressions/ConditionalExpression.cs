using System.Text.RegularExpressions;
using Cesium.Core;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

internal class ConditionalExpression(
    IPreprocessorExpression condition,
    IPreprocessorExpression trueExpression,
    IPreprocessorExpression falseExpression)
    : IPreprocessorExpression
{
    public Location Location => condition.Location;

    public string EvaluateExpression(IMacroContext context)
    {
        var conditionValue = condition.EvaluateExpression(context);

        var parsedConditionValue = conditionValue is null ? 0 : Parse(condition.Location, conditionValue);
        if (parsedConditionValue.AsBoolean())
        {
            return trueExpression.EvaluateExpression(context) ?? "0";
        }
        else
        {
            return falseExpression.EvaluateExpression(context) ?? "0";
        }

        int Parse(Location location, string macrosValue)
        {
            if (Regex.IsMatch(macrosValue, $"^(0|[1-9][0-9]*)$"))
                return int.Parse(macrosValue);

            if (Regex.IsMatch(macrosValue, "^0b[01]+$"))
                return Convert.ToInt32(macrosValue[2..], 2);

            if (Regex.IsMatch(macrosValue, $"^{Regexes.HexLiteral}$"))
                return Convert.ToInt32(macrosValue[2..], 16);

            if (Regex.IsMatch(macrosValue, "^0[0-7]+$"))
                return Convert.ToInt32(macrosValue[1..], 8);

            throw new PreprocessorException(location, $"Cannot parse integer: {macrosValue}.");
        }
    }
}
