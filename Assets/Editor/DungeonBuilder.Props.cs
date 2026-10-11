using System.Linq;
using UnityEditor;
using UnityEngine;

// Props: chest, wall torch, grass tufts, the castle flag. (Items are in DungeonBuilder.Items.cs.)
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
        glow.intensity = 3f * LightBoost;
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

    // Purely decorative: a flickering flame sprite and a flickering light. The art is drawn at
    // its in-game size, so like every sprite it's placed at scale 1.
    private static GameObject CreateTorchPrefab(SpriteSheetImporter.SpriteSheet props)
    {
        var go = new GameObject("WallTorch");

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, props, "Torch");

        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0f, 0.75f, 0f); // at the flame
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

    // A flat pixel shadow about `size` units across. Shadows are never scaled (that would make
    // their pixels bigger than everyone else's): the nearest size from Shadows.png is used.
    // shadowSprite is the 1-unit one, used when nothing closer fits.
    private static void AddShadow(GameObject go, Sprite shadowSprite, float size, float lift = 0.02f)
    {
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        shadow.transform.localPosition = new Vector3(0f, lift, 0f) + ArtStyle.ShadowOffset(ArtStyle.OutdoorSun);
        shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadow.sprite = ShadowSprite(size) ?? shadowSprite;
        shadow.sortingOrder = -1;
        SetFloat(shadow.gameObject.AddComponent<GroundShadow>(), "lift", lift);
    }

    private static SpriteSheetImporter.SpriteSheet shadowSheet; // Shadows.png, loaded by CreateAssets

    private static Sprite ShadowSprite(float size)
    {
        if (shadowSheet == null) return null;
        var best = shadowSheet.Layout.animations
            .OrderBy(a => Mathf.Abs(int.Parse(a.name.Substring("Shadow_".Length)) / (float)ArtStyle.PixelsPerUnit - size))
            .First();
        return shadowSheet.Frames(best.name)[0];
    }
}
