using UnityEngine;

// Sir Hopsalot: hops about near where he came out of his bush, and says ribbit if you
// say hello. Say hello ten times and he does a big celebratory leap.
public class Frog : MonoBehaviour, IInteractable
{
    [SerializeField] private SpriteFlipbook flipbook;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] hopFrames;
    [SerializeField] private AudioClip ribbitSound;
    [SerializeField] private float wander = 1.6f;   // how far from home he hops
    [SerializeField] private float hopSeconds = 0.45f;

    public const int HellosForTheBigLeap = 10;

    private Vector3 home, from, to;
    private float hopStart = -1f, nextHopAt, height = 0.6f;

    public bool IsHopping => hopStart >= 0f;
    public Vector3 Position => transform.position;
    public string Prompt => "Say hi to Sir Hopsalot";
    public bool CanInteract => true;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Awake()
    {
        home = transform.position;
        nextHopAt = Time.time + Random.Range(1f, 3f);
    }

    public string Interact(GameObject player)
    {
        AudioManager.Play(ribbitSound);
        int hellos = GameSession.AddToCounter("frog_hellos");
        if (hellos == HellosForTheBigLeap)
        {
            Hop(transform.position, 2.2f);
            return "Sir Hopsalot does a big happy leap! Ribbit ribbit!";
        }
        return hellos % 2 == 1 ? "Ribbit!" : "Ribbit ribbit! (That means hello in Frog.)";
    }

    public void HopNow() => Hop(home + new Vector3(Random.Range(-wander, wander), 0f, Random.Range(-wander, wander) * 0.5f), 0.6f);

    private void Hop(Vector3 target, float jump)
    {
        from = transform.position;
        to = target;
        height = jump;
        hopStart = Time.time;
        flipbook.Play(hopFrames, 6f);
        if (to.x != from.x) flipbook.GetComponent<SpriteRenderer>().flipX = to.x > from.x;
    }

    private void Update()
    {
        if (IsHopping)
        {
            float t = (Time.time - hopStart) / hopSeconds;
            if (t >= 1f)
            {
                transform.position = to;
                hopStart = -1f;
                flipbook.Play(idleFrames, 2f);
                nextHopAt = Time.time + Random.Range(2f, 5f);
                return;
            }
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * height;
        }
        else if (Time.time >= nextHopAt)
        {
            HopNow();
        }
    }
}
