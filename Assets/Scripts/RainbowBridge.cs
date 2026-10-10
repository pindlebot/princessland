using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// A rainbow bridge across a chasm: a row of chasm tiles between two rainbow posts. Drawn with the Rainbow Chalk (a press
// of E at either post), after which the planks appear one by one and the invisible walls at the chasm's edges go, so you
// can walk across. A bridge stays drawn: it remembers by its id, like a chest. DungeonBuilder makes one for every pair
// of posts that face each other across chasm tiles.
public class RainbowBridge : MonoBehaviour
{
    [SerializeField] private string persistentId;
    [SerializeField] private Collider[] walls;        // the chasm tiles' invisible walls
    [SerializeField] private GameObject[] planks;     // one flat rainbow quad per chasm tile (inactive until drawn)
    [SerializeField] private AudioClip drawSound;
    [SerializeField] private GameObject sparkle;

    public bool IsDrawn { get; private set; }
    public int Length => planks.Length;
    public static string DrawnCounter => "bridges_drawn";

    private void Start()
    {
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId)) Open(instant: true);
    }

    // Draws the bridge (the hero must have the chalk; the post checks that).
    public void Draw()
    {
        if (IsDrawn) return;
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        GameSession.AddToCounter(DrawnCounter);
        AudioManager.Play(drawSound);
        Open(instant: false);
    }

    private void Open(bool instant)
    {
        IsDrawn = true;
        foreach (var wall in walls) if (wall != null) wall.enabled = false;
        NavGrid.MarkDirty();
        if (instant) foreach (var plank in planks) plank.SetActive(true);
        else StartCoroutine(Appear());
        foreach (var post in GetComponentsInChildren<RainbowPost>(true)) post.Refresh();
    }

    private IEnumerator Appear()
    {
        foreach (var plank in planks)
        {
            plank.SetActive(true);
            if (sparkle != null) Instantiate(sparkle, plank.transform.position + Vector3.up * 0.4f, Quaternion.identity);
            yield return new WaitForSeconds(0.25f);
        }
    }
}
