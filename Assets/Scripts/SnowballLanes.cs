using System.Collections;
using UnityEngine;

// The Snow Yeti's snowballs. Narrow red lanes light up across the icy den (the telegraph; longer in Gentle Mode), then a
// big snowball rolls along each one and knocks flat anyone still standing in it. On the ice you slide, so mind your footing.
[RequireComponent(typeof(BossAbilities))]
public class SnowballLanes : MonoBehaviour
{
    [SerializeField] private float cooldown = 7f;
    [SerializeField] private int lanes = 3;
    [SerializeField] private float laneWidth = 2.2f;
    [SerializeField] private float warnSeconds = 1.5f;
    [SerializeField] private float travelSeconds = 1.6f;
    [SerializeField] private float knockback = 3f;
    [SerializeField] private int damage = 2;
    [SerializeField] private Sprite[] ballFrames;
    [SerializeField] private AudioClip rollSound;

    private BossAbilities boss;
    private Health health;
    private Transform player;
    private Health playerHealth;
    private float nextAt;

    public bool IsRolling { get; private set; }
    public int BallsHit { get; private set; }

    private void Awake()
    {
        boss = GetComponent<BossAbilities>();
        health = GetComponent<Health>();
    }

    private void Start()
    {
        var hero = FindAnyObjectByType<PlayerController>();
        if (hero == null) return;
        player = hero.transform;
        playerHealth = hero.GetComponent<Health>();
        nextAt = Time.time + 6f;
    }

    private void Update()
    {
        if (player == null || health.IsDead || playerHealth.IsDead || DialogueController.IsOpen) return;
        if (!boss.IsEngaged || IsRolling || boss.IsSlamming || Time.time < nextAt) return;
        StartLanes();
    }

    // Public so tests can trigger it on demand.
    public void StartLanes()
    {
        if (IsRolling || player == null || health.IsDead) return;
        StartCoroutine(Roll());
    }

    private IEnumerator Roll()
    {
        IsRolling = true;
        var map = FindAnyObjectByType<LevelMap>();
        float w = map != null ? map.Width * map.TileSize : 60f, h = map != null ? map.Height * map.TileSize : 40f;
        bool alongX = Random.value < 0.5f;
        float length = (alongX ? w : h) + 4f, start = -2f;
        // The first lane is right where you stand; the others are spread across the room.
        var lanePositions = new float[lanes];
        lanePositions[0] = alongX ? player.position.z : player.position.x;
        for (int i = 1; i < lanes; i++) lanePositions[i] = Random.Range(2f, (alongX ? h : w) - 2f);

        // Which way they'll roll is decided now, so the warning can point that way.
        bool fromFar = Random.value < 0.5f;
        float dir = fromFar ? -1f : 1f;
        var axis = alongX ? Vector3.right : Vector3.forward;
        var across = alongX ? Vector3.forward : Vector3.right;

        // Each lane: a strip of arrowheads with rails along its edges (shape, not just colour), a charging shimmer, a tick.
        float warn = Telegraph.Clamp(warnSeconds * (GameSession.Settings.gentle ? BossAbilities.GentleWindupFactor : 1f));
        var strips = new TelegraphMarker[lanes];
        for (int i = 0; i < lanes; i++)
        {
            var at = alongX ? new Vector3((start + length) / 2f, 0f, lanePositions[i]) : new Vector3(lanePositions[i], 0f, (start + length) / 2f);
            strips[i] = Telegraph.Band(at, axis * dir, length - start, laneWidth, warn);
        }
        AudioManager.Play(ActionFeedback.Clip("warn_charge"));
        for (float t = 0f; t < warn; t += Time.deltaTime)
        {
            if (health.IsDead) { foreach (var s in strips) s.Finish(); IsRolling = false; yield break; }
            yield return null;
        }
        foreach (var s in strips) s.Finish();
        AudioManager.Play(rollSound);

        // The snowballs roll, one per lane.
        float from = fromFar ? length : start, to = fromFar ? start : length;
        var balls = new SpriteRenderer[lanes];
        for (int i = 0; i < lanes; i++)
        {
            var go = new GameObject("Snowball");
            balls[i] = go.AddComponent<SpriteRenderer>();
            balls[i].sortingOrder = 8;
            go.AddComponent<Billboard>();
        }
        var hit = false;
        for (float t = 0f; t < travelSeconds; t += Time.deltaTime)
        {
            float pos = Mathf.Lerp(from, to, t / travelSeconds);
            for (int i = 0; i < lanes; i++)
            {
                var at = alongX ? new Vector3(pos, 0.6f, lanePositions[i]) : new Vector3(lanePositions[i], 0.6f, pos);
                balls[i].transform.position = at;
                if (ballFrames != null && ballFrames.Length > 0) balls[i].sprite = ballFrames[(int)(t * 10f) % ballFrames.Length];
                if (!hit && player != null && !playerHealth.IsDead)
                {
                    var rel = player.position - at;
                    rel.y = 0f;
                    if (Mathf.Abs(Vector3.Dot(rel, axis)) < 1.1f && Mathf.Abs(Vector3.Dot(rel, across)) < laneWidth / 2f + 0.2f)
                    {
                        hit = true;
                        BallsHit++;
                        playerHealth.TakeDamage(damage);
                        StartCoroutine(Shove(axis * dir * knockback));
                    }
                }
            }
            yield return null;
        }
        foreach (var b in balls) Destroy(b.gameObject);
        nextAt = Time.time + cooldown;
        IsRolling = false;
    }

    private IEnumerator Shove(Vector3 push)
    {
        var body = player != null ? player.GetComponent<CharacterController>() : null;
        for (float t = 0f; t < 0.3f && body != null && body.enabled; t += Time.deltaTime)
        {
            body.Move(push * (Time.deltaTime / 0.3f));
            yield return null;
        }
    }
}
