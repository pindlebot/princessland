using UnityEngine;
using UnityEngine.SceneManagement;

// The Glimmer Mines' fast-travel line: a mine cart at a station in each of the first three rooms. Press E and
// it rattles you to the next station down the line (Mines1 > Mines2 > Mines3 > Mines1). The rails are jammed with
// rubble until Digby has his lost moles back ("thanked:digby"), then the line is open. You arrive beside the
// cart's own station (the "Cart" named spawn).
public class MineCart : MonoBehaviour, IInteractable
{
    public const string SpawnName = "Cart";
    public const string OpenCondition = "thanked:digby";
    public static readonly string[] Stations = { "Mines1", "Mines2", "Mines3" };

    [SerializeField] private AudioClip rideSound;
    [SerializeField] private AudioClip blockedSound;

    public Vector3 Position => transform.position;
    public bool CanInteract => !used && !DialogueController.IsOpen;
    public static bool IsOpen => Condition.Met(OpenCondition);
    public string Prompt => IsOpen ? $"Ride the cart to {SaveSystem.PlaceName(NextStation())}" : "Look at the cart";

    private bool used;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    // The next stop down the line from this scene (wrapping round).
    public static string NextStation()
    {
        int here = System.Array.IndexOf(Stations, SceneManager.GetActiveScene().name);
        return Stations[(here + 1) % Stations.Length];
    }

    public string Interact(GameObject player)
    {
        if (!IsOpen)
        {
            AudioManager.Play(blockedSound);
            return "The rails ahead are jammed with rubble. Digby the mole might know how to clear it.";
        }
        used = true;
        string next = NextStation();
        GameSession.NextSpawn = SpawnName;
        GameSession.AddToCounter("cart_rides");
        SaveSystem.Autosave(next, SpawnName);
        AudioManager.Play(rideSound);
        ScreenFade.GoTo(next);
        return "All aboard!";
    }
}
