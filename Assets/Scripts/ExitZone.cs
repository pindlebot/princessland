using UnityEngine;
using UnityEngine.SceneManagement;

// The level's exit: walking into it either loads the next level or, if there is none,
// wins the game. It can stay locked (and invisible) until every enemy is defeated.
public class ExitZone : MonoBehaviour
{
    [Tooltip("Scene to load when the player walks in. Leave empty for the final exit (wins the game).")]
    [SerializeField] private string nextScene;
    [Tooltip("Where to arrive in the next scene (a named spawn); empty = its start.")]
    [SerializeField] private string nextSpawn = "";
    [SerializeField] private bool requireAllEnemiesDefeated;
    [SerializeField] private GameObject visual; // the green crystal, hidden while locked
    [SerializeField] private AudioClip openSound; // fanfare when a locked exit opens
    [Tooltip("Optional: the exit stays sealed until this enemy (e.g. a boss) is dead.")]
    [SerializeField] private Health guardian;

    private bool playerInside;
    private bool used;
    private bool wasOpen;

    private bool hasGuardian;

    public bool IsOpen => (!requireAllEnemiesDefeated || EnemyAI.AliveCount == 0
                           || GameSession.Flags.Contains(LevelBootstrap.ClearedFlag))
                          // A destroyed guardian compares equal to null (Unity overloads ==).
                          && (!hasGuardian || guardian == null || guardian.IsDead);

    private void Awake() => hasGuardian = guardian != null;
    public bool RequiresClear => requireAllEnemiesDefeated;

    // Remember whether the player is standing here, so the exit also works if it opens
    // while they're already inside it.
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) playerInside = false;
    }

    // Already open when the level starts (e.g. it was cleared on an earlier visit)?
    // Then there's no "it just opened" moment to celebrate.
    private void Start() => wasOpen = IsOpen;

    private void Update()
    {
        bool open = IsOpen;
        if (visual.activeSelf != open) visual.SetActive(open);
        if (open && !wasOpen && (requireAllEnemiesDefeated || hasGuardian))
        {
            AudioManager.Play(openSound);
            GameSession.Flags.Add(LevelBootstrap.ClearedFlag); // remember, in case we leave and come back
            SaveSystem.AutosaveSoon();
        }
        wasOpen = open;
        visual.transform.Rotate(0f, 60f * Time.deltaTime, 0f); // slow spin so it stands out

        if (open && playerInside && !used)
        {
            used = true;
            if (string.IsNullOrEmpty(nextScene)) GameManager.Instance.Win();
            else
            {
                GameSession.NextSpawn = string.IsNullOrEmpty(nextSpawn) ? null : nextSpawn;
                SaveSystem.Autosave(nextScene, nextSpawn); // the stairs are a door too
                SceneManager.LoadScene(nextScene);
            }
        }
    }
}
