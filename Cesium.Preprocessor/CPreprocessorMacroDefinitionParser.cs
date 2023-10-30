using System.Collections.Immutable;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser.Attributes;


namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;
using ParameterList = ImmutableArray<string>;

[Parser(typeof(CPreprocessorTokenType))]
internal partial class CPreprocessorMacroDefinitionParser
{
    [Rule("macro: WhiteSpace PreprocessingToken WhiteSpace?")]
    private static MacroDefinition MakeIdentifier(ICPreprocessorToken whitespace, ICPreprocessorToken macroName, ICPreprocessorToken? whitespace2) =>
        new ObjectMacroDefinition(macroName.Text);

    [Rule("macro: WhiteSpace PreprocessingToken '(' WhiteSpace? ')' WhiteSpace?")]
    private static MacroDefinition MakeFunction(
    ICPreprocessorToken whitespace,
    ICPreprocessorToken macroName,
    ICPreprocessorToken openParen,
    ICPreprocessorToken whitespace_,
    ICPreprocessorToken closeParen,
    ICPreprocessorToken? whitespace2) =>
        new FunctionMacroDefinition(macroName.Text, ParameterList.Empty);

    [Rule("macro: WhiteSpace PreprocessingToken '(' parameter_type_list ')' WhiteSpace?")]
    private static MacroDefinition MakeFunctionWithParameters(
    ICPreprocessorToken whitespace,
    ICPreprocessorToken macroName,
    ICPreprocessorToken openParen,
    ParameterTypeList parameters,
    ICPreprocessorToken closeParen,
    ICPreprocessorToken? whitespace2) =>
        new FunctionMacroDefinition(macroName.Text, parameters.Parameters, parameters.HasEllipsis);

    [Rule("parameter_type_list: parameter_list")]
    private static ParameterTypeList MakeParameterTypeList(ParameterList parameters) => new(parameters);

    [Rule("parameter_type_list: parameter_list ',...'")]
    private static ParameterTypeList MakeParameterTypeList(ParameterList parameters, ICPreprocessorToken _) =>
        new(parameters, true);

    [Rule("parameter_type_list: parameter_list ',' WhiteSpace? '...'")]
    private static ParameterTypeList MakeParameterTypeList(ParameterList parameters, ICPreprocessorToken _, ICPreprocessorToken __, ICPreprocessorToken ___) =>
        new(parameters, true);

    [Rule("parameter_type_list: WhiteSpace? '...' WhiteSpace?")]
    private static ParameterTypeList MakeParameterTypeList(ICPreprocessorToken _, ICPreprocessorToken __, ICPreprocessorToken ___) =>
        new(ParameterList.Empty, true);

    [Rule("parameter_list: parameter")]
    private static ParameterList MakeParameterList(string parameter) =>
        ImmutableArray.Create(parameter);

    [Rule("parameter_list: parameter_list ',' parameter")]
    private static ParameterList MakeParameterList(ParameterList prev, ICPreprocessorToken _, string parameter) =>
        prev.Add(parameter);

    [Rule("parameter: WhiteSpace? PreprocessingToken WhiteSpace?")]
    private static string MakeParameter(
    ICPreprocessorToken whitespace,
    ICPreprocessorToken parameter,
    ICPreprocessorToken whitespace_) =>
        parameter.Text;
}
