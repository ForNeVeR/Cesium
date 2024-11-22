using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir;

internal record ParametersInfo(IList<ParameterInfo> Parameters, bool IsVoid, bool IsVarArg)
{
    public static ParametersInfo? Of(ParameterTypeList? parameters, IDeclarationScope scope)
    {
        if (parameters == null) return null;
        var (parameterList, hasEllipsis) = parameters;

        bool isVoid;
        if (parameterList.Length == 1)
        {
            var parameter = parameterList[0];
            var (specifiers, declarator, abstractDeclarator) = parameter;
            if (specifiers.Length != 1 || declarator != null || abstractDeclarator != null) isVoid = false;
            else
            {
                isVoid = specifiers.Single() is SimpleTypeSpecifier { TypeName: "void" };
            }
        }
        else isVoid = false;

        if (isVoid && hasEllipsis)
            throw new CompilationException(
                $"Cannot declare both void and ellipsis in the same parameter type list: {parameters}.");

        if (parameterList.IsEmpty)
            throw new AssertException($"Impossible: empty parameter list: {parameters}.");

        return new ParametersInfo(
            isVoid ? Array.Empty<ParameterInfo>() : parameterList.Select((declaration, index) => ParameterInfo.Of(declaration, index, scope)).ToList(),
            isVoid,
            hasEllipsis);
    }
}

internal record ParameterInfo(IType Type, string? Name, int Index)
{
    public static ParameterInfo Of(ParameterDeclaration declaration, int index, IDeclarationScope scope)
    {
        var (specifiers, declarator, abstractDeclarator) = declaration;
        var (type, identifier, cliImportMemberName) = (declarator, abstractDeclarator) switch
        {
            (null, { }) => LocalDeclarationInfo.Of(specifiers, abstractDeclarator, scope),
            (_, null) => LocalDeclarationInfo.Of(specifiers, declarator, null, scope),
            _ => throw new AssertException(
                $"Both declarator and abstract declarator found for declaration {declaration}.")
        };

        if (cliImportMemberName != null)
            throw new CompilationException("CLI import specifier isn't supported for a parameter.");

        return new ParameterInfo(type, identifier, index);
    }
}
