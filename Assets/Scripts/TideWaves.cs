using System.Collections;
using UnityEngine;

// King Crabbington's tide: while he hides in his shell, a great wave sweeps across the court. A wide red band
// shows on the floor first (the telegraph: arrowheads pointing the way it rolls; longer in Gentle Mode), then the wave rolls along it, and anyone still in
// the band is knocked back and splashed like a slam. Step out of the band before it comes.
[RequireComponent(typeof(BossAbilities))]
public class TideWaves : MonoBehaviour
{
    [SerializeField] private float cooldown = 6f;
    [SerializeField] private float warnSeconds = 1.6f;
    [SerializeField] private float travelSeconds = 1.2f;
    [SerializeField] private float bandWidth = 4f;
    [SerializeField] private float knockback = 4f;
    [SerializeField] private int damage = 2;   // like the slam: a full hit (Gentle Mode halves it to a heart)
    [SerializeField] private AudioClip waveSound;

    private BossAbilities boss;
    private ShellCycle shell;
    private Health health;
    private Transform player;
    private Health playerHealth;
    private float nextAt;
    private static Sprite square;

    public bool IsWaving { get; private set; }
    public int WavesHit { get; private set; }

    private void Awake()
    {
        boss = GetComponent<BossAbilities>();
        shell = GetComponent<ShellCycle>();
        health = GetComponent<Health>();
    }

    private void Start()
    {
        var hero = FindAnyObjectByType<PlayerController>();
        if (hero == null) return;
        player = hero.transform;
        playerHealth = hero.GetComponent<Health>();
        nextAt = Time.time + 5f;
    }

    private void Update()
    {
        if (player == null || health.IsDead || playerHealth.IsDead || DialogueController.IsOpen) return;
        if (!boss.IsEngaged || IsWaving || Time.time < nextAt) return;
        if (shell != null && shell.IsPeeking) return; // the tide is for while he's in his shell
        StartWave();
    }

    // Public so tests can trigger it on demand.
    public void StartWave()
    {
        if (IsWaving || player == null || health.IsDead) return;
        StartCoroutine(Wave());
    }

    private static Sprite Square()
    {
        if (square == null)
        {
            var tex = Texture2D.whiteTexture;
            square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width); // exactly 1 unit across
        }
        return square;
    }

    private static SpriteRenderer Flat(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var sr = SpriteMaterial.Apply(go.AddComponent<SpriteRenderer>());
        sr.sprite = Square();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    private IEnumerator Wave()
    {
        IsWaving = true;
        var map = FindAnyObjectByType<LevelMap>();
        float length = (map != null ? Mathf.Max(map.Width, map.Height) * map.TileSize : 60f) + 4f;
        bool alongX = Random.value < 0.5f;
        float lane = alongX ? player.position.z : player.position.x;      // the band follows where you stood
        var axis = alongX ? Vector3.right : Vector3.forward;
        var across = alongX ? Vector3.forward : Vector3.right;
        float start = -2f;
        bool fromFar = Random.value < 0.5f;
        float sign = fromFar ? -1f : 1f;
        float from = fromFar ? length : start, to = fromFar ? start : length;

        // The warning band across the whole court, centred on the lane: arrowheads point the way the wave will roll, with
        // rails along both edges (the shape says it; colour doesn't have to), a rising horn, and a tick as it locks in.
        float centre = (start + length) / 2f;
        var bandCentre = alongX ? new Vector3(centre, 0f, lane) : new Vector3(lane, 0f, centre);
        float warn = Telegraph.Clamp(warnSeconds * (GameSession.Settings.gentle ? BossAbilities.GentleWindupFactor : 1f));
        var band = Telegraph.Band(bandCentre, axis * sign, length - start, bandWidth, warn);
        AudioManager.Play(ActionFeedback.Clip("tide_warn"));
        for (float t = 0f; t < warn; t += Time.deltaTime)
        {
            if (health.IsDead) { band.Finish(); IsWaving = false; yield break; }
            yield return null;
        }
        band.Finish();
        AudioManager.Play(waveSound);

        // The wave itself: a pale-blue band rolling along it.
        var wave = Flat("TideWave", new Color(0.55f, 0.85f, 1f, 0.75f), 5);
        wave.transform.localScale = alongX ? new Vector3(1.6f, bandWidth, 1f) : new Vector3(bandWidth, 1.6f, 1f);
        bool hit = false;
        var shake = FindAnyObjectByType<IsoCameraFollow>();
        if (shake != null) shake.Shake(0.15f, 0.8f);
        for (float t = 0f; t < travelSeconds; t += Time.deltaTime)
        {
            float pos = Mathf.Lerp(from, to, t / travelSeconds);
            wave.transform.position = alongX ? new Vector3(pos, 0.2f, lane) : new Vector3(lane, 0.2f, pos);
            if (!hit && player != null && !playerHealth.IsDead)
            {
                var rel = player.position - wave.transform.position;
                float along = Vector3.Dot(rel, axis), side = Mathf.Abs(Vector3.Dot(rel, across));
                if (Mathf.Abs(along) < 1.2f && side < bandWidth / 2f)
                {
                    hit = true;
                    WavesHit++;
                    playerHealth.TakeDamage(damage);
                    StartCoroutine(Shove(axis * sign * knockback));
                }
            }
            yield return null;
        }
        Destroy(wave.gameObject);
        nextAt = Time.time + cooldown;
        IsWaving = false;
    }

    // The wave carries the hero along with it for a few steps (walls still stop them: CharacterController.Move).
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
