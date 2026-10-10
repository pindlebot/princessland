using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// A quick fade to black and back, to hide the load when you walk off the edge of a room (RoomEdge).
// It lives on its own object that survives scene changes, drawing a black rectangle over everything
// (IMGUI, so it needs no UI setup). GoTo fades out, loads the scene, and fades in on the other side.
public class ScreenFade : MonoBehaviour
{
    public const float Seconds = 0.3f;

    private static ScreenFade instance;
    private static Texture2D black;
    private float alpha;
    private bool fadingIn;

    // 0 = clear, 1 = black. Tests read it to see a transition happen.
    public static float Alpha => instance != null ? instance.alpha : 0f;
    public static bool IsBusy => instance != null && (instance.fadingIn || instance.alpha > 0f);

    private static ScreenFade Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("ScreenFade");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ScreenFade>();
                black = new Texture2D(1, 1);
                black.SetPixel(0, 0, Color.black);
                black.Apply();
            }
            return instance;
        }
    }

    // Fade out, load `scene`, fade back in.
    public static void GoTo(string scene) => Instance.StartCoroutine(Instance.Run(scene));

    private IEnumerator Run(string scene)
    {
        for (float t = 0f; t < Seconds; t += Time.unscaledDeltaTime)
        {
            alpha = t / Seconds;
            yield return null;
        }
        alpha = 1f;
        SceneManager.LoadScene(scene);
        yield return null; // the new scene exists from here on
        yield return null;
        fadingIn = true;
        for (float t = 0f; t < Seconds; t += Time.unscaledDeltaTime)
        {
            alpha = 1f - t / Seconds;
            yield return null;
        }
        alpha = 0f;
        fadingIn = false;
    }

    private void OnGUI()
    {
        if (alpha <= 0.001f) return;
        GUI.depth = -1000;
        var old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), black);
        GUI.color = old;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
