using UnityEditor;
using UnityEngine;

// The bathroom's big, luxurious bath (legend prop "bathtub"): a white-marble tub three tiles
// wide on a stepped plinth, open all round, with a gold swan-neck tap you can turn on and off (a
// HouseFixture with the Faucet effect, at the spout end of the tub: stand there and press E).
// Getting in works like before: E puts the hero in their swimwear, and a bottle of Barnaby's
// bubble bath heaps foam on the water. You climb out dripping: the open towel shelf (legend prop
// "towels") dries you off.
// Beside it, two big potted plants (legend props "pottedfern" and "pottedmonstera").
// Art: Tools/make_bath_sprites.py (Bath.png). The overlays (stream, bubbles) are drawn in the
// tub's own frame, so they line up when they sit where the tub does.
public static partial class DungeonBuilder
{
    // Where the spout is, from the tub's middle (Bath.png: x 68 of 96, 16 px per unit). The
    // faucet's E spot sits there, so stand at that end of the tub to use it.
    private static readonly Vector3 FaucetOffset = new Vector3((68f - 47.5f) / 16f, 0f, 0f);

    private static void CreateBathPrefabs(SharedAssets assets, SpriteSheetImporter.SpriteSheet furniture, Sprite shadow)
    {
        var bath = SpriteSheetImporter.Import("Bath");
        assets.PropPrefabs["bathtub"] = CreateFixture(bath, "Bathtub", new Vector3(5.2f, 1.2f, 1.6f), shadow, 5.6f,
            "Take a bath", "You change into your swimsuit and sink into the big marble bath. Ahh, so warm!",
            HouseFixture.Effect.Bathe, "splash",
            tub =>
            {
                SetString(tub, "standPrompt", "Get out of the bath");
                SetString(tub, "standMessage", "You climb out, dripping wet! Grab a towel from the shelf.");
                SetRef(tub, "standSound", Sound("splash"));
                SetFloat(tub, "seatHeight", 1.75f);   // the bubbles in the hero's Bathe frame sit at the water line
                SetFloat(tub, "seatForward", 0.85f);  // in front of the tub's back rim, behind nothing
                SetRef(tub, "bubbleItem", AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/BubbleBath.asset"));
                SetRef(tub, "bubbles", AddBubbleHeap(tub.gameObject, bath));
                SetString(tub, "bubbleMessage",
                    "You change into your swimsuit and pour in Barnaby's bubble bath. Glug glug... BUBBLES, all over the water!");
                SetRef(tub, "bubbleSound", Sound("bubbles"));
                AddFaucet(tub.gameObject, bath);
            });

        // The open towel shelf: dries you off after a bath (the Towel effect).
        assets.PropPrefabs["towels"] = CreateFixture(furniture, "TowelShelf", new Vector3(2f, 2.2f, 0.9f), shadow, 2.2f,
            "Grab a towel",
            "Stacks of fluffy towels, folded just so. | You bury your face in a towel. It smells like sunshine!",
            HouseFixture.Effect.Towel, "paper",
            shelf => SetString(shelf, "dryMessage", "You wrap up in a big fluffy towel and dry off. Toasty and squeaky clean!"));

        assets.PropPrefabs["pottedfern"] = CreateFixture(bath, "PottedFern", new Vector3(1.2f, 1.6f, 1.2f), shadow, 1.4f,
            "Water the fern",
            "You sprinkle the fern. It loves the warm, damp air in here! | The fern's fronds tickle your nose. Achoo!",
            HouseFixture.Effect.None, "water_run");
        assets.PropPrefabs["pottedmonstera"] = CreateFixture(bath, "PottedMonstera", new Vector3(1.2f, 2.6f, 1.2f), shadow, 1.4f,
            "Water the monstera",
            "You water the monstera. Its leaves have holes in them, like Swiss cheese! | " +
            "You polish a big shiny leaf. It looks very happy.",
            HouseFixture.Effect.None, "water_run");
    }

    // The faucet: an E spot at the spout end of the tub that shows or hides the running water.
    // The water's sound is an AmbientLoop on the stream, so it plays exactly while it's shown.
    private static void AddFaucet(GameObject tub, SpriteSheetImporter.SpriteSheet sheet)
    {
        var stream = new GameObject("Stream");
        stream.transform.SetParent(tub.transform, false);
        stream.transform.localPosition = new Vector3(0f, 0f, -0.05f); // just in front of the tub
        AddLoopingSprite(stream, sheet, "Stream");
        var loop = stream.AddComponent<AmbientLoop>();
        SetRef(loop, "clip", Sound("faucet"));
        SetFloat(loop, "volume", 0.45f);
        stream.SetActive(false);

        var tap = new GameObject("Faucet");
        tap.transform.SetParent(tub.transform, false);
        tap.transform.localPosition = FaucetOffset;
        var fixture = tap.AddComponent<HouseFixture>();
        SetString(fixture, "prompt", "Turn off the water");
        SetString(fixture, "offPrompt", "Turn on the water");
        SetString(fixture, "message",
            "You turn the gold tap. Whoosh! Warm water pours into the tub. | " +
            "Squeak, whoosh! The water splashes and swirls. | " +
            "You turn on the tap. Splish, splash, splosh!");
        SetString(fixture, "offMessage", "Squeak! You turn off the tap. Drip... drip... drop.");
        SetInt(fixture, "effect", (int)HouseFixture.Effect.Faucet);
        SetRef(fixture, "sound", Sound("tap_turn"));
        SetRef(fixture, "stream", stream);
    }

    // The bubble bath's foam: heaped along the water (behind the bather, who lies further
    // toward the camera) and a few bubbles drifting up out of it.
    // Hidden until someone pours in a bottle (HouseFixture's Bath settings).
    private static GameObject AddBubbleHeap(GameObject tub, SpriteSheetImporter.SpriteSheet sheet)
    {
        var heap = new GameObject("Bubbles");
        heap.transform.SetParent(tub.transform, false);
        heap.transform.localPosition = new Vector3(0f, 0f, -0.3f);
        AddLoopingSprite(heap, sheet, "BubbleFoam");
        foreach (var x in new[] { -2.1f, -1.1f, -0.2f, 0.8f, 1.8f })
        {
            var bubble = new GameObject("Bubble");
            bubble.transform.SetParent(heap.transform, false);
            bubble.transform.localPosition = new Vector3(x, 1.2f, 0f);
            bubble.AddComponent<SpriteRenderer>().sprite = sheet.Frames("Bubble")[0];
            bubble.AddComponent<Billboard>();
            Drift(bubble, radius: 0.3f, speed: 0.5f, height: 0.6f, rise: 2.2f);
        }
        heap.SetActive(false);
        return heap;
    }

    private static void Drift(GameObject go, float radius, float speed, float height, float rise)
    {
        var drift = go.AddComponent<AmbientWander>();
        SetFloat(drift, "radius", radius);
        SetFloat(drift, "speed", speed);
        SetFloat(drift, "height", height);
        SetFloat(drift, "bob", 0.05f);
        SetFloat(drift, "rise", rise);
    }
}
