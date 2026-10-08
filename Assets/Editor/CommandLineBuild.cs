using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Entry points for building from the command line (see ~/scripts/rebuild-isodungeon.sh):
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.RebuildScenes
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.RebuildAll
//   Unity -batchmode -quit -projectPath . -executeMethod CommandLineBuild.ApplyAppIcon
// In batch mode an exception or EditorApplication.Exit(1) makes Unity exit non-zero.
public static class CommandLineBuild
{
    private const string PlayerPath = "Builds/Tidecrown.app";
    private const string AppIconPath = "Assets/Art/AppIcon.png"; // drawn by Tools/make_hud_sprites.py

    // Same as Dungeon > Rebuild All Scenes.
    public static void RebuildScenes() => DungeonBuilder.BuildAll();

    // Regenerates every scene, then builds the macOS player into Builds/.
    public static void RebuildAll()
    {
        DungeonBuilder.BuildAll();
        BuildPlayer();
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

    public static void BuildPlayer()
    {
        ApplyAppIcon();
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
