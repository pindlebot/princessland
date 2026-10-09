using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Entry points for building from the command line (see ~/scripts/rebuild-tidecrown.sh):
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.RebuildScenes
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.RebuildAll
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.ApplyAppIcon
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.RebuildLinux  (scenes + Linux player
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.BuildLinux     for the Steam Deck; see
//                                                        ~/scripts/deploy-tidecrown-to-deck.sh)
// In batch mode an exception or EditorApplication.Exit(1) makes Unity exit non-zero.
public static class CommandLineBuild
{
    private const string PlayerPath = "Builds/Tidecrown.app";
    // A folder, not one file: the executable sits beside Tidecrown_Data/ and UnityPlayer.so.
    private const string LinuxPlayerPath = "Builds/Linux/Tidecrown.x86_64";
    private const string AppIconPath = "Assets/Art/AppIcon.png"; // drawn by Tools/make_hud_sprites.py

    // Same as Dungeon > Rebuild All Scenes.
    public static void RebuildScenes() => DungeonBuilder.BuildAll();

    // Regenerates every scene, then builds the macOS player into Builds/.
    public static void RebuildAll()
    {
        DungeonBuilder.BuildAll();
        BuildPlayer();
    }

    // Regenerates every scene, then builds the Linux (Steam Deck) player into Builds/Linux/.
    public static void RebuildLinux()
    {
        DungeonBuilder.BuildAll();
        BuildLinux();
    }

    // Makes AppIcon.png the default icon (the Mac app's Finder and Dock icon). Saved into
    // ProjectSettings, and applied again before every build in case the PNG was redrawn.
    public static void ApplyAppIcon()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(AppIconPath);
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        AssetDatabase.SaveAssets();
    }

    public static void BuildPlayer() => Build(BuildTarget.StandaloneOSX, PlayerPath);

    // A 64-bit Linux build: what the Steam Deck runs natively (no Proton needed). Needs Unity
    // Hub's "Linux Build Support (Mono)" module. The scenes are built as they are; run
    // RebuildScenes first (or use deploy-tidecrown-to-deck.sh) if the level files changed.
    public static void BuildLinux()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
        {
            Debug.LogError("[CommandLineBuild] Linux Build Support isn't installed. Unity Hub > Installs > " +
                           $"{Application.unityVersion} > Add modules > Linux Build Support (Mono).");
            EditorApplication.Exit(1);
            return;
        }
        Build(BuildTarget.StandaloneLinux64, LinuxPlayerPath);
    }

    private static void Build(BuildTarget target, string path)
    {
        ApplyAppIcon();
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        };
        var summary = BuildPipeline.BuildPlayer(options).summary;
        Debug.Log($"[CommandLineBuild] {summary.result}: {summary.outputPath} " +
                  $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalTime})");
        if (summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
