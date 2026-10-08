using UnityEditor;
using UnityEngine;

// Props and items: chest, wall torch, grass tufts, the Ember Ring pickup, the castle flag.
public static partial class DungeonBuilder
{
    // Gold stars rising with a warm flash: used when a chest opens and when you level up.
    private static GameObject CreateSparklePrefab(SpriteSheetImporter.SpriteSheet props)
    {
        var sparkle = new GameObject("ChestSparkle");
        sparkle.AddComponent<SpriteRenderer>();
        SetFlipbook(sparkle.AddComponent<SpriteFlipbook>(), props, "Sparkle", destroyWhenDone: true);
        sparkle.AddComponent<Billboard>();
        var glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(sparkle.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1f, 0f);
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.85f, 0.4f);
        glow.range = 5f;
        glow.intensity = 3f;
        SetFloat(glow.gameObject.AddComponent<FadeOutLight>(), "duration", 0.6f);
        return SavePrefab(sparkle, "ChestSparkle");
    }

    // Chest: a solid box collider (blocks walking and spells), a camera-facing sprite,
    // and the Chest script that opens it. Opening spawns the sparkle effect.
    private static GameObject CreateChestPrefab(SpriteSheetImporter.SpriteSheet props, Sprite shadowSprite, GameObject sparklePrefab)
    {
        var go = new GameObject("Chest");
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.5f, 0f);
        box.size = new Vector3(1.3f, 1f, 1f);

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        var spriteRenderer = sprite.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = props.Frames("Chest_Closed")[0];
        sprite.AddComponent<Billboard>();

        AddShadow(go, shadowSprite, 1.6f);

        var chest = go.AddComponent<Chest>();
        SetRef(chest, "spriteRenderer", spriteRenderer);
        SetRef(chest, "openEffectPrefab", sparklePrefab);
        SetRef(chest, "openSound", Sound("chest_open"));
        SetRefs(chest, "openFrames", props.Frames("Chest_Open"));
        SetFloat(chest, "fps", props.Anim("Chest_Open").fps);

        return SavePrefab(go, "Chest");
    }

    // Purely decorative: a flickering flame sprite and a flickering light. The root is
    // scaled down; the sprite is a child so Billboard (which sets its own scale) doesn't undo it.
    private static GameObject CreateTorchPrefab(SpriteSheetImporter.SpriteSheet props)
    {
        var go = new GameObject("WallTorch");
        go.transform.localScale = Vector3.one * 0.75f;

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, props, "Torch");

        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0f, 1.5f, 0f); // at the flame
        light.type = LightType.Point;
        light.color = new Color(1f, 0.62f, 0.25f);
        light.range = 6f;
        light.gameObject.AddComponent<FlickerLight>();

        return SavePrefab(go, "WallTorch");
    }

    // Grass_A and Grass_B are plain tufts; Grass_C has little flowers (used sparingly).
    private static GameObject CreateGrassPrefab(SpriteSheetImporter.SpriteSheet props, string anim)
    {
        var go = new GameObject(anim.Replace("_", ""));
        AddLoopingSprite(go, props, anim);
        return SavePrefab(go, anim.Replace("_", ""));
    }

    // A dropped gold coin: spins, pops out of the defeated enemy, and flies to the player.
    private static CoinPickup CreateCoinPrefab(SpriteSheetImporter.SpriteSheet props)
    {
        var go = new GameObject("Coin");
        AddLoopingSprite(go, props, "Coin");
        SetRef(go.AddComponent<CoinPickup>(), "collectSound", Sound("coin"));
        return SavePrefab<CoinPickup>(go, "Coin");
    }

    // The banner on top of the castle keep.
    private static GameObject CreateFlagPrefab(SpriteSheetImporter.SpriteSheet props)
    {
        var go = new GameObject("Flag");
        AddLoopingSprite(go, props, "Flag");
        return SavePrefab(go, "Flag");
    }

    // A camera-facing sprite playing one looping animation, each copy starting at a random
    // frame so rows of them (grass, torches, flags) don't move in lockstep.
    private static void AddLoopingSprite(GameObject go, SpriteSheetImporter.SpriteSheet sheet, string anim)
    {
        go.AddComponent<SpriteRenderer>();
        var flipbook = go.AddComponent<SpriteFlipbook>();
        SetFlipbook(flipbook, sheet, anim, destroyWhenDone: false);
        SetBool(flipbook, "randomStart", true);
        go.AddComponent<Billboard>();
    }

    private static void AddShadow(GameObject go, Sprite shadowSprite, float size)
    {
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadow.transform.localScale = Vector3.one * size;
        shadow.sprite = shadowSprite;
        shadow.sortingOrder = -1;
    }

    // ---------- Items ----------

    // Items are ScriptableObject assets: pure data, shared by everything that refers to them.
    // You can also make new ones by hand: Project window > Create > Dungeon > Item.
    private static ItemDefinition CreateEmberRing()
    {
        var ring = LoadOrCreateAsset<ItemDefinition>("Assets/Items/EmberRing.asset");
        var so = new SerializedObject(ring);
        so.FindProperty("id").stringValue = "ember_ring";
        so.FindProperty("displayName").stringValue = "Ember Ring";
        so.FindProperty("description").stringValue = "A gold band set with a smouldering gem. +1 spell damage.";
        so.FindProperty("icon").objectReferenceValue = SpriteSheetImporter.ImportSingle("Assets/Art/UI/IconEmberRing.png", 16);
        so.FindProperty("slot").enumValueIndex = (int)EquipSlot.Ring;
        so.FindProperty("spellDamageBonus").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        return ring;
    }

    // Every item, so ids from the inventory (and later, save files) can be turned back into items.
    // Add new items to this list as they're created.
    private static ItemDatabase CreateItemDatabase(params ItemDefinition[] items)
    {
        var database = LoadOrCreateAsset<ItemDatabase>("Assets/Items/ItemDatabase.asset");
        SetRefs(database, "items", items);
        return database;
    }

    // An item on the floor: a bobbing, glinting sprite with a faint glow so it's easy to spot.
    private static GameObject CreateItemPickupPrefab(SpriteSheetImporter.SpriteSheet props, ItemDefinition item, Sprite shadowSprite)
    {
        var go = new GameObject("EmberRingPickup");

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        AddLoopingSprite(visual, props, "Ring");

        AddShadow(go, shadowSprite, 0.6f);

        var glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.45f, 0.25f);
        glow.range = 2.5f;
        glow.intensity = 1.5f;

        var pickup = go.AddComponent<ItemPickup>();
        SetRef(pickup, "item", item);
        SetRef(pickup, "visual", visual.transform);
        SetRef(pickup, "pickupSound", Sound("pickup"));
        return SavePrefab(go, "EmberRingPickup");
    }
}
