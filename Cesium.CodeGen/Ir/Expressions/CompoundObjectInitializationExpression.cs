using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Immutable;

namespace Cesium.CodeGen.Ir.Expressions;
internal sealed class CompoundObjectInitializationExpression : IExpression
{
    private IType? _type;
    private FieldDefinition? _typeDef;
    private Action? _prefixAction;
    private Action? _postfixAction;
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

    public void Hint(FieldDefinition type, Action prefixAction, Action postfixAction)
    {
        _typeDef = type;
        _prefixAction = prefixAction;
        _postfixAction = postfixAction;
    }

    public void EmitTo(IEmitScope scope)
    {
        if (_type == null && _typeDef == null)
            throw new Exception("_type is null!");

        var instructions = scope.Method.Body.Instructions;
        TypeDefinition typeDef = _type != null ? ((StructType)_type).Resolve(scope.Context).Resolve() : _typeDef!.FieldType.Resolve();
        var fieldsDefs = typeDef.Fields;
        var initializers = _initializers;

        if (typeDef.IsCArray())
        {
            var element = typeDef.Fields[0].FieldType;
            for (int i = 0; i < initializers.Length; i++)
            {
                IExpression? expr = initializers[i];

                if (expr == null)
                    throw new CompilationException($"Retrieved null initializer!");

                
                if (_prefixAction is not null)
                {
                    _prefixAction();
                    instructions.Add(Instruction.Create(OpCodes.Ldflda, _typeDef));
                }
                if (i != 0)
                {
                    instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                    instructions.Add(Instruction.Create(OpCodes.Sizeof, element)); // size = sizeof(array element)
                    instructions.Add(Instruction.Create(OpCodes.Mul)); // offset = id * size
                    instructions.Add(Instruction.Create(OpCodes.Add)); // parent_struct_field_address + offset
                }
                expr.EmitTo(scope);
                instructions.Add(GetWriteInstruction(element.MetadataType));
            }
            return;
        }

        _prefixAction?.Invoke();
        var newobj = new VariableDefinition(typeDef);
        scope.Method.Body.Variables.Add(newobj);

        instructions.Add(Instruction.Create(OpCodes.Ldloca, newobj));
        instructions.Add(Instruction.Create(OpCodes.Initobj, typeDef));

        if (initializers.Length == 0) // zero init like SomeType name = { };
        {
            instructions.Add(Instruction.Create(OpCodes.Ldloc, newobj)); // push new object
            if (_postfixAction is not null)
            {
                _postfixAction();
                instructions.Add(Instruction.Create(OpCodes.Ldflda, _typeDef));
            }
            return;
        }    

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
                EmitPathToField(scope, typeDef, f);
            }
            else if (init is CompoundObjectInitializationExpression objInit)
            {                
                // UNSAFE UNSAFE UNSAFE UNSAFE UNSAFE UNSAFE UNSAFE
                objInit.Hint(
                    fieldsDefs[i],
                    () =>
                    {
                        instructions.Add(Instruction.Create(OpCodes.Ldloca, newobj));
                    },
                    () =>
                    {
                        instructions.Add(Instruction.Create(OpCodes.Stfld, fieldsDefs[i]));
                    });
                objInit.EmitTo(scope);
            }
            else if (init is AssignmentExpression assignment)
            {
                throw new CompilationException($"You cant use {assignment} in the object initialization!");
            }
            else
                throw new NotImplementedException($"Unsupported or unknown Initializer or Expression: {init.GetType().FullName}");
        }

        instructions.Add(Instruction.Create(OpCodes.Ldloc, newobj)); // push new object
        _postfixAction?.Invoke();
    }

    private static void EmitPathToField(IEmitScope scope, TypeDefinition type, CompoundObjectFieldInitializer initializer)
    {
        FieldDefinition? field = null;

        var path = initializer.Designation.Designators;
        var last = path.Length - 1;
        for (int i = 0; i < path.Length; i++)
        {
            var p = path[i];
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
            else if (p is BracketsDesignator b)
            {
                var instructions = scope.Method.Body.Instructions;
                var arrayType = field!.FieldType.Resolve();
                var element = arrayType.Fields[0].FieldType;


                b.Expression.ToIntermediate().EmitTo(scope); // element id
                instructions.Add(Instruction.Create(OpCodes.Sizeof, element)); // size = sizeof(array element)
                instructions.Add(Instruction.Create(OpCodes.Mul)); // offset = id * size

                instructions.Add(Instruction.Create(OpCodes.Add)); // ref result = ref previousField[offset]; or ref result = ref Unsafe.AddByteOffset(ref previoutsField, offset)

                if (i == last)
                {
                    initializer.Inner.EmitTo(scope); // push data
                    instructions.Add(GetWriteInstruction(element.MetadataType));
                    return;
                }

                continue;
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
                initializer.Inner.EmitTo(scope);
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

    static Instruction GetWriteInstruction(MetadataType type)
    {
        switch (type)
        {
            case MetadataType.Boolean:
            case MetadataType.Byte:
            case MetadataType.SByte:
                return Instruction.Create(OpCodes.Stind_I1);
            case MetadataType.UInt16:
            case MetadataType.Int16:
                return Instruction.Create(OpCodes.Stind_I2);
            case MetadataType.Int32:
            case MetadataType.UInt32:
                return Instruction.Create(OpCodes.Stind_I4);
            case MetadataType.Int64:
            case MetadataType.UInt64:
                return Instruction.Create(OpCodes.Stind_I8);
            case MetadataType.Single:
                return Instruction.Create(OpCodes.Stind_R4);
            case MetadataType.Double:
                return Instruction.Create(OpCodes.Stind_R8);
            case MetadataType.Pointer:
            case MetadataType.IntPtr:
            case MetadataType.UIntPtr:
            case MetadataType.ByReference:
                return Instruction.Create(OpCodes.Stind_I);
            default:
                throw new CompilationException($"This array type isnt supported: {type}");
        }
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
