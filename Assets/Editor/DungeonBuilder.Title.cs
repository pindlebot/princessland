using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// The title screen with the three save slots: the first scene in the build.
public static partial class DungeonBuilder
{
    private static void BuildTitle(SharedAssets assets)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.047f, 0.086f);
        cam.gameObject.AddComponent<AudioListener>();

        var ui = new GameObject("Title");
        var doc = ui.AddComponent<UIDocument>();
        doc.panelSettings = HudPanelSettings();
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Title.uxml");

        var controller = ui.AddComponent<TitleController>();
        SetRefs(controller, "heroes", new Object[] { assets.Wizard, assets.Princess });
        SetString(controller, "characterSelectScene", "CharacterSelect");
        SetRef(controller, "selectSound", Sound("ui_select"));
        SetRef(controller, "startSound", Sound("ui_start"));
        AddAudio("music_castle");

        EditorSceneManager.SaveScene(scene, TitleScenePath);
    }
}
