using UnityEngine;

// One end of a rainbow bridge: a tall post wound with colour. With the Rainbow Chalk in the treasures tab, press E and it
// draws the bridge across the chasm to its partner post (RainbowBridge). Without the chalk it just looks at you: a
// "come back later" bubble (HintBubble) says you need something to draw with.
public class RainbowPost : MonoBehaviour, IInteractable
{
    [SerializeField] private RainbowBridge bridge;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite plainSprite;
    [SerializeField] private Sprite litSprite;

    public RainbowBridge Bridge => bridge;
    public Vector3 Position => transform.position;
    public bool CanInteract => bridge != null && !DialogueController.IsOpen;
    public string Prompt => bridge != null && bridge.IsDrawn ? "Look at the rainbow bridge"
                          : Abilities.Has(Abilities.RainbowChalk) ? "Draw a rainbow bridge" : "Look at the post";

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start() => Refresh();

    public void Refresh()
    {
        if (sprite != null && bridge != null) sprite.sprite = bridge.IsDrawn ? litSprite : plainSprite;
    }

    public string Interact(GameObject player)
    {
        if (bridge == null) return null;
        if (bridge.IsDrawn) return "A beautiful rainbow bridge. It hums softly.";
        if (!Abilities.Has(Abilities.RainbowChalk))
        {
            ActionFeedback.MissingTool(Abilities.RainbowChalk);
            return "A tall post wound with bands of colour. There's another one across the chasm. If only you had something to draw with...";
        }
        bridge.Draw();
        Refresh();
        return "You draw a rainbow bridge across the chasm!";
    }
}
