using UnityEngine;

// The furniture in the hero's home: bed, toilet, sink, paper towel, wardrobe, bookshelf,
// toy chest, lamp and plant. One data-driven component covers all of them: each instance
// just sets its prompt, message, sound and effect.
//
// The message can hold several answers separated by '|'; each use gives the next one, so
// the toy chest has a different toy each time you open it.
//
// Rest (the bed) works like Sit too: you climb in and lie tucked under the quilt, asleep, with your
// health and mana restored, until you press E again (or walk away). The same goes for Bathe.
// Bathe works like Sit (the bathtub: you lie back in it in your swimwear until you get out).
// With a bottle of bubble bath in your bag, getting in pours it in (using it up) and a
// mountain of bubbles heaps up over the tub until you get out.
// A sink with soap (soapPrompt set) takes two goes: soap first, then rinse; washing without
// soap works too.
// Gather hands you an ingredient for cooking (an egg from the hens' coop, flour from the pantry,
// a strawberry from the fruit bowl), one at a time: if you're already carrying one, it says so.
// Faucet turns a stream of water on and off (the bath's tap), like the lamp's light.
// Getting into the bath leaves you dripping wet; Towel (the towel shelf) dries you off.
// A fixture can count its uses (useCounter: each time you get up from the toilet, "toilet_flushes")
// and mark you with a flag while you've done it (useFlag: "unwashed", cleared by rinsing your hands,
// which Amethyra notices). After surpriseAt uses, something pops out (the toilet frog, who hands
// you his hat). Rinsing your hands at a sink counts "hands_washed".
// Read (a note, a computer) counts each look and sets its flag, then says the next message.
// Effects are saved as numbers, so new ones go at the end of the enum.
public class HouseFixture : MonoBehaviour, IInteractable
{
    public enum Effect { None, Rest, WashHands, DryHands, Sit, Lamp, Bathe, Gather, Faucet, Towel, Read }

    private const string WetHandsFlag = "wetHands";
    public const string SoapyHandsFlag = "soapyHands";
    public const string DrippingFlag = "dripping"; // out of the bath, not dried off yet
    public const string UnwashedFlag = "unwashed";      // used the toilet, haven't rinsed your hands since
    public const string HandsWashedCounter = "hands_washed";
    public const string ToiletFrogFlag = "found:toilet_frog";

    [SerializeField] private string prompt = "Use";
    [TextArea] [SerializeField] private string message;
    [SerializeField] private Effect effect;
    [SerializeField] private AudioClip sound;

    [Header("Sit")]
    [SerializeField] private string standPrompt = "Flush and stand up";
    [TextArea] [SerializeField] private string standMessage;
    [SerializeField] private AudioClip standSound;
    [Tooltip("How far the hero's sprite is lifted to sit on top (world units).")]
    [SerializeField] private float seatHeight = 0.85f;
    [Tooltip("How far toward the camera the hero sits, so they're drawn in front of the seat.")]
    [SerializeField] private float seatForward = 0.35f;
    [Tooltip("How far along the screen's right the hero sits from the middle (negative = left): the bed's left pillow.")]
    [SerializeField] private float seatSideways;

    [Header("Soap (a sink)")]
    [Tooltip("Shown until you've soaped your hands, e.g. \"Pump the soap\". Empty = no soap step.")]
    [SerializeField] private string soapPrompt;
    [TextArea] [SerializeField] private string soapMessage;
    [SerializeField] private AudioClip soapSound;

    [Header("Bath")]
    [Tooltip("Getting into the bath uses one of these from the bag, if you have one...")]
    [SerializeField] private ItemDefinition bubbleItem;
    [Tooltip("...and shows this (the heap of bubbles) until you get out.")]
    [SerializeField] private GameObject bubbles;
    [TextArea] [SerializeField] private string bubbleMessage;
    [SerializeField] private AudioClip bubbleSound;

    [Header("Lamp")]
    [SerializeField] private Light lamp;
    [Tooltip("The prompt while the lamp (or the faucet) is off.")]
    [SerializeField] private string offPrompt = "Turn the lamp on";

