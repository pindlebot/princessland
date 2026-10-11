using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-off (and repeatable) setup for the Universal Render Pipeline: creates the pipeline and renderer assets under
// Assets/Settings and makes them the project's pipeline at every quality level. Run:
//   Unity -batchmode -quit -projectPath . -executeMethod UrpSetup.Run
// (The materials are switched by DungeonBuilder.Mat when the scenes are rebuilt.)
public static class UrpSetup
{
    private const string Folder = "Assets/Settings";

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>($"{Folder}/TidecrownRenderer.asset");
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, $"{Folder}/TidecrownRenderer.asset");
        }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>($"{Folder}/TidecrownURP.asset");
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, $"{Folder}/TidecrownURP.asset");
        }

        // The built-in pipeline's look: a sun with crisp shadows, a few per-pixel point lights, no MSAA (pixel art), no HDR.
        var so = new SerializedObject(pipeline);
        void Set(string name, int value)
        {
            var property = so.FindProperty(name);
            if (property == null) { Debug.LogWarning($"[UrpSetup] no setting {name}"); return; }
            property.intValue = value;
        }
        void SetBool(string name, bool value)
        {
            var property = so.FindProperty(name);
            if (property == null) { Debug.LogWarning($"[UrpSetup] no setting {name}"); return; }
            property.boolValue = value;
        }
        SetBool("m_SupportsHDR", false);
        Set("m_MSAA", 1);
        Set("m_MainLightShadowmapResolution", 2048);
        Set("m_AdditionalLightsRenderingMode", 2);      // per pixel
        Set("m_AdditionalLightsPerObjectLimit", 8);
        SetBool("m_RequireDepthTexture", false);
        SetBool("m_RequireOpaqueTexture", false);
        var distance = so.FindProperty("m_ShadowDistance");
        if (distance != null) distance.floatValue = 40f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
        EditorUtility.SetDirty(renderer);

        GraphicsSettings.defaultRenderPipeline = pipeline;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[UrpSetup] URP is the project's render pipeline.");
    }
}
