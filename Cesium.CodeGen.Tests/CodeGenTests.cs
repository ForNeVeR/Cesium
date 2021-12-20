using System;
using System.Linq;
using Cesium.Parser;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Xunit;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Tests;

public class CodeGenTests
{
    [Fact]
    public void EmptyMainTest()
    {
        const string source = "int main() {}";
        var translationUnit = new CParser(new CLexer(source)).ParseTranslationUnit().Ok.Value;
        var assembly = Generator.GenerateAssembly(
            translationUnit,
            new AssemblyNameDefinition("test", new Version()),
            ModuleKind.Console);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        var method = moduleType.GetMethods().Single();
        Assert.Equal("main", method.Name);
    }
}
