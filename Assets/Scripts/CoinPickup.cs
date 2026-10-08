using UnityEngine;

// A gold coin dropped by an enemy. It pops out in a little arc, then flies to the player
// (speeding up as it goes) and is collected automatically: no button needed.
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private float magnetRange = 40f;  // effectively "anywhere on screen"
    [SerializeField] private float magnetSpeed = 14f;  // top speed
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float collectRange = 0.6f;
    [SerializeField] private AudioClip collectSound;

    public int Value { get; set; } = 1;

    private const float PopSeconds = 0.45f;
    private const float RestHeight = 0.3f;
    private Vector3 velocity;
    private float spawnTime;
    private bool collected;
    private float speed;

    private void Start()
    {
        spawnTime = Time.time;
        // Burst outward and upward in a random direction.
        var dir = Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.6f);
        velocity = new Vector3(dir.x, Random.Range(4f, 6f), dir.y);
    }

    private void Update()
    {
        if (collected) return;

        if (Time.time - spawnTime < PopSeconds)
        {
            velocity.y -= 20f * Time.deltaTime; // gravity
            var p = transform.position + velocity * Time.deltaTime;
            if (p.y < RestHeight) { p.y = RestHeight; velocity = Vector3.zero; }
            transform.position = p;
            return;
        }

        var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        if (player == null) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance < collectRange) Collect();
        else if (distance < magnetRange)
        {
            speed = Mathf.Min(magnetSpeed, speed + acceleration * Time.deltaTime);
            transform.position += toPlayer.normalized * Mathf.Min(speed * Time.deltaTime, distance);
        }
    }

    private void Collect()
    {
        collected = true;
        GameSession.Progress.AddGold(Value);
        AudioManager.Play(collectSound, 0.5f);
        Destroy(gameObject);
    }
}
