using UnityEngine;

// Everything inside a secret room (the floor, the desk, the star shard), kept out of sight until the level's
// fake wall has been walked through. Walls are low in this game, so without this the room behind a fake wall
// would show over the top of it. DungeonBuilder puts the room's objects under "Contents".
public class SecretRoom : MonoBehaviour
{
    [SerializeField] private GameObject contents;

    public bool IsShown => contents.activeSelf;

    private void Start() => contents.SetActive(Found());

    private void Update()
    {
        if (!contents.activeSelf && Found()) contents.SetActive(true);
    }

    private static bool Found() =>
        GameSession.Flags.Contains(FakeWall.FoundFlag(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
}
