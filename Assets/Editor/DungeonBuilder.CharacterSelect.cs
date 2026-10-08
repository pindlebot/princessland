using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// The character select screen: the first scene in the build.
public static partial class DungeonBuilder
{
    private static void BuildCharacterSelect(SharedAssets assets)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // A plain camera so the Game view isn't empty behind the UI.
        var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.047f, 0.086f);
        cam.gameObject.AddComponent<AudioListener>();

        var ui = new GameObject("CharacterSelect");
        var doc = ui.AddComponent<UIDocument>();
        doc.panelSettings = HudPanelSettings();
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/CharacterSelect.uxml");

        var controller = ui.AddComponent<CharacterSelectController>();
        SetRefs(controller, "characters", new Object[] { assets.Wizard, assets.Princess });
        SetString(controller, "firstLevel", "Level0");
        SetRef(controller, "selectSound", Sound("ui_select"));
        SetRef(controller, "startSound", Sound("ui_start"));
        AddAudio("music_castle");

        EditorSceneManager.SaveScene(scene, CharacterSelectScenePath);
    }
}
