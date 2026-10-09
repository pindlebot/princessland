using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// Play Mode tests for cooking: gathering ingredients (the kitchen's pantry and fruit bowl, the
// hens' coop in Hollyhock), the stove's recipe card, and eating what you made.
public class CookingTests
{
    [SetUp]
    [TearDown]
    public void Reset()
    {
        GameSession.NewGame(null);
        Time.timeScale = 1f;
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        for (float t = 0f; t < 3f && SceneManager.GetActiveScene().name != scene; t += Time.unscaledDeltaTime)
            yield return null;
        yield return null;
        yield return null;
        foreach (var e in Object.FindObjectsByType<EnemyAI>()) e.enabled = false;
        LevelBootstrap.Current.Player.GetComponent<PlayerController>().enabled = false;
    }

    private static ItemDefinition Item(string id) =>
        Resources.FindObjectsOfTypeAll<ItemDefinition>().First(i => i.Id == id);

    private static HouseFixture Gatherer(string name) =>
        Object.FindObjectsByType<HouseFixture>().Single(f => f.name == name);

    private static VisualElement Hud() =>
        Object.FindAnyObjectByType<HudController>().GetComponent<UIDocument>().rootVisualElement;

    [UnityTest]
    public IEnumerator ThePantryAndFruitBowlGiveOneIngredientAtATime()
    {
        yield return Load("Kitchen");
        var player = LevelBootstrap.Current.Player;
        var bag = player.GetComponent<Inventory>();

        Assert.AreEqual(HouseFixture.Effect.Gather, Gatherer("Pantry").Kind);
        StringAssert.Contains("flour", Gatherer("Pantry").Interact(player));
        Assert.AreEqual(1, bag.Count(Item("flour")));
        StringAssert.Contains("already", Gatherer("Pantry").Interact(player), "one at a time");
        Assert.AreEqual(1, bag.Count(Item("flour")));

        StringAssert.Contains("strawberry", Gatherer("Island").Interact(player));
        Assert.AreEqual(1, bag.Count(Item("strawberry")));
    }

    [UnityTest]
    public IEnumerator TheHensCoopGivesAnEgg()
    {
        yield return Load("Level0");
        var player = LevelBootstrap.Current.Player;
        var coop = Gatherer("Coop");
        Assert.AreEqual("Look in the nest box", coop.Prompt);
        StringAssert.Contains("egg", coop.Interact(player));
        Assert.AreEqual(1, player.GetComponent<Inventory>().Count(Item("egg")));
        Assert.AreEqual("cluck", AudioManager.Instance.LastPlayed.name);
    }

    [UnityTest]
    public IEnumerator TheStoveCooksPancakesFromTheRecipe()
    {
        yield return Load("Kitchen");
        var player = LevelBootstrap.Current.Player;
        var bag = player.GetComponent<Inventory>();
        var stove = Object.FindAnyObjectByType<CraftingStation>();
        var recipe = stove.Recipes.Single();
        Assert.AreEqual("strawberry_pancakes", recipe.Id);
        CollectionAssert.AreEquivalent(new[] { "egg", "flour", "strawberry" }, recipe.Ingredients.Select(i => i.item.Id));

        // Only some of the ingredients: the card opens, pauses the game, and says what's missing.
        bag.Add(Item("egg"));
        bag.Add(Item("flour"));
        Assert.IsNull(stove.Interact(player));
        var view = CookingView.Instance;
        Assert.IsTrue(view.IsOpen);
        Assert.AreEqual(0f, Time.timeScale, "the game waits while you read the recipe");
        Assert.IsTrue(GameInput.GameplayBlocked);
        var card = Hud().Q("recipe-card");
        Assert.AreEqual("Strawberry Pancakes", card.Q<Label>(className: "recipe-name").text);
        Assert.AreEqual(2, card.Query(className: "recipe-ingredient").Where(e => e.ClassListContains("have")).ToList().Count);
        Assert.IsFalse(card.Q(className: "cook-button").ClassListContains("ready"));

        Assert.IsFalse(view.Cook());
        StringAssert.Contains("1 Strawberry", view.Message);
        Assert.AreEqual(1, bag.Count(Item("egg")), "nothing used up");

        // All three: cook!
        bag.Add(Item("strawberry"));
        Assert.IsTrue(card.Q(className: "cook-button").ClassListContains("ready"), "the card updates as the bag changes");
        Assert.IsTrue(view.Cook());
        StringAssert.Contains("Pancakes", view.Message);
        Assert.AreEqual(1, bag.Count(Item("pancakes")));
        Assert.IsTrue(card.Q(className: "cook-button").ClassListContains("made"), "a happy button, not \"find the ingredients\"");
        Assert.AreEqual(0, bag.Count(Item("egg")) + bag.Count(Item("flour")) + bag.Count(Item("strawberry")));
        Assert.AreEqual(1, GameSession.GetCounter("made:strawberry_pancakes"));
        Assert.AreEqual("cook", AudioManager.Instance.LastPlayed.name);

        view.Close();
        Assert.IsFalse(view.IsOpen);
        Assert.AreEqual(1f, Time.timeScale);
        yield return null;
        Assert.IsFalse(GameInput.GameplayBlocked);
    }

    [UnityTest]
    public IEnumerator EatingPancakesFromTheBagRestoresHeartsAndMagic()
    {
        yield return Load("Kitchen");
        var player = LevelBootstrap.Current.Player;
        var bag = player.GetComponent<Inventory>();
        var health = player.GetComponent<Health>();
        var mana = player.GetComponent<Mana>();
        var pancakes = Item("pancakes");
        Assert.IsTrue(pancakes.IsFood);
        Assert.AreEqual("+3 hearts, +50 magic", pancakes.FoodText);

        bag.Add(pancakes);
        health.TakeDamage(4);
        mana.TrySpend(mana.Max);
        int hurt = health.Current;
        Assert.IsTrue(bag.Eat(pancakes));
        Assert.AreEqual(Mathf.Min(health.Max, hurt + 3), health.Current);
        Assert.Greater(health.Current, hurt);
        Assert.Greater(mana.Current, 40f);
        Assert.AreEqual(0, bag.Count(pancakes), "eaten up");
        Assert.IsFalse(bag.Eat(Item("egg")), "you can't eat what you haven't got (or a raw egg)");
    }
}
