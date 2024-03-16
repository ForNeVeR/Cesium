using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    public static int Main()
    {
        // while (!Debugger.IsAttached) Thread.Sleep(100);
        return Execute<Build>(x => x.CompileAll);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("If set to true, ignores all cached build results. Default: false")]
    readonly bool SkipCaches = false;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    Target Clean => _ => _
        .Before(RestoreAll)
        .Executes(() =>
        {
            DotNetClean();
        });

    Target RestoreAll => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution.FileName));
        });

    Target CompileAll => _ => _
        .DependsOn(RestoreAll)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetConfiguration(Configuration)
                .SetProjectFile(Solution.FileName)
                .EnableNoRestore());
        });

    Target ForceClear => _ => _
        .OnlyWhenDynamic(() => SkipCaches)
        .Before(RestoreAll);
}
