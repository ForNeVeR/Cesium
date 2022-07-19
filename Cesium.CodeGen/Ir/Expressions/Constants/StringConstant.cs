using Cesium.CodeGen.Contexts;
using Cesium.Parser;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal class StringConstant : IConstant
{
    private readonly string _value;
    public StringConstant(IToken<CTokenType> token)
    {
        if (token.Kind != CTokenType.StringLiteral)
            throw new NotSupportedException($"Not supported token kind for a string constant: {token.Kind}.");
        _value = token.UnwrapStringLiteral();
    }

    public void EmitTo(IDeclarationScope scope)
    {
        var fieldReference = scope.AssemblyContext.GetConstantPoolReference(_value);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, fieldReference));
    }

    // TODO: String? Not a char* or something?
    public TypeReference GetConstantType(IDeclarationScope scope) => scope.TypeSystem.String;
}
