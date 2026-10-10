using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A stone brazier. A spell lights it: fire sets it burning, water fills its basin with sparkling
// water. Either way it counts as lit, so neither hero gets stuck. Light every brazier on a level and
// something appears (an "item <id> hidden braziers" on the map: BossReward waits for it). Each one
// remembers being lit by where it stands.
public class Brazier : MonoBehaviour, ISpellTarget
{
    public const string AllLitCounter = "braziers_lit";

    [SerializeField] private string persistentId;
    [SerializeField] private SpriteFlipbook flipbook;
    [SerializeField] private Sprite[] unlitFrames, fireFrames, waterFrames;
    [SerializeField] private float fireFps = 6f;
    [SerializeField] private float waterFps = 4f;
    [SerializeField] private Light glow;
    [SerializeField] private GameObject sparkle;
    [SerializeField] private AudioClip lightSound;

    private static readonly List<Brazier> all = new List<Brazier>();
    public static IReadOnlyList<Brazier> All => all;
    public static event System.Action AllLit;

    public bool IsLit { get; private set; }
    public SpellElement LitBy { get; private set; }

    // Set (and saved) once every brazier on a level has been lit.
    public static string DoneFlag(string scene) => "braziers:" + scene;
    private string WaterFlag => "brazier_water:" + persistentId;

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void Start()
    {
        bool lit = !string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId);
        Show(lit, lit && GameSession.Flags.Contains(WaterFlag) ? SpellElement.Water : SpellElement.Fire);
    }

    public void OnSpellHit(int damage, SpellElement element) => Light(element);

    public void Light(SpellElement element)
    {
        if (IsLit) return;
        var kind = element == SpellElement.Water ? SpellElement.Water : SpellElement.Fire;
        if (!string.IsNullOrEmpty(persistentId))
        {
            GameSession.MarkUsed(persistentId);
            if (kind == SpellElement.Water) GameSession.Flags.Add(WaterFlag);
        }
        GameSession.AddToCounter(AllLitCounter);
        Show(true, kind);
        AudioManager.Play(lightSound);
        if (sparkle != null) Instantiate(sparkle, transform.position + Vector3.up * 1.6f, Quaternion.identity);

        // That was the last one?
        if (all.All(b => b.IsLit))
        {
            GameSession.Flags.Add(DoneFlag(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            AllLit?.Invoke();
        }
    }

    private void Show(bool lit, SpellElement kind)
    {
        IsLit = lit;
        LitBy = kind;
        if (!lit) flipbook.Play(unlitFrames, 1f);
        else flipbook.Play(kind == SpellElement.Water ? waterFrames : fireFrames, kind == SpellElement.Water ? waterFps : fireFps);
        if (glow != null)
        {
            glow.enabled = lit;
            glow.color = kind == SpellElement.Water ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.6f, 0.25f);
        }
    }
}
