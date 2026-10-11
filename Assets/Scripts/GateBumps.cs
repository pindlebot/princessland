using UnityEngine;

// "You need a tool for this": leaning on a gap, a stone block or deep water without the tool that crosses it says so,
// with the tool's own picture and a sound (ActionFeedback.MissingTool), instead of the hero just standing there.
//
// It only speaks once the player has leaned on the obstacle for a moment, and then not again until they let go and
// come back, so a hero walking along a shoreline isn't nagged. (The thought bubble over a gap, HintBubble, still
// teaches the first time; this is the answer to "I tried it and nothing happened".)
//
// HopAbility adds this to every hero, like the abilities it stands in for.
[RequireComponent(typeof(PlayerController))]
public class GateBumps : MonoBehaviour
{
    [SerializeField] private float leanSeconds = 0.3f;

    private PlayerController player;
    private LevelMap map;
    private bool searchedForMap;
    private float leaning;
    private bool spoke;

    private void Awake() => player = GetComponent<PlayerController>();

    private void Update()
    {
        var direction = player.MoveDirection;
        if (GameInput.ActionsBlocked || player.IsHopping || player.IsSeated || direction.sqrMagnitude < 0.01f ||
            (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct))
        {
            leaning = 0f;
            spoke = false;
            return;
        }

        string missing = MissingToolAhead(direction);
        if (missing == null)
        {
            leaning = 0f;
            spoke = false;
            return;
        }
        leaning += Time.deltaTime;
        if (leaning >= leanSeconds && !spoke)
        {
            spoke = true;
            ActionFeedback.MissingTool(missing);
        }
    }

    // The tool that would get the hero past what they're pushing against, if they don't have it; otherwise null.
    private string MissingToolAhead(Vector3 direction)
    {
        var dir = PushBlock.Snap(direction);
        if (!Abilities.Has(Abilities.BouncyBoots) && Gap.At(transform.position + dir * 1.15f) != null)
            return Abilities.BouncyBoots;

        if (!Abilities.Has(Abilities.MoleMitts) && BlockAhead(dir)) return Abilities.MoleMitts;

        if (!Abilities.Has(Abilities.BubbleCharm))
        {
            if (!searchedForMap) { map = FindAnyObjectByType<LevelMap>(); searchedForMap = true; }
            if (map != null && LevelMap.IsWater(map.TileAt(transform.position + dir * 1.3f))) return Abilities.BubbleCharm;
        }
        return null;
    }

    // Is a stone block right in front of us, in our lane? (The same test PushAbility makes once we have the Mitts.)
    private bool BlockAhead(Vector3 dir)
    {
        foreach (var block in PushBlock.All)
        {
            var to = block.transform.position - transform.position;
            to.y = 0f;
            float along = Vector3.Dot(to, dir);
            float across = Vector3.Cross(dir, to).magnitude;
            if (along > 0f && along <= 2.1f && across <= PushBlock.TileSize * 0.45f) return true;
        }
        return false;
    }
}
