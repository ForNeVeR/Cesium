using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System.Collections.Immutable;

namespace Cesium.CodeGen.Ir.Expressions;
internal sealed class CompoundObjectInitializationExpression : IExpression
{
    private IType? _type;
    private TypeDefinition? _typeDef;
    private readonly ImmutableArray<IExpression?> _initializers;
    private IDeclarationScope? _scope;

    public CompoundObjectInitializationExpression(IType type, ImmutableArray<IExpression?> initializers)
    {
        _type = type;
        _initializers = initializers;
    }

    public CompoundObjectInitializationExpression(ImmutableArray<IExpression?> initializers)
    {
        _initializers = initializers;
    }

    public void Hint(TypeDefinition type) => _typeDef = type;

    public void EmitTo(IEmitScope scope)
    {
        if (_type == null && _typeDef == null)
            throw new Exception("_type is null!");

        var instructions = scope.Method.Body.Instructions;
        TypeDefinition typeDef = _type != null ? ((StructType)_type).Resolve(scope.Context).Resolve() : _typeDef!;
        var fieldsDefs = typeDef.Fields;
        var initializers = _initializers;

        // it is better to use { ldflda variable; initobj<T>; }, but we need to change the order of processing Expressions
        // because right now it's being processed like this { newobj; (dup, ldnum, stfld)*; stfld variable }
        var constructor = _type != null ? ((StructType)_type).Constructor : _typeDef!.Methods.First(_ => _.Name == ".ctor");
        instructions.Add(Instruction.Create(OpCodes.Newobj, constructor));

        var newobj = new VariableDefinition(typeDef);
        scope.Method.Body.Variables.Add(newobj);

        instructions.Add(Instruction.Create(OpCodes.Stloc, newobj));

        if (initializers.Length == 0) // zero init like SomeType name = { };
            return;

        for (int i = 0; i < initializers.Length; i++)
        {
            var init = initializers[i];

            if (init == null)
                throw new CompilationException($"Retrieved null initializer!");

            if (init is ConstantLiteralExpression)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldloca, newobj));
                init.EmitTo(scope);
                instructions.Add(Instruction.Create(OpCodes.Stfld, fieldsDefs[i]));
            }
            else if (init is CompoundObjectFieldInitializer f)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldloca, newobj));
                EmitPathToField(scope, typeDef, f.Designation.Designators, f.Inner);
            }
            else if (init is CompoundObjectInitializationExpression objInit)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldloca, newobj));
                // UNSAFE UNSAFE UNSAFE UNSAFE UNSAFE UNSAFE UNSAFE
                objInit.Hint(fieldsDefs[i].FieldType.Resolve());
                objInit.EmitTo(scope);
                instructions.Add(Instruction.Create(OpCodes.Stfld, fieldsDefs[i]));
            }
            else if (init is AssignmentExpression assignment)
            {
                throw new CompilationException($"You cant use {assignment} in the object initialization!");
            }
            else
                throw new NotImplementedException($"Unsupported or unknown Initializer or Expression: {init.GetType().FullName}");
        }

        instructions.Add(Instruction.Create(OpCodes.Ldloc, newobj)); // push new object
    }

    private static void EmitPathToField(IEmitScope scope, TypeDefinition type, ImmutableArray<Designator> path, IExpression value)
    {
        // in: [struct pointer]
        var last = path.Length - 1;
        for (int i = 0; i < path.Length; i++)
        {
            var p = path[i];
            FieldDefinition field;
            if (p is IdentifierDesignator id)
            {
                field = type.Fields.FirstOrDefault(_ => _.Name == id.FieldName)!;
                if (field == null) // maybe anon?
                {
                    List<FieldDefinition> list = new(1);
                    foreach(var f in type.Fields)
                    {
                        if (ResolveAnon(id.FieldName, f, list))
                        {
                            scope.LdFldA(f);
                            var start = list.Count - 1;
                            for (int x = start; x >= 1; x--)
                            {
                                var f2 = list[x];
                                scope.LdFldA(f2);
                            }
                            field = list[0];
                            break;
                        }
                    }
                }
            }
            else
                throw new NotImplementedException();

            if (field == null)
                throw new NullReferenceException("field"); // unexpected

            if (i != last)
            {
                scope.LdFldA(field);
                type = field.FieldType.Resolve();
            }
            else
            {
                value.EmitTo(scope);
                scope.StFld(field);
            }
        }
    }

    static bool ResolveAnon(string fieldName, FieldDefinition type, List<FieldDefinition> list)
    {
        var fieldType = type.FieldType.Resolve();
        if (fieldType == null) return false;
        if (fieldType.IsPrimitive) return false; // skip primitives (int, long and etc)

        foreach (var field in fieldType.Fields)
        {
            if (field.Name == fieldName)
            {
                list.Add(field);
                return true;
            }

            var resolved = field.FieldType.Resolve();
            if (resolved == null) continue;
            if (resolved.IsPrimitive) continue; // They don't have fields, so skip them
            if ((resolved.Name.StartsWith("_Union_") || resolved.Name.StartsWith("_Anon_")) && ResolveAnon(fieldName, field, list))
            {
                list.Add(field);
                return true;
            }
        }
        return false;
    }

    public IType GetExpressionType(IDeclarationScope scope) => _type!;

    public IExpression Lower(IDeclarationScope scope)
    {
        _scope = scope;
        if (_type != null) CheckIfResolved(scope);
        return this;
    }

    private void CheckIfResolved(IDeclarationScope scope)
    {
        if (_type!.TypeKind == TypeKind.Unresolved)
            _type = scope.ResolveType(_type);
    }
}
