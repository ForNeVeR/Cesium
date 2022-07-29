using Cesium.CodeGen.Contexts.Meta;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cesium.CodeGen.Contexts
{
    internal record GlobalConstructorScope(AssemblyContext Context, MethodDefinition Method) : IDeclarationScope
    {
        public AssemblyContext AssemblyContext => Context;
        public ModuleDefinition Module => Context.Module;
        public TypeSystem TypeSystem => Module.TypeSystem;
        public IReadOnlyDictionary<string, FunctionInfo> Functions => Context.Functions;
        TranslationUnitContext IDeclarationScope.Context => throw new NotImplementedException();
        public Dictionary<string, VariableDefinition> Variables => new();

        public ParameterDefinition? GetParameter(string name)
        {
            throw new NotImplementedException();
        }
    }
}
