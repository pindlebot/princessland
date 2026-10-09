using UnityEditor;
using UnityEngine;

// Cooking, the first (and so far only) kind of crafting, after Stardew Valley's kitchen: you
// collect ingredients, then make a dish from a recipe card at the stove in the castle kitchen.
//
//   Strawberry Pancakes = 1 Egg (the hens' coop in Hollyhock) + 1 Flour (the kitchen pantry)
//                       + 1 Strawberry (the fruit bowl on the kitchen island)
//   Eat them from the bag (click): +3 hearts, +50 magic. Take a stack into the dungeon!
//
// The items are in DungeonBuilder.Items.cs (with what eating them does), the places that hand
// them out are HouseFixtures with the Gather effect (the pantry and island in
// DungeonBuilder.Home.cs, the coop in DungeonBuilder.Town.cs). To add a dish: draw it and its
// ingredients (Tools/make_item_sprites.py), add their ItemSpecs, add a RecipeSpec below, and
// Rebuild All Scenes. The stove learns every recipe here; the card flips between them.
public static partial class DungeonBuilder
{
    private class RecipeSpec
    {
        public string Id, Asset, Result, Made;
        public (string asset, int count)[] Ingredients;
    }

    private static readonly RecipeSpec[] RecipeSpecs =
    {
        new RecipeSpec
        {
            Id = "strawberry_pancakes", Asset = "StrawberryPancakes", Result = "Pancakes",
            Ingredients = new[] { ("Egg", 1), ("Flour", 1), ("Strawberry", 1) },
            Made = "Crack, stir, pour, sizzle... FLIP! A stack of Strawberry Pancakes! | " +
                   "You flip a pancake so high it nearly touches the ceiling! Strawberry Pancakes! | " +
                   "Mmm, the whole kitchen smells of pancakes. You've made another stack!",
        },
    };

    private static ItemDefinition ItemAsset(string asset)
    {
        var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/Items/{asset}.asset");
        if (item == null) throw new System.Exception($"[DungeonBuilder] No item Assets/Items/{asset}.asset (is it in ItemSpecs?)");
        return item;
    }

    // Recipes are ScriptableObject assets in Assets/Recipes/, like items.
    private static Recipe[] CreateRecipes()
    {
        var recipes = new Recipe[RecipeSpecs.Length];
        for (int r = 0; r < RecipeSpecs.Length; r++)
        {
            var spec = RecipeSpecs[r];
            var recipe = LoadOrCreateAsset<Recipe>($"Assets/Recipes/{spec.Asset}.asset");
            var so = new SerializedObject(recipe);
            so.FindProperty("id").stringValue = spec.Id;
            so.FindProperty("kind").enumValueIndex = (int)CraftKind.Cooking;
            so.FindProperty("result").objectReferenceValue = ItemAsset(spec.Result);
            so.FindProperty("madeMessage").stringValue = spec.Made;
            var list = so.FindProperty("ingredients");
            list.arraySize = spec.Ingredients.Length;
            for (int i = 0; i < spec.Ingredients.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = ItemAsset(spec.Ingredients[i].asset);
                element.FindPropertyRelative("count").intValue = spec.Ingredients[i].count;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            recipes[r] = recipe;
        }
        return recipes;
    }

    // The kitchen stove: press E to open its recipe card (CraftingStation + the HUD's CookingView).
    private static GameObject CreateStove(SpriteSheetImporter.SpriteSheet sheet, Sprite shadow, Recipe[] recipes)
    {
        var go = CreateFurniture(sheet, "Stove", new Vector3(2.4f, 1.6f, 1.2f), shadow, 2.6f);
        var station = go.AddComponent<CraftingStation>();
        SetString(station, "prompt", "Cook at the stove");
        SetRefs(station, "recipes", recipes);
        SetRef(station, "openSound", Sound("paper"));
        return SavePrefab(go, "Stove");
    }

    // Sets up a Gather fixture: what it hands out, and what it says if you're already carrying one.
    private static void Gather(HouseFixture fixture, string itemAsset, string haveOne)
    {
        SetRef(fixture, "gift", ItemAsset(itemAsset));
        SetString(fixture, "haveOneMessage", haveOne);
        SetString(fixture, "bagFullMessage", "Your bag is full! Eat something, or make a little room.");
    }
}
