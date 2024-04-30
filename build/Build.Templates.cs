using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target PackTemplates => _ => _
        .DependsOn(CompileAll)
        .Executes(() =>
        {
            DotNetPack(_ => _
                .Apply(settings => !string.IsNullOrEmpty(RuntimeId) ? settings.SetRuntime(RuntimeId) : settings)
                .SetConfiguration(Configuration)
                .SetProject(Solution.Templates.Cesium_Templates_CSharp)
                .EnableNoRestore());
        });
}
