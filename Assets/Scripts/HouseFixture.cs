using UnityEngine;

// The furniture in the hero's home: bed, toilet, sink, paper towel, wardrobe, bookshelf,
// toy chest, lamp and plant. One data-driven component covers all of them: each instance
// just sets its prompt, message, sound and effect.
//
// The message can hold several answers separated by '|'; each use gives the next one, so
// the toy chest has a different toy each time you open it.
public class HouseFixture : MonoBehaviour, IInteractable
{
    public enum Effect { None, Rest, WashHands, DryHands, Sit, Lamp }

    private const string WetHandsFlag = "wetHands";

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

    [Header("Lamp")]
    [SerializeField] private Light lamp;
    [SerializeField] private string offPrompt = "Turn the lamp on";

    private int uses;

    public Effect Kind => effect;
    public Vector3 Position => transform.position;
    public bool CanInteract => true;

    public string Prompt
    {
        get
        {
            if (effect == Effect.Sit && IsSittingHere(Player())) return standPrompt;
            if (effect == Effect.Lamp && lamp != null && !lamp.enabled) return offPrompt;
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
                var hero = player.GetComponent<PlayerController>();
                if (IsSittingHere(hero))
                {
                    hero.StandUp();
                    AudioManager.Play(standSound);
                    return standMessage;
                }
                Vector3 towardCamera = Camera.main != null
                    ? -Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized
                    : Vector3.back;
                hero.SitOn(transform, transform.position + towardCamera * seatForward, seatHeight);
                break;

            case Effect.Rest:
                var health = player.GetComponent<Health>();
                health.Heal(health.Max);
                player.GetComponent<Mana>().Refill();
                break;

            case Effect.WashHands:
                GameSession.Flags.Add(WetHandsFlag);
                break;

            case Effect.DryHands:
                // Remove() returns false if the flag wasn't there: nobody washed first.
                if (!GameSession.Flags.Remove(WetHandsFlag))
                {
                    AudioManager.Play(sound);
                    return "Your hands are already dry. Did you wash them?";
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

    private bool IsSittingHere(PlayerController hero) => hero != null && hero.Seat == transform;

    private static PlayerController Player() =>
        LevelBootstrap.Current != null && LevelBootstrap.Current.Player != null
            ? LevelBootstrap.Current.Player.GetComponent<PlayerController>()
            : null;
}