    [Header("Faucet")]
    [Tooltip("The running water, shown while the faucet is on (its AmbientLoop plays the sound).")]
    [SerializeField] private GameObject stream;
    [TextArea] [SerializeField] private string offMessage;

    [Header("Towel")]
    [Tooltip("Said when the towel dries you off after a bath (otherwise it says its message).")]
    [TextArea] [SerializeField] private string dryMessage;

    [Header("Gather")]
    [Tooltip("What you get (one at a time).")]
    [SerializeField] private ItemDefinition gift;
    [Tooltip("Said when you're already carrying one.")]
    [TextArea] [SerializeField] private string haveOneMessage = "You already have one.";
    [TextArea] [SerializeField] private string bagFullMessage = "Your bag is full!";

    [Header("Counting uses (the toilet)")]
    [Tooltip("A GameSession counter that goes up each time you get up from this seat. Empty = don't count.")]
    [SerializeField] private string useCounter;
    [Tooltip("A flag set each time (cleared elsewhere, e.g. 'unwashed' until you rinse your hands).")]
    [SerializeField] private string useFlag;
    [Tooltip("After this many uses, a surprise pops out (0 = never).")]
    [SerializeField] private int surpriseAt;
    [SerializeField] private GameObject surprisePrefab;   // Sir Hopsalot's cousin, hopping out
    [SerializeField] private ItemDefinition surpriseGift; // ...and the hat he gives you
    [TextArea] [SerializeField] private string surpriseMessage;
    [SerializeField] private AudioClip surpriseSound;

    private int uses;
    private bool wasSittingHere;

    public Effect Kind => effect;
    public Vector3 Position => transform.position;
    public bool CanInteract => true;

    public string Prompt
    {
        get
        {
            if (IsSeatEffect && IsSittingHere(Player())) return standPrompt;
            if (effect == Effect.WashHands && NeedsSoap) return soapPrompt;
            if (effect == Effect.Lamp && lamp != null && !lamp.enabled) return offPrompt;
            if (effect == Effect.Faucet && !IsRunning) return offPrompt;
            return prompt;
        }
    }

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    public string Interact(GameObject player)
    {
        switch (effect)
        {
            case Effect.Sit:
            case Effect.Bathe:
            case Effect.Rest:
                var hero = player.GetComponent<PlayerController>();
                if (IsSittingHere(hero))
                {
                    if (bubbles != null) bubbles.SetActive(false);
                    hero.StandUp();
                    AudioManager.Play(standSound);
                    return standMessage;
                }
                Vector3 towardCamera = Camera.main != null
                    ? -Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized
                    : Vector3.back;
                Vector3 across = Camera.main != null
                    ? Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized
                    : Vector3.right;
                hero.SitOn(transform, transform.position + towardCamera * seatForward + across * seatSideways, seatHeight,
                           bathing: effect == Effect.Bathe, sleeping: effect == Effect.Rest);
                if (effect == Effect.Rest)
                {
                    // A nap restores everything, the moment you're tucked in.
                    var rested = player.GetComponent<Health>();
                    rested.Heal(rested.Max);
                    player.GetComponent<Mana>().Refill();
                }
                if (effect == Effect.Bathe) GameSession.Flags.Add(DrippingFlag);
                if (effect == Effect.Bathe && bubbleItem != null && bubbles != null
                    && player.TryGetComponent(out Inventory bag) && bag.Remove(bubbleItem))
                {
                    bubbles.SetActive(true);
                    GameSession.AddToCounter("bubbleBaths");
                    AudioManager.Play(sound);
                    AudioManager.Play(bubbleSound);
                    return bubbleMessage;
                }
                break;

            case Effect.WashHands:
                if (NeedsSoap)
                {
                    GameSession.Flags.Add(SoapyHandsFlag);
                    AudioManager.Play(soapSound);
                    return soapMessage;
                }
                GameSession.Flags.Remove(SoapyHandsFlag); // rinsed off
                GameSession.Flags.Add(WetHandsFlag);
                GameSession.Flags.Remove(UnwashedFlag);
                GameSession.AddToCounter(HandsWashedCounter);
                break;

            case Effect.DryHands:
                // Remove() returns false if the flag wasn't there: nobody washed first.
                if (!GameSession.Flags.Remove(WetHandsFlag))
                {
                    AudioManager.Play(sound);
                    return "Your hands are already dry. Did you wash them?";
                }
                break;

            case Effect.Gather:
                if (gift != null && player.TryGetComponent(out Inventory basket))
                {
                    if (basket.Count(gift) > 0) return haveOneMessage;
                    if (!basket.Add(gift)) return bagFullMessage;
                }
                break;

            case Effect.Read: // a note or a computer: each look counts and sets its flag (Dad's Workshop)
                if (!string.IsNullOrEmpty(useCounter)) GameSession.AddToCounter(useCounter);
                if (!string.IsNullOrEmpty(useFlag)) GameSession.Flags.Add(useFlag);
                break;

            case Effect.Towel:
                if (GameSession.Flags.Remove(DrippingFlag))
                {
                    AudioManager.Play(sound);
                    return dryMessage;
                }
                break;

            case Effect.Faucet:
                if (stream != null)
                {
                    stream.SetActive(!stream.activeSelf);
                    AudioManager.Play(sound);
                    return stream.activeSelf ? NextMessage() : offMessage;
                }
                break;

            case Effect.Lamp:
                if (lamp != null)
                {
                    lamp.enabled = !lamp.enabled;
                    AudioManager.Play(sound);
                    return lamp.enabled ? "Click! The lamp is on." : "Click! The lamp is off. Spooky!";
                }
                break;
        }
        AudioManager.Play(sound);
        return NextMessage();
    }

