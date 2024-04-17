using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target TestCodeGen => _ => _
        .Executes(() => ExecuteTests(Solution.Cesium_CodeGen_Tests));

    Target TestCompiler => _ => _
        .Executes(() => ExecuteTests(Solution.Cesium_Compiler_Tests));

    Target TestIntegration => _ => _
        .Executes(() => ExecuteTests(Solution.Cesium_IntegrationTests));

    Target TestParser => _ => _
        .Executes(() => ExecuteTests(Solution.Cesium_Parser_Tests));

    Target TestRuntime => _ => _
        .Executes(() => ExecuteTests(Solution.Cesium_Runtime_Tests));

    Target TestSdk => _ => _
        .DependsOn(PackCompilerBundle)
        .DependsOn(PackSdk)
        .Executes(() => ExecuteTests(Solution.Cesium_Sdk_Tests));

    Target TestAll => _ => _
        .DependsOn(TestCodeGen)
        .DependsOn(TestCompiler)
        .DependsOn(TestIntegration)
        .DependsOn(TestParser)
        .DependsOn(TestRuntime)
        .DependsOn(TestSdk);

    void ExecuteTests(Project project)
    {
        DotNetTest(_ => _
            .SetConfiguration(Configuration)
            .SetProjectFile(project.GetMSBuildProject().ProjectFileLocation.File));
    }
}
