using UnityEngine;
using UnityEngine.SceneManagement;

// Starts a level: spawns the chosen character at the right spawn point and hands the new
// player to everything that needs it (camera, game manager, HUD, minimap). Also holds the
// level's HUD text, and clears out enemies if this level was already won earlier.
//
// DefaultExecutionOrder(-100) makes this Awake run before other scripts' Awake/Start,
// so the player exists before anyone goes looking for it.
[DefaultExecutionOrder(-100)]
public class LevelBootstrap : MonoBehaviour
{
    [SerializeField] private CharacterDefinition defaultCharacter;
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Other places to arrive, picked by name via GameSession.NextSpawn (e.g. \"FromHouse\").")]
    [SerializeField] private Transform[] namedSpawns;
    [SerializeField] private bool playerTorch = true; // the carried light; off in daylight

    [Header("HUD text")]
    [SerializeField] private string title = "Escape the dungeon"; // "{hero}" becomes the hero's name
    [SerializeField] private bool showEnemyCount = true;
    [SerializeField] private string lockedHint = "";
    [SerializeField] private string openHint = "Find the green crystal";

    [Header("Scene objects to hook the player up to")]
    [SerializeField] private IsoCameraFollow cameraFollow;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HudController hud;
    [SerializeField] private Minimap minimap;

    public static LevelBootstrap Current { get; private set; }

    public GameObject Player { get; private set; }
    public CharacterDefinition Character { get; private set; }
    public string Title => title.Replace("{hero}", Character.DisplayName.Split(" the ")[0]);
    public bool ShowEnemyCount => showEnemyCount;
    public static string ClearedFlag => "cleared:" + SceneManager.GetActiveScene().name;
    public string Hint(bool exitOpen) => exitOpen ? openHint : lockedHint;

    private void Awake()
    {
        Current = this;
        Character = GameSession.SelectedCharacter != null ? GameSession.SelectedCharacter : defaultCharacter;

        // Won this level before (and just popped back in, e.g. from the castle)? Remove its
        // enemies before their own Awake runs, so they never count as alive.
        if (GameSession.Flags.Contains(ClearedFlag))
            foreach (var enemy in FindObjectsByType<EnemyAI>())
            {
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }

        var spawn = spawnPoint;
        foreach (var named in namedSpawns)
            if (named != null && named.name == GameSession.NextSpawn)
                spawn = named;
        GameSession.NextSpawn = null;
        GameSession.EnteredBy = spawn == spawnPoint ? "" : spawn.name;

        Player = Instantiate(Character.Prefab, spawn.position, spawn.rotation);
        Player.name = Character.Prefab.name;
        var torch = Player.transform.Find("Torch");
        if (torch != null) torch.gameObject.SetActive(playerTorch);

        cameraFollow.SetTarget(Player.transform);
        gameManager.SetPlayer(Player.GetComponent<Health>());
        hud.Bind(Player, Character);
        minimap.SetPlayer(Player.transform);
    }
}