    private string NextMessage()
    {
        if (string.IsNullOrEmpty(message)) return message;
        var answers = message.Split('|');
        return answers[uses++ % answers.Length].Trim();
    }

    public bool HasBubbles => bubbles != null && bubbles.activeSelf;
    public bool IsRunning => stream != null && stream.activeSelf;
    public ItemDefinition Gift => gift;

    // Walking out of the bath (rather than pressing E) pops the bubbles too.
    private void Update()
    {
        bool sitting = IsSittingHere(Player());
        if (HasBubbles && !sitting) bubbles.SetActive(false);
        // Getting up counts as a use, whether you pressed E or just walked away.
        if (wasSittingHere && !sitting) OnGotUp();
        wasSittingHere = sitting;
    }

    private void OnGotUp()
    {
        if (!string.IsNullOrEmpty(useCounter)) GameSession.AddToCounter(useCounter);
        if (!string.IsNullOrEmpty(useFlag)) GameSession.Flags.Add(useFlag);
        TrySurprise();
    }

    // The tenth flush: a frog pops out of the bowl and gives you his hat (once; if your bag is
    // full he waits for the next flush).
    private void TrySurprise()
    {
        if (surpriseAt <= 0 || string.IsNullOrEmpty(useCounter) || GameSession.Flags.Contains(ToiletFrogFlag)) return;
        if (GameSession.GetCounter(useCounter) < surpriseAt) return;
        var hero = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        if (hero == null || !hero.TryGetComponent(out Inventory bag)) return;
        if (surpriseGift != null && !bag.Add(surpriseGift)) return; // no room: try again next time

        GameSession.Flags.Add(ToiletFrogFlag);
        AudioManager.Play(surpriseSound);
        if (surprisePrefab != null)
        {
            var frog = Instantiate(surprisePrefab, transform.position + new Vector3(-1f, 0f, -1.4f), Quaternion.identity);
            if (frog.TryGetComponent(out Frog hop)) hop.HopNow();
        }
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast(surpriseMessage);
    }

    private bool NeedsSoap => !string.IsNullOrEmpty(soapPrompt) && !GameSession.Flags.Contains(SoapyHandsFlag);

    private bool IsSeatEffect => effect == Effect.Sit || effect == Effect.Bathe || effect == Effect.Rest;

    private bool IsSittingHere(PlayerController hero) => hero != null && hero.Seat == transform;

    private static PlayerController Player() =>
        LevelBootstrap.Current != null && LevelBootstrap.Current.Player != null
            ? LevelBootstrap.Current.Player.GetComponent<PlayerController>()
            : null;
}
