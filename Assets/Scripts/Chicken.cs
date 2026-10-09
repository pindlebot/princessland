using UnityEngine;

// A hen (or a chick) pottering about the village's pen: she trots to a spot near where she was
// placed, pecks at the corn for a while, then picks another spot. A tiny two-state machine
// (walking / pecking) driven by timers, with no physics: she has no collider, so she never
// gets stuck, and staying within `radius` of home keeps her inside the pen.
// The sprite is drawn facing right; it's flipped to face the way she's walking on screen.
// (Petting her is a HouseFixture on the same object: "Pet the hen".)
public class Chicken : MonoBehaviour
{
    [SerializeField] private SpriteFlipbook flipbook;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] peckFrames;
    [SerializeField] private float fps = 6f;
    [SerializeField] private float radius = 1.1f;           // how far from home she wanders
    [SerializeField] private float speed = 0.9f;            // metres per second
    [SerializeField] private Vector2 peckSeconds = new Vector2(1.2f, 3.5f);

    private SpriteRenderer sprite;
    private Vector3 home, target;
    private float peckUntil;
    private bool walking;

    public bool IsWalking => walking;
    public Vector3 Home => home;
    public float Radius => radius;

    private void Start()
    {
        sprite = flipbook.GetComponent<SpriteRenderer>();
        home = transform.position;
        Peck(Random.Range(0f, peckSeconds.y)); // so the flock doesn't move in step
    }

    private void Update()
    {
        if (!walking)
        {
            if (Time.time < peckUntil) return;
            Vector2 spot = Random.insideUnitCircle * radius;
            target = home + new Vector3(spot.x, 0f, spot.y);
            walking = true;
            flipbook.Play(walkFrames, fps);
            FaceToward(target - transform.position);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude < 0.0001f)
            Peck(Random.Range(peckSeconds.x, peckSeconds.y));
    }

    private void Peck(float seconds)
    {
        walking = false;
        peckUntil = Time.time + seconds;
        flipbook.Play(peckFrames, fps);
    }

    // Flip to face left or right on screen: compare the walk direction with the camera's right.
    private void FaceToward(Vector3 direction)
    {
        var cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        sprite.flipX = Vector3.Dot(direction, right) < 0f;
    }
}
