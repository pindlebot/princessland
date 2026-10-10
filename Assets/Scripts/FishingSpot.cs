using UnityEngine;

// A fishing spot at the end of a jetty: a bucket and a rod. Press E to cast, wait for the bobber to dip (a plip
// and a ring), then press E again before the fish gets away. Press too early and it's scared off. It all runs on
// the one E button, so it needs no menu: Interact does whatever the moment calls for.
//
// Every fish is counted ("fish_caught", which Captain Clamshell's Fishing Lesson reads) and sells for a few coins; a
// rare Golden Carp is worth a lot. Once you have the Fishing Rod (Clamshell gives it) you get longer to react.
public class FishingSpot : MonoBehaviour, IInteractable
{
    public const string CaughtCounter = "fish_caught";
    public const string RodItem = "fishing_rod";

    public enum Phase { Idle, Waiting, Bite }

    [SerializeField] private SpriteRenderer bobber;
    [SerializeField] private Sprite[] floatFrames;
    [SerializeField] private Sprite[] dipFrames;
    [SerializeField] private Vector3 bobberOffset = new Vector3(0f, 0f, 2.4f); // where the water is
    [SerializeField] private float biteWindow = 1.0f;       // seconds you have to press E
    [SerializeField] private float cancelDistance = 4f;
    [SerializeField] private AudioClip castSound;
    [SerializeField] private AudioClip biteSound;
    [SerializeField] private AudioClip catchSound;

    private static readonly (string name, int coins, int weight)[] Fish =
    {
        ("Minnow", 1, 45), ("Perch", 3, 30), ("Bluegill", 5, 17), ("Rainbow Trout", 10, 7), ("Golden Carp", 30, 1),
    };

    public Phase State { get; private set; } = Phase.Idle;
    public string LastCatch { get; private set; } = "";
    public Vector3 Position => transform.position;
    public bool CanInteract => !DialogueController.IsOpen;
    public string Prompt => State == Phase.Idle ? "Cast your line" : State == Phase.Waiting ? "Pull the line in" : "Reel in!";
    public float BiteWindow => biteWindow * (GameSession.Settings.gentle ? 1.6f : 1f) + (Abilities.Has(RodItem) ? 0.5f : 0f);

    private float biteAt, escapeAt;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start()
    {
        if (bobber != null)
        {
            bobber.transform.position = transform.position + bobberOffset + Vector3.down * 0.1f;
            bobber.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (State == Phase.Idle) return;
        var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        if (player != null && Vector3.Distance(player.transform.position, transform.position) > cancelDistance) { Reel(null); return; }

        if (State == Phase.Waiting && Time.time >= biteAt) Bite();
        else if (State == Phase.Bite && Time.time >= escapeAt) Reel("It got away!");
        AnimateBobber();
    }

    public string Interact(GameObject player)
    {
        switch (State)
        {
            case Phase.Idle:
                State = Phase.Waiting;
                biteAt = Time.time + Random.Range(1.8f, 4.2f);
                AudioManager.Play(castSound);
                if (bobber != null) bobber.gameObject.SetActive(true);
                return "You cast your line. When the bobber dips, press " + GameInput.InteractKey + "!";
            case Phase.Waiting:
                Reel(null);
                return "Too soon! You pulled it in, and the fish swam off.";
            default:
                return Catch();
        }
    }

    // Public so tests can skip the wait.
    public void Bite()
    {
        if (State != Phase.Waiting) return;
        State = Phase.Bite;
        escapeAt = Time.time + BiteWindow;
        AudioManager.Play(biteSound);
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast("A bite! Press " + GameInput.InteractKey + "!");
    }

    private string Catch()
    {
        var (name, coins) = PickFish();
        LastCatch = name;
        GameSession.AddToCounter(CaughtCounter);
        if (name == "Golden Carp") GameSession.Flags.Add("found:goldencarp");
        GameSession.Progress.AddGold(coins);
        AudioManager.Play(catchSound);
        Reel(null);
        return $"You caught a {name}! It's worth {coins} {(coins == 1 ? "coin" : "coins")}. ({GameSession.GetCounter(CaughtCounter)} caught)";
    }

    private (string name, int coins) PickFish()
    {
        bool rod = Abilities.Has(RodItem);
        int total = 0;
        foreach (var f in Fish) total += f.weight + (rod && f.coins >= 10 ? f.weight * 2 : 0);
        int roll = Random.Range(0, total);
        foreach (var f in Fish)
        {
            roll -= f.weight + (rod && f.coins >= 10 ? f.weight * 2 : 0);
            if (roll < 0) return (f.name, f.coins);
        }
        return (Fish[0].name, Fish[0].coins);
    }

    private void Reel(string message)
    {
        State = Phase.Idle;
        if (bobber != null) bobber.gameObject.SetActive(false);
        if (message == null) return;
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast(message);
    }

    private void AnimateBobber()
    {
        if (bobber == null) return;
        var frames = State == Phase.Bite ? dipFrames : floatFrames;
        if (frames == null || frames.Length == 0) return;
        bobber.sprite = frames[(int)(Time.time * (State == Phase.Bite ? 6f : 3f)) % frames.Length];
    }
}
