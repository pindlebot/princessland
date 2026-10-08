using UnityEngine;

// Closes the game. Application.Quit does nothing in the editor, so there it stops Play mode
// instead. Tests set Override to see the request without ending the test run.
public static class AppQuit
{
    public static System.Action Override;

    public static void Quit()
    {
        if (Override != null) { Override(); return; }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
