using System.Collections.Immutable;
using Cesium.Ast;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir;

internal record ParametersInfo(IList<ParameterInfo> Parameters, bool IsVoid, bool IsVarArg)
{
    private static readonly ParameterDeclaration VoidParameterDeclaration = new(
        ImmutableArray.Create<IDeclarationSpecifier>(new TypeSpecifier("void")));

    public static ParametersInfo Of(ParameterTypeList parameters)
    {
        var (parameterList, hasEllipsis) = parameters;

        var isVoid = parameterList.Length == 1 && parameterList.Single() == VoidParameterDeclaration;
        if (isVoid && hasEllipsis)
            throw new NotSupportedException(
                $"Cannot declare both void and ellipsis in the same parameter type list: {parameters}.");

        if (parameterList.IsEmpty)
            throw new NotSupportedException($"Impossible: empty parameter list: {parameters}.");

        return new ParametersInfo(
            isVoid ? Array.Empty<ParameterInfo>() : parameterList.Select(ParameterInfo.Of).ToList(),
            isVoid,
            hasEllipsis);
    }
}

internal record ParameterInfo(IType Type, string Name)
{
    public static ParameterInfo Of(ParameterDeclaration declaration)
    {
        var (specifiers, declarator, abstractDeclarator) = declaration;
        throw new NotSupportedException($"Specifiers are not supported, yet: {string.Join(", ", specifiers)}.");
    }
}
