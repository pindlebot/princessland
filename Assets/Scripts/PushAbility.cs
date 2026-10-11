using UnityEngine;

// The Mole Mitts: lean on a stone block (walk into it) and the hero shoves it one tile along (PushBlock).
// Like HopAbility, the component sits on every hero from the start and does nothing until the Mitts are in the
// treasures tab (Abilities.Has). Digging soft dirt is SoftDirt's job: that one is a press of E.
[RequireComponent(typeof(PlayerController))]
public class PushAbility : MonoBehaviour
{
    [SerializeField] private float pushSeconds = 0.25f; // lean on a block this long and it moves
    [SerializeField] private float reach = 2.1f;        // how far ahead (centre to centre) a block can be

    private PlayerController player;
    private float pushing;

    private void Awake() => player = GetComponent<PlayerController>();

    private void Update()
    {
        if (!Abilities.Has(Abilities.MoleMitts) || player.IsHopping || player.IsSeated) { pushing = 0f; return; }
        if (GameManager.Instance != null && !GameManager.Instance.PlayerCanAct) { pushing = 0f; return; }
        if (GameInput.ActionsBlocked || player.MoveDirection.sqrMagnitude < 0.01f) { pushing = 0f; return; }

        var dir = PushBlock.Snap(player.MoveDirection);
        var block = BlockAhead(dir);
        if (block == null) { pushing = 0f; return; }
        pushing += Time.deltaTime;
        if (pushing >= pushSeconds)
        {
            pushing = 0f;
            player.FaceFor(dir, 0.3f);
            block.TrySlide(dir);
        }
    }

    // The block right in front of us (in our lane), if there is one.
    public PushBlock BlockAhead(Vector3 dir)
    {
        foreach (var block in PushBlock.All)
        {
            var to = block.transform.position - transform.position;
            to.y = 0f;
            float along = Vector3.Dot(to, dir);
            float across = Vector3.Cross(dir, to).magnitude;
            if (along > 0f && along <= reach && across <= PushBlock.TileSize * 0.45f) return block;
        }
        return null;
    }
}
