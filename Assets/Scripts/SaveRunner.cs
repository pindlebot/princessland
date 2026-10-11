using UnityEngine;

// Writes the autosave a moment after something worth keeping happens (SaveSystem.AutosaveSoon), so a burst of events is
// one write, and writes it straight away when the game is paused, loses focus or quits (a phone call, closing the
// window), so a session can end at any moment without losing a reward. It lives on one hidden object that survives
// scene loads.
public class SaveRunner : MonoBehaviour
{
    private const float Delay = 1.5f;   // seconds to wait for more events before writing
    private static SaveRunner instance;
    private bool pending;
    private float dueAt;

    public static bool IsPending => instance != null && instance.pending;

    public static void Request()
    {
        if (GameSession.Slot < 0) return;   // tests and Play in a level scene don't save
        if (instance == null)
        {
            var go = new GameObject("SaveRunner") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SaveRunner>();
        }
        if (!instance.pending) instance.dueAt = Time.unscaledTime + Delay;
        instance.pending = true;
    }

    // Drop anything waiting (a test cleaning up, or a new game starting).
    public static void Cancel()
    {
        if (instance != null) instance.pending = false;
    }

    // Write now if anything is waiting.
    public static void Flush()
    {
        if (instance == null || !instance.pending) return;
        instance.pending = false;
        SaveSystem.AutosaveHere();
    }

    private void Update()
    {
        if (pending && Time.unscaledTime >= dueAt) Flush();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) SaveNow();
    }

    private void OnApplicationQuit() => SaveNow();

    // Closing, pausing or losing focus: save even if nothing special happened, so progress since the last door is kept.
    private void SaveNow()
    {
        pending = false;
        SaveSystem.AutosaveHere();
    }
}
