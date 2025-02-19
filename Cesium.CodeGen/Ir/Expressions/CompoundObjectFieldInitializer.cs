// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;
internal sealed class CompoundObjectFieldInitializer : IExpression
{
    internal IExpression Inner;
    internal Designation Designation;

    internal CompoundObjectFieldInitializer(AssignmentInitializer initializer, IDeclarationScope scope)
        : this(initializer.Expression.ToIntermediate(scope), initializer.Designation!)
    {
    }

    internal CompoundObjectFieldInitializer(IExpression inner, Designation designation)
    {
        Inner = inner;
        Designation = designation;
    }

    public void EmitTo(IEmitScope scope) => Inner.EmitTo(scope);

    public IType GetExpressionType(IDeclarationScope scope) => Inner.GetExpressionType(scope);

    public IExpression Lower(IDeclarationScope scope) => new CompoundObjectFieldInitializer(Inner.Lower(scope), Designation);
}
