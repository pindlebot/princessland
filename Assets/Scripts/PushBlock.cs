using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A heavy stone block. With the Mole Mitts (PushAbility), walk into it and it slides one tile, with a clunk.
// It only moves if the tile beyond is free: nothing solid there (a wall, another block, a gap's invisible wall,
// a monster) and floor underneath. A block never remembers where it was left: leave the room and come back
// and it's home again, so a block pushed into a corner never traps anyone for good.
public class PushBlock : MonoBehaviour
{
    public const float TileSize = 2f; // mirrors DungeonBuilder.Tile

    [SerializeField] private float slideSeconds = 0.35f;
    [SerializeField] private AudioClip pushSound;

    private static readonly List<PushBlock> all = new List<PushBlock>();
    public static IReadOnlyList<PushBlock> All => all;

    private Collider body;

    public bool IsMoving { get; private set; }
    public int Pushes { get; private set; }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);
    private void Awake() => body = GetComponentInChildren<Collider>();

    // The north/south/east/west unit vector closest to a direction.
    public static Vector3 Snap(Vector3 direction)
    {
        direction.y = 0f;
        return Mathf.Abs(direction.x) > Mathf.Abs(direction.z) ? new Vector3(Mathf.Sign(direction.x), 0f, 0f)
                                                               : new Vector3(0f, 0f, Mathf.Sign(direction.z));
    }

    // Can it slide one tile towards `direction` right now?
    public bool CanSlide(Vector3 direction)
    {
        if (IsMoving) return false;
        var target = transform.position + Snap(direction) * TileSize;
        var half = new Vector3(TileSize * 0.42f, 0.8f, TileSize * 0.42f);
        foreach (var hit in Physics.OverlapBox(target + Vector3.up * 1f, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (hit != body && !hit.transform.IsChildOf(transform)) return false;
        return Physics.Raycast(target + Vector3.up * 1f, Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Ignore); // floor underfoot
    }

    // Slides one tile if it can. Returns whether it started.
    public bool TrySlide(Vector3 direction)
    {
        if (!CanSlide(direction)) return false;
        StartCoroutine(Slide(transform.position + Snap(direction) * TileSize));
        return true;
    }

    private IEnumerator Slide(Vector3 target)
    {
        IsMoving = true;
        Pushes++;
        GameSession.AddToCounter("blocks_pushed");
        AudioManager.Play(pushSound);
        Vector3 from = transform.position;
        for (float t = 0f; t < slideSeconds; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, target, t / slideSeconds);
            yield return null;
        }
        transform.position = target;
        NavGrid.MarkDirty();
        IsMoving = false;
    }
}
