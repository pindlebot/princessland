using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Entry points for building from the command line (see ~/scripts/rebuild-isodungeon.sh):
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.RebuildScenes
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.RebuildAll
// In batch mode an exception or EditorApplication.Exit(1) makes Unity exit non-zero.
public static class CommandLineBuild
{
    private const string PlayerPath = "Builds/IsoDungeon.app";

    // Same as Dungeon > Rebuild All Scenes.
    public static void RebuildScenes() => DungeonBuilder.BuildAll();

    // Regenerates every scene, then builds the macOS player into Builds/.
    public static void RebuildAll()
    {
        DungeonBuilder.BuildAll();
        BuildPlayer();
    }

    public static void BuildPlayer()
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = PlayerPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None,
        };
        var summary = BuildPipeline.BuildPlayer(options).summary;
        Debug.Log($"[CommandLineBuild] {summary.result}: {summary.outputPath} " +
                  $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalTime})");
        if (summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
