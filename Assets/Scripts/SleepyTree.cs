using UnityEngine;

// A sleepy tree in the Whispering Woods. Hit it with your magic and it wakes up: it opens its
// eyes, tosses its nightcap, and sparkles. Each tree is remembered (it stays awake when you come
// back) and counted in "trees_woken", which is how Old Moss's quest "Wake the Trees" knows how
// you're doing.
public class SleepyTree : MonoBehaviour, ISpellTarget
{
    public const string WokenCounter = "trees_woken";

    [Tooltip("Unique per placed tree (set by DungeonBuilder), so it stays awake when you come back.")]
    [SerializeField] private string persistentId;
    [SerializeField] private SpriteFlipbook flipbook;
    [SerializeField] private Sprite[] sleepFrames;
    [SerializeField] private Sprite[] awakeFrames;
    [SerializeField] private float sleepFps = 1.5f;
    [SerializeField] private float awakeFps = 3f;
    [SerializeField] private GameObject sparkle;
    [SerializeField] private AudioClip wakeSound;

    public bool IsAwake { get; private set; }

    private void Start() => SetAwake(!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId));

    public void OnSpellHit(int damage, SpellElement element) => Wake();

    public void Wake()
    {
        if (IsAwake) return;
        SetAwake(true);
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        int woken = GameSession.AddToCounter(WokenCounter);
        AudioManager.Play(wakeSound);
        if (sparkle != null) Instantiate(sparkle, transform.position + Vector3.up * 2f, Quaternion.identity);
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null)
            hud.ShowToast(woken >= QuestCatalog.SleepyTreeGoal ? "That's every sleepy tree! Tell Old Moss!"
                                                                : $"The tree wakes up! ({woken}/{QuestCatalog.SleepyTreeGoal})");
    }

    private void SetAwake(bool awake)
    {
        IsAwake = awake;
        flipbook.Play(awake ? awakeFrames : sleepFrames, awake ? awakeFps : sleepFps);
    }
}
