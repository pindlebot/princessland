using UnityEngine;

// Plays a list of sprites in order: a lightweight alternative to an Animator for
// effects that only ever play one animation. (Characters need a state machine to
// switch between Idle/Walk/Cast/...; a fireball or explosion doesn't.)
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private bool loop = true;
    [Tooltip("Destroy the whole effect (the root object) after the last frame. Ignored when looping.")]
    [SerializeField] private bool destroyWhenDone;
    [Tooltip("Start looping animations at a random point, so many copies (torches, grass) don't move in sync.")]
    [SerializeField] private bool randomStart;
    [Tooltip("Keep animating while the game is paused (Time.timeScale = 0), e.g. an NPC mid-conversation.")]
    [SerializeField] private bool unscaledTime;

    private SpriteRenderer spriteRenderer;
    private float startTime;

    public float Duration => frames.Length / fps;
    private float Now => unscaledTime ? Time.unscaledTime : Time.time;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startTime = Now;
        if (randomStart && loop) startTime -= Random.value * Duration;
        spriteRenderer.sprite = frames[0];
    }

    private void Start() => Update(); // show the right (possibly random) frame on the first frame

    // Switch to a different animation (e.g. the dragon's Idle -> Talk), from its first frame.
    public void Play(Sprite[] newFrames, float newFps)
    {
        frames = newFrames;
        fps = newFps;
        startTime = Now;
        Update();
    }

    private void Update()
    {
        int frame = (int)((Now - startTime) * fps);
        if (frame >= frames.Length)
        {
            if (!loop)
            {
                if (destroyWhenDone) Destroy(transform.root.gameObject);
                return;
            }
            frame %= frames.Length;
        }
        spriteRenderer.sprite = frames[frame];
    }
}
