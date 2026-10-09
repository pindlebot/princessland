using System;
using UnityEngine;

// What kind of crafting a recipe is, and so which station makes it. Saves don't store this,
// but keep new kinds at the end anyway, like the other enums.
public enum CraftKind { Cooking }

// A recipe card, like the ones in the Stardew Valley guidebook: a name, a short list of
// ingredients with how many of each, and what it makes. Saved as an asset (Assets/Recipes/),
// so a station just lists the recipes it can make. Create more: Create > Dungeon > Recipe.
[CreateAssetMenu(menuName = "Dungeon/Recipe", fileName = "NewRecipe")]
public class Recipe : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public ItemDefinition item;
        [Min(1)] public int count;
    }

    [SerializeField] private string id = "new_recipe";
    [SerializeField] private CraftKind kind = CraftKind.Cooking;
    [SerializeField] private Ingredient[] ingredients;
    [Tooltip("What it makes (its name, icon and what it does come from this item).")]
    [SerializeField] private ItemDefinition result;
    [Tooltip("Said when you make it. Several answers separated by '|' take turns.")]
    [TextArea] [SerializeField] private string madeMessage;

    public string Id => id;
    public CraftKind Kind => kind;
    public Ingredient[] Ingredients => ingredients;
    public ItemDefinition Result => result;
    public string MadeMessage => madeMessage;
}

// The rules for making things: check the bag for every ingredient, take them out, put the
// result in. Plain static methods, so the stove, the HUD's recipe card and the tests all agree.
public static class Crafting
{
    public static bool HasIngredients(Inventory bag, Recipe recipe)
    {
        foreach (var ingredient in recipe.Ingredients)
            if (bag.Count(ingredient.item) < ingredient.count) return false;
        return true;
    }

    // Makes one if every ingredient is in the bag. The ingredients come out first, so the
    // result always has room. Returns the "you made it" message, or null if it couldn't.
    public static string Craft(Inventory bag, Recipe recipe)
    {
        if (!HasIngredients(bag, recipe)) return null;
        foreach (var ingredient in recipe.Ingredients)
            for (int i = 0; i < ingredient.count; i++)
                bag.Remove(ingredient.item);
        bag.Add(recipe.Result);
        int made = GameSession.AddToCounter("made:" + recipe.Id); // saved with the other counters
        if (string.IsNullOrEmpty(recipe.MadeMessage)) return $"You made {recipe.Result.DisplayName}!";
        var answers = recipe.MadeMessage.Split('|');
        return answers[(made - 1) % answers.Length].Trim();
    }
}
