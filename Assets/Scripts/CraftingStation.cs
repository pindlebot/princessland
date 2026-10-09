using UnityEngine;

// Somewhere you make things: the kitchen stove, for cooking. Press E and it opens the HUD's
// recipe card (CookingView) with the recipes it knows; the card does the rest. Crafting only
// ever happens at a station, so you have to go home to the kitchen to cook.
public class CraftingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private CraftKind kind = CraftKind.Cooking;
    [SerializeField] private string prompt = "Cook something";
    [SerializeField] private Recipe[] recipes;
    [SerializeField] private AudioClip openSound;

    public CraftKind Kind => kind;
    public Recipe[] Recipes => recipes;
    public Vector3 Position => transform.position;
    public string Prompt => prompt;
    public bool CanInteract => recipes != null && recipes.Length > 0;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    public string Interact(GameObject player)
    {
        if (CookingView.Instance == null) return "The stove is cold."; // no HUD (a test scene)
        AudioManager.Play(openSound);
        CookingView.Instance.Open(this, player.GetComponent<Inventory>());
        return null; // the card says everything
    }
}
