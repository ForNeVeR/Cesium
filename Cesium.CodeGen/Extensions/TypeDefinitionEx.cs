using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Extensions;

internal static class TypeDefinitionEx
{
    public static MethodDefinition DefineMethod(
        this TypeDefinition type,
        TranslationUnitContext context,
        string name,
        TypeReference returnType,
        ParametersInfo? parameters)
    {
        var method = new MethodDefinition(
            name,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType);
        AddParameters(context, method, parameters);
        type.Methods.Add(method);
        return method;
    }

    private static void AddParameters(
        TranslationUnitContext context,
        MethodReference method,
        ParametersInfo? parametersInfo)
    {
        if (parametersInfo == null) return;
        var (parameters, isVoid, isVarArg) = parametersInfo;
        if (isVoid) return;

        // TODO[#87]: Process empty (non-void) parameter list.

        foreach (var parameter in parameters)
        {
            var (type, name, _) = parameter;
            var parameterDefinition = new ParameterDefinition(type.Resolve(context))
            {
                Name = name
            };
            method.Parameters.Add(parameterDefinition);
        }
        if (isVarArg)
        {
            var parameterDefinition = new ParameterDefinition(context.TypeSystem.Void.MakePointerType())
            {
                Name = "__varargs"
            };
            method.Parameters.Add(parameterDefinition);
        }
    }
    public static TypeDefinition? GetType(this AssemblyDefinition assemblyDefinition, string typeName)
    {
        foreach (var module in assemblyDefinition.Modules)
        {
            var foundType = module.GetType(typeName);
            if (foundType != null)
            {
                return foundType;
            }
        }

        return null;
    }
    public static MethodDefinition FindMethod(this TypeDefinition typeDefinition, string methodName)
    {
        return typeDefinition.Methods.SingleOrDefault(method => method.Name == methodName)
               ?? throw new CompilationException($"Cannot find method {methodName} on type {typeDefinition.FullName}");
    }

    public static FieldDefinition GetOrAddField(this TypeDefinition typeDefinition, TranslationUnitContext context, IType type, string name)
    {
        var field = typeDefinition.Fields.FirstOrDefault(f => f.Name == name);
        if (field == null)
        {
            field = new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static, type.Resolve(context));
            typeDefinition.Fields.Add(field);
        }

        return field;
    }
}
