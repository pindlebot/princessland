using UnityEngine;

// A wall you can walk through: it looks exactly like the walls around it, but has no collider (just a
// trigger), so walking into it carries you through into a secret room. The first time, it sparkles, plays a
// sound and says so. The minimap draws it as an ordinary wall (and hides the secret room behind it) until
// you've found it (LevelMap, Minimap), and the find is remembered in a flag, one per level.
public class FakeWall : MonoBehaviour
{
    public const string FoundCounter = "secrets_found";

    [SerializeField] private GameObject sparkle;
    [SerializeField] private AudioClip foundSound;
    [SerializeField] private string message = "You found a secret passage!";

    public static string FoundFlag(string scene) => "secret:" + scene;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        Reveal();
    }

    public void Reveal()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!GameSession.Flags.Add(FoundFlag(scene))) return;
        GameSession.AddToCounter(FoundCounter);
        AudioManager.Play(foundSound);
        if (sparkle != null) Instantiate(sparkle, transform.position + Vector3.up, Quaternion.identity);
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast(message);
    }
}
