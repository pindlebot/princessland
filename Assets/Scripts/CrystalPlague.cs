using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The Castle Grounds are overrun with dark green crystals: the Grey Gloom's plague, spreading since he stole
// the Amethyst. Its children are the crystals (DungeonBuilder scatters them). They stay until the condition
// holds ("has:amethyst"), then shatter away one by one (with a fanfare) the first time you arrive after that.
// The map file says so with a "plague_until:" header; levels without one are clean.
public class CrystalPlague : MonoBehaviour
{
    [Tooltip("A Condition (Condition.cs): once it holds, the crystals are gone.")]
    [SerializeField] private string clearedWhen = "has:amethyst";
    [SerializeField] private float waitSeconds = 1.2f;    // how long you see the plague before it breaks
    [SerializeField] private float shatterSeconds = 3.5f; // how long the whole field takes to go
    [SerializeField] private AudioClip clearSound;

    private float arrivedAt;
    private bool shattering;

    public bool IsCleared => Condition.Met(clearedWhen);
    public bool IsGone { get; private set; }
    public int CrystalCount => transform.childCount;
    public static string SeenFlag(string scene) => "plague_seen:" + scene;

    private void Start()
    {
        arrivedAt = Time.time;
        // Already cleared on an earlier visit: no crystals this time.
        if (IsCleared && GameSession.Flags.Contains(SeenFlag(SceneManager.GetActiveScene().name))) Vanish();
    }

    private void Update()
    {
        if (IsGone || shattering || !IsCleared || Time.time - arrivedAt < waitSeconds) return;

        if (GameSession.Flags.Add(SeenFlag(SceneManager.GetActiveScene().name)))
        {
            AudioManager.Play(clearSound);
            var hud = FindAnyObjectByType<HudController>();
            if (hud != null) hud.ShowToast("The crystals are crumbling away!");
        }
        shattering = true;
        foreach (Transform crystal in transform)
            StartCoroutine(Shatter(crystal, Random.value * (shatterSeconds - 0.6f)));
        Invoke(nameof(Vanish), shatterSeconds + 0.2f);
    }

    // A crystal shivers, then shrinks away.
    private IEnumerator Shatter(Transform crystal, float delay)
    {
        yield return new WaitForSeconds(delay);
        Vector3 size = crystal.localScale;
        for (float t = 0f; t < 0.6f && crystal != null; t += Time.deltaTime)
        {
            float k = t / 0.6f;
            crystal.localScale = size * (1f - k * k);
            yield return null;
        }
        if (crystal != null) crystal.gameObject.SetActive(false);
    }

    private void Vanish()
    {
        IsGone = true;
        foreach (Transform crystal in transform) crystal.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}
