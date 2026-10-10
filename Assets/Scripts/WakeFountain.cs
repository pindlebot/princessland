using UnityEngine;
using UnityEngine.SceneManagement;

// A fountain is a waypoint. Walk up to one and it remembers you:
//   - it becomes the place you wake after a Gentle Mode nap (instead of where you came in),
//   - the first time, it heals you, saves the game, and "touches" the fountain for good, which
//     puts it on the fountain menu's travel list (FountainTravelView, press T beside a fountain),
//   - after that, the fountain menu can rest you (heal + save) or take you to another fountain.
// One fountain per scene, so "fountain:<Scene>" is the flag that says you've found it.
public class WakeFountain : MonoBehaviour
{
    public const string SpawnName = "Fountain"; // the named spawn you arrive at when you travel here

    [SerializeField] private float reach = 4f;
    [SerializeField] private Transform wakeSpot; // just in front of the basin
    [SerializeField] private AudioClip touchSound;

    public bool IsWakePoint => LevelBootstrap.Current != null && LevelBootstrap.Current.WakePoint == wakeSpot;
    public Transform WakeSpot => wakeSpot;

    public static string FlagFor(string scene) => "fountain:" + scene;
    public static bool Touched(string scene) => GameSession.Flags.Contains(FlagFor(scene));

    // The fountain in this scene that the player is standing beside (null if none is within reach).
    public static WakeFountain Near(Vector3 position)
    {
        foreach (var fountain in FindObjectsByType<WakeFountain>())
            if (fountain.IsInReach(position)) return fountain;
        return null;
    }

    public bool IsInReach(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= reach * reach;
    }

    private void Update()
    {
        var bootstrap = LevelBootstrap.Current;
        if (bootstrap == null || bootstrap.Player == null) return;
        if (!IsInReach(bootstrap.Player.transform.position)) return;

        if (!IsWakePoint) bootstrap.WakePoint = wakeSpot;
        if (!Touched(SceneManager.GetActiveScene().name)) Touch(bootstrap.Player);
    }

    private void Touch(GameObject player)
    {
        GameSession.Flags.Add(FlagFor(SceneManager.GetActiveScene().name));
        Rest(player);
        AudioManager.Play(touchSound);
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast($"The fountain remembers you! Press {GameInput.TravelKey} here to rest or travel.");
    }

    // Heals everything and saves, standing at this fountain (it's where you'll continue from).
    public static void Rest(GameObject player)
    {
        var health = player.GetComponent<Health>();
        health.Heal(health.Max);
        if (player.TryGetComponent(out Mana mana)) mana.Refill();
        SaveSystem.Autosave(SceneManager.GetActiveScene().name, SpawnName);
    }
}
