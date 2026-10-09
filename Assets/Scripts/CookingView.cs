using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The recipe card the stove opens (CraftingStation), drawn like the cards in the Stardew Valley
// guidebook: the dish and what it does, then its ingredients, each with how many you have
// and a tick once you have enough. Like the skill tree, the card is built here from code
// (Hud.uxml only has the empty panel), so a recipe asset is all a new dish needs.
//
// The game pauses while it's open. E / A (or Enter, or clicking the button) cooks; Esc / B
// closes it; left and right flip between recipes when a station knows more than one.
[RequireComponent(typeof(UIDocument))]
public class CookingView : MonoBehaviour
{
    [SerializeField] private AudioClip cookSound;
    [SerializeField] private AudioClip missingSound;
    [SerializeField] private AudioClip closeSound;

    public static CookingView Instance { get; private set; }

    // Like DialogueController.BlocksInput: the frame it closes still counts, so the Esc that
    // closed it doesn't also open the pause menu.
    public static bool BlocksInput => Instance != null && (Instance.IsOpen || Time.frameCount == Instance.closedFrame);

    public bool IsOpen => station != null;
    public Recipe Current => station != null ? station.Recipes[index] : null;
    public string Message => message.text;

    private VisualElement panel, card;
    private Label message, title;
    private CraftingStation station;
    private Inventory bag;
    private int index, openedFrame = -1, closedFrame = -1;
    private bool justMade; // the button cheers instead of "find the ingredients" right after cooking

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsOpen) Time.timeScale = 1f;
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        panel = root.Q("cooking");
        card = root.Q("recipe-card");
        message = root.Q<Label>("cooking-message");
        title = root.Q<Label>("cooking-title");
        root.Q("cooking-close").RegisterCallback<ClickEvent>(_ => Close());
    }

    public void Open(CraftingStation at, Inventory inventory)
    {
        if (IsOpen || at == null || inventory == null) return;
        station = at;
        bag = inventory;
        index = 0;
        justMade = false;
        openedFrame = Time.frameCount;
        title.text = at.Kind == CraftKind.Cooking ? "Cooking" : at.Kind.ToString();
        message.text = "";
        bag.Changed += Refresh;
        panel.EnableInClassList("open", true);
        Time.timeScale = 0f; // pause the world, like a conversation
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        bag.Changed -= Refresh;
        station = null;
        bag = null;
        closedFrame = Time.frameCount;
        panel.EnableInClassList("open", false);
        Time.timeScale = 1f;
        AudioManager.Play(closeSound, 0.6f);
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return; // the E that opened it isn't a "cook"
        if (GameInput.BackPressed || GameInput.MenuPressed) { Close(); return; }
        if (GameInput.InteractPressed || GameInput.ConfirmPressed) Cook();
        int count = station.Recipes.Length;
        if (count > 1 && (GameInput.LeftPressed || GameInput.RightPressed))
        {
            index = (index + (GameInput.RightPressed ? 1 : count - 1)) % count;
            message.text = "";
            justMade = false;
            Refresh();
        }
    }

    // Cooks the recipe on the card if everything's in the bag; otherwise says what's missing.
    public bool Cook()
    {
        if (!IsOpen) return false;
        var recipe = Current;
        string made = Crafting.Craft(bag, recipe);
        if (made == null)
        {
            message.text = $"You still need {Missing(recipe)}.";
            AudioManager.Play(missingSound, 0.6f);
            return false;
        }
        justMade = true;
        message.text = made + " It's in your bag: open the bag and click it to eat it.";
        AudioManager.Play(cookSound);
        Refresh();
        var dish = card.Q(className: "recipe-dish-icon");
        dish?.AddToClassList("pop");
        dish?.schedule.Execute(() => dish.RemoveFromClassList("pop")).StartingIn(180);
        return true;
    }

    private string Missing(Recipe recipe)
    {
        var parts = new List<string>();
        foreach (var ingredient in recipe.Ingredients)
        {
            int missing = ingredient.count - bag.Count(ingredient.item);
            if (missing > 0) parts.Add($"{missing} {ingredient.item.DisplayName}");
        }
        return parts.Count switch
        {
            0 => "nothing",
            1 => parts[0],
            _ => string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " and " + parts[^1],
        };
    }

    // Rebuilds the card: cheap (a handful of elements), and only on opening, cooking or
    // flipping to another recipe.
    private void Refresh()
    {
        if (!IsOpen) return;
        var recipe = Current;
        var dish = recipe.Result;
        card.Clear();

        var top = Row("recipe-top");
        top.Add(Icon(dish, "recipe-dish-icon"));
        var naming = new VisualElement { pickingMode = PickingMode.Ignore };
        naming.Add(Text(dish.DisplayName, "recipe-name"));
        naming.Add(Text(dish.FoodText, "recipe-effect"));
        top.Add(naming);
        card.Add(top);
        card.Add(Text(dish.Description, "recipe-description"));

        card.Add(Text("Ingredients", "section-label"));
        foreach (var ingredient in recipe.Ingredients)
        {
            int have = bag.Count(ingredient.item);
            bool enough = have >= ingredient.count;
            var row = Row("recipe-ingredient");
            row.EnableInClassList("have", enough);
            row.Add(Icon(ingredient.item, "recipe-ingredient-icon"));
            var words = new VisualElement { pickingMode = PickingMode.Ignore };
            words.style.flexGrow = 1;
            words.Add(Text(ingredient.item.DisplayName, "recipe-ingredient-name"));
            if (!enough) words.Add(Text(ingredient.item.Description, "recipe-ingredient-where"));
            row.Add(words);
            row.Add(Text($"{Mathf.Min(have, ingredient.count)}/{ingredient.count}", "recipe-count"));
            row.Add(Row("recipe-tick")); // a gold star once you have enough (Hud.uss)
            card.Add(row);
        }

        bool ready = Crafting.HasIngredients(bag, recipe);
        string label = ready ? $"{GameInput.InteractKey}: Cook!"
                     : justMade ? $"Yum! You made {dish.DisplayName}!"
                     : "Find the ingredients first";
        var button = Text(label, "cook-button");
        button.pickingMode = PickingMode.Position;
        button.EnableInClassList("ready", ready);
        button.EnableInClassList("made", !ready && justMade);
        button.RegisterCallback<ClickEvent>(_ => Cook());
        card.Add(button);
        if (station.Recipes.Length > 1)
            card.Add(Text($"<  Recipe {index + 1} of {station.Recipes.Length}  >", "recipe-pages"));
    }

    private static VisualElement Row(string cls)
    {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.AddToClassList(cls);
        return row;
    }

    private static Label Text(string text, string cls)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.AddToClassList(cls);
        return label;
    }

    private static VisualElement Icon(ItemDefinition item, string cls)
    {
        var icon = new VisualElement { pickingMode = PickingMode.Ignore };
        icon.AddToClassList(cls);
        if (item != null && item.Icon != null) icon.style.backgroundImage = new StyleBackground(item.Icon);
        return icon;
    }
}
