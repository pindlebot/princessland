using System.Collections.Generic;
using UnityEngine;

// One tile of a gap: a pit in the floor, too wide to step over. It has an invisible wall (on the
// Water layer, so spells fly over it) that keeps everyone out; hopping with the Bouncy Boots
// (HopAbility) is the only way across. Gaps are placed in the Castle Grounds and Dungeon before
// you have the boots, so you see where you'll come back to.
public class Gap : MonoBehaviour
{
    public const float TileSize = 2f; // mirrors DungeonBuilder.Tile

    private static readonly List<Gap> all = new List<Gap>();
    public static IReadOnlyList<Gap> All => all;

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    public Vector3 Center => transform.position;

    // The gap tile that contains this point (on the floor plane), if any.
    public static Gap At(Vector3 world)
    {
        foreach (var gap in all)
        {
            var c = gap.Center;
            if (Mathf.Abs(world.x - c.x) <= TileSize / 2f && Mathf.Abs(world.z - c.z) <= TileSize / 2f) return gap;
        }
        return null;
    }
}
