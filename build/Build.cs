using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    public static int Main()
    {
        return Execute<Build>(x => x.CompileAll);
    }

    [Parameter("Configuration to build - Default is 'Debug' or 'Release'")]
    readonly Configuration Configuration = Configuration.Debug;

    [Parameter("If set to true, ignores all cached build results. Default: false")]
    readonly bool SkipCaches = false;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [Parameter("If set, only executes targets for a specified runtime identifier. Provided RID must be included in <RuntimeIdentifiers> property of Cesium.Compiler project.")]
    readonly string RuntimeId = string.Empty;

    string EffectiveRuntimeId => !string.IsNullOrEmpty(RuntimeId)
        ? RuntimeId
        : Solution.Cesium_Compiler.GetProperty("DefaultAppHostRuntimeIdentifier") ?? string.Empty;

    [Parameter("If set to true, publishes compiler packs in AOT mode.")]
    readonly bool PublishAot = false;

    Target Clean => _ => _
        .Before(RestoreAll)
        .Executes(() =>
        {
            DotNetClean(_ => _
                .Apply(settings => !string.IsNullOrEmpty(RuntimeId) ? settings.SetRuntime(RuntimeId) : settings));
        });

    Target RestoreAll => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .Apply(settings => !string.IsNullOrEmpty(RuntimeId) ? settings.SetRuntime(RuntimeId) : settings)
                .SetProjectFile(Solution.FileName));
        });

    Target CompileAll => _ => _
        .DependsOn(RestoreAll)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .Apply(settings => !string.IsNullOrEmpty(RuntimeId) ? settings.SetRuntime(RuntimeId) : settings)
                .SetConfiguration(Configuration)
                .SetProjectFile(Solution.FileName)
                .EnableNoRestore());
        });

    Target ForceClear => _ => _
        .OnlyWhenDynamic(() => SkipCaches)
        .Before(RestoreAll);
}
