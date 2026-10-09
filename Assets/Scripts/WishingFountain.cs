using UnityEngine;

// Easter egg: toss a coin into the castle-grounds fountain and make a wish. The first two
// wishes just plink in; the third one comes true and the fountain showers you with coins.
// After that it keeps a few cheerful replies. Wishes are counted in GameSession ("wishes").
// The wishing well in the castle's courtyard is the same component with its own counter and
// flag, so its wishes come true separately.
public class WishingFountain : MonoBehaviour, IInteractable
{
    public const int WishesUntilMagic = 3;
    public const int Reward = 15;
    public const string GrantedFlag = "wish:granted";

    [Tooltip("What it's called in its messages: \"fountain\", \"well\".")]
    [SerializeField] private string thing = "fountain";
    [Tooltip("The GameSession counter its wishes are counted in.")]
    [SerializeField] private string counter = "wishes";
    [Tooltip("The flag set once its wish has come true.")]
    [SerializeField] private string grantedFlag = GrantedFlag;
    [SerializeField] private AudioClip plinkSound;
    [SerializeField] private AudioClip wishSound;
    [SerializeField] private GameObject sparkle; // played when the wish comes true

    private static readonly string[] Wishes =
    {
        "Plink! You wish for a pony. Nothing happens... yet.",
        "Plink! You wish for a castle made of cake. The water glimmers a little...",
    };

    private static readonly string[] Afterwards =
    {
        "Plink! The {thing} giggles.",
        "Plink! You wish for more wishes. Clever!",
        "Plink! A fish winks at you. Wait, there are no fish in here...",
    };

    public Vector3 Position => transform.position;
    public string Prompt => "Make a wish (1 coin)";
    public bool CanInteract => true;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    public string Interact(GameObject player)
    {
        if (!GameSession.Progress.SpendGold(1))
            return "You need a coin to make a wish. Monsters drop lots of them!";

        AudioManager.Play(plinkSound);
        int wish = GameSession.AddToCounter(counter);
        if (GameSession.Flags.Contains(grantedFlag))
            return Afterwards[(wish - 1) % Afterwards.Length].Replace("{thing}", thing);
        if (wish < WishesUntilMagic)
            return Wishes[(wish - 1) % Wishes.Length];

        GameSession.Flags.Add(grantedFlag);
        GameSession.Progress.AddGold(Reward);
        AudioManager.Play(wishSound);
        if (sparkle != null) Instantiate(sparkle, transform.position + Vector3.up * 1.5f, Quaternion.identity);
        return $"Your wish comes true! The {thing} sparkles and gives you {Reward} coins!";
    }
}
