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

        bool isVoid;
        if (parameterList.Length == 1)
        {
            var parameter = parameterList[0];
            var (specifiers, declarator, abstractDeclarator) = parameter;
            if (specifiers.Length != 1 || declarator != null || abstractDeclarator != null) isVoid = false;
            else
            {
                isVoid = specifiers.Single() is TypeSpecifier { TypeName: "void" };
            }
        }
        else isVoid = false;

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

internal record ParameterInfo(IType Type, string? Name)
{
    public static ParameterInfo Of(ParameterDeclaration declaration)
    {
        var (specifiers, declarator, abstractDeclarator) = declaration;
        if (abstractDeclarator != null)
            throw new NotImplementedException(
                $"Parameter with abstract declarator is not supported, yet: {declaration}.");

        var (type, isConst, identifier, parameters, cliImportMemberName) = DeclarationInfo.Of(specifiers, declarator);
        if (isConst)
            throw new NotImplementedException(
                $"Const parameter isn't supported, yet: {identifier}.");

        if (parameters != null)
            throw new NotImplementedException($"Parameters with parameters are not supported, yet: {parameters}.");

        if (cliImportMemberName != null)
            throw new NotSupportedException("CLI import specifier isn't supported for a parameter.");

        return new ParameterInfo(type, identifier);
    }
}
