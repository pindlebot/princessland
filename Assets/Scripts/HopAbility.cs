using UnityEngine;

// The Bouncy Boots: walk into a gap (or press the hop button beside one) and the hero springs over
// it in a short arc, landing on the far side. No physics jumping: it's a scripted hop along the
// tile grid, one or two gap tiles wide, and only straight (north, south, east or west).
//
// Like LanternLight and SpellAbility, the component sits on every hero from the start and does
// nothing until the boots are in the treasures tab (Abilities.Has).
[RequireComponent(typeof(PlayerController))]
public class HopAbility : MonoBehaviour
{
    [SerializeField] private int maxGapTiles = 2;       // how wide a gap you can clear
    [SerializeField] private float pushSeconds = 0.12f; // walk into a gap this long and you hop
    [SerializeField] private float hopSeconds = 0.5f;   // per gap tile (the hop is a little longer over two)
    [SerializeField] private float hopHeight = 1.6f;
    [SerializeField] private GameObject puffPrefab;     // dust where you land
    [SerializeField] private AudioClip hopSound;
    [SerializeField] private AudioClip landSound;

    private PlayerController player;
    private float pushing;

    public int Hops { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        if (!TryGetComponent<GateBumps>(out _)) gameObject.AddComponent<GateBumps>(); // says "need a tool" at a gate you can't pass
    }

    private void Update()
    {
        if (player.IsHopping || !Abilities.Has(Abilities.BouncyBoots)) { pushing = 0f; return; }
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) return;
        if (GameInput.ActionsBlocked || player.IsSeated) { pushing = 0f; return; }

        if (GameInput.HopPressed)
        {
            // The hero's own facing, or the way they're walking.
            TryHop(player.MoveDirection.sqrMagnitude > 0.01f ? player.MoveDirection : transform.forward);
            return;
        }
        if (player.MoveDirection.sqrMagnitude > 0.01f && GapAhead(player.MoveDirection, out _, out _))
        {
            pushing += Time.deltaTime;
            if (pushing >= pushSeconds) { pushing = 0f; TryHop(player.MoveDirection); }
        }
        else pushing = 0f;
    }

    // Hop over the gap in front of us, towards `direction` (snapped to north/south/east/west).
    // Returns false if there's no gap there, it's too wide, or there's nowhere to land.
    public bool TryHop(Vector3 direction)
    {
        if (player.IsHopping || !Abilities.Has(Abilities.BouncyBoots)) return false;
        if (!GapAhead(direction, out var dir, out var landing)) return false;

        AudioManager.Play(hopSound);
        player.FaceFor(dir, 0.6f);
        float seconds = hopSeconds * (1f + 0.3f * (Mathf.RoundToInt(Vector3.Distance(transform.position, landing) / Gap.TileSize) - 2));
        player.HopTo(landing, Mathf.Max(0.35f, seconds), hopHeight, () =>
        {
            Hops++;
            GameSession.AddToCounter("hops");
            AudioManager.Play(landSound);
            if (puffPrefab != null) Instantiate(puffPrefab, new Vector3(landing.x, 0.3f, landing.z), Quaternion.identity);
        });
        return true;
    }

    // Is there a gap (at most maxGapTiles wide) right in front of us, with solid ground on the far side?
    // dir is the direction snapped to the grid; landing is where we'd touch down.
    public bool GapAhead(Vector3 direction, out Vector3 dir, out Vector3 landing)
    {
        dir = Vector3.zero;
        landing = Vector3.zero;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;
        dir = Mathf.Abs(direction.x) > Mathf.Abs(direction.z) ? new Vector3(Mathf.Sign(direction.x), 0f, 0f)
                                                               : new Vector3(0f, 0f, Mathf.Sign(direction.z));
        var here = transform.position;
        var first = Gap.At(here + dir * 1.15f); // the wall stops us half a metre from the pit's edge
        if (first == null) return false;

        // Walk across the gap's tiles to the first tile that isn't one.
        var tile = first.Center;
        int width = 0;
        while (Gap.At(tile) != null)
        {
            tile += dir * Gap.TileSize;
            if (++width > maxGapTiles) return false;
        }

        // Keep to our own lane (so we don't slide sideways), but land in the middle of the far tile along the way.
        var lateral = Vector3.Cross(Vector3.up, dir);
        float side = Vector3.Dot(here - first.Center, lateral);
        landing = tile + lateral * Mathf.Clamp(side, -0.6f, 0.6f);
        landing.y = here.y;
        // Something solid there (a wall, a tree), or nothing underfoot?
        if (Physics.CheckSphere(landing + Vector3.up * 1f, 0.4f, ~LevelMap.WaterMask, QueryTriggerInteraction.Ignore)) return false;
        return Physics.Raycast(landing + Vector3.up * 1f, Vector3.down, 3f, ~LevelMap.WaterMask, QueryTriggerInteraction.Ignore);
    }
}
