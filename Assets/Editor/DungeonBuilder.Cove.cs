using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Mermaid Cove (Assets/Levels/Cove.txt): pirates ('J'), the dark mermaids who throw bolts from
// the water ('&'), Pearl, Coralie's big sister ('Z', in the water), palms ('q'), the pirates' ship
// at anchor ('@'), their treasure ('$'), waterfalls pouring off the cliffs ('|') and off the
// island's edge, and the rowboat that every door on an outdoor level is (the castle grounds'
// pond <-> the cove's jetty). Art comes from Tools/make_cove_sprites.py.
public static partial class DungeonBuilder
{
    // A deckhand with a cutlass: a little tougher and quicker to swing than a skeleton.
    private static readonly EnemyStats PirateStats = new EnemyStats
    {
        Name = "Pirate", Health = 3, Experience = 20, MinCoins = 3, MaxCoins = 5,
        MoveSpeed = 3f, AggroRange = 8f, AttackRange = 1.4f, AttackCooldown = 1.1f,
        HitSound = "pirate_hit", DeathSound = "pirate_death", AttackSound = "enemy_attack",
    };

    // She stays in the water and throws a slow bolt every couple of seconds from up to 8m away
    // (you can step aside). AttackRange is her throwing range; she never chases.
    private static readonly EnemyStats DarkMermaidStats = new EnemyStats
    {
        Name = "DarkMermaid", Health = 3, Experience = 25, MinCoins = 3, MaxCoins = 6,
        MoveSpeed = 0f, AggroRange = 9f, AttackRange = 8f, AttackCooldown = 2.4f,
        HitSound = "siren_hit", DeathSound = "siren_hit", AttackSound = "siren_cast",
    };

    private static Talk[] PearlTalks() => new[]
    {
        new Talk
        {
            Requires = "cleared:Cove", NotIf = "thanked:cove", Sets = "thanked:cove,met:Pearl", Gold = 30,
            Lines = new[]
            {
                N("You did it, {hero}! You beat Captain Grumblebeard and the sea-spell is broken. My friends are themselves again, and they're SO embarrassed."),
                N("And the pirates rowed off in such a hurry, they left their secret stair in the sea cave wide open!"),
                N("Here, take these sea-coins. Coralie will want to hear all about this!"),
            },
        },
        new Talk
        {
            NotIf = "met:Pearl", Sets = "met:Pearl",
            Lines = new[]
            {
                N("Eep! Quick, hide behind a palm tree! ...Oh. You're not a pirate. Phew!"),
                N("I'm Pearl, Coralie's big sister. Welcome to Mermaid Cove, {hero}."),
                H("Coralie, from the castle pond? She's lovely!"),
                N("She is! But a band of pirates sailed in last week. Their captain has a grumpy old sea-spell, and he cast it on my friends."),
                N("Now they glow, and sulk, and throw dark bolts at everybody. They don't really mean it!"),
                N("Break the spell with your magic, and chase those pirates off our beaches. Mind the bolts: step aside when you see one coming!"),
                N("Their captain, Grumblebeard, guards the sea cave from the beach below it, with a parrot, a peg leg and the loudest cannons you ever heard."),
                N("When you see a red circle on the sand, don't stand in it!"),
                H("Leave it to me!"),
            },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("The waterfalls come all the way down from the clouds. Coralie swam UP one once. Show-off.") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("The pirates keep their treasure in the sea cave, past the stream. Be careful in there!") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Why are pirates called pirates?"), H("Why?"), N("Because they ARRR! Hee hee!") },
        },
        new Talk
        {
            SmallTalk = true, Requires = FrogBush.FoundFlag,
            Lines = new[] { N("A seagull brought a letter from Coralie: you found Sir Hopsalot! He's the bravest frog in the world.") },
        },
    };

    private static void CreateCovePrefabs(SharedAssets assets, CoinPickup coin, GameObject defeatEffect, Sprite shadow)
    {
        assets.Pirate = CreateEnemyPrefab(CharacterSpriteBuilder.Build("Pirate"), PirateStats, coin, defeatEffect);
        assets.DarkMermaid = CreateDarkMermaidPrefab(coin, defeatEffect);
        assets.Pearl = CreateNpcPrefab("Pearl", "Pearl", "Pearl", "Assets/Art/UI/PortraitPearl.png",
                                       "voice_mermaid", PearlTalks(), null, null, 0f);

        var coast = SpriteSheetImporter.Import("Coast");
        assets.Palm = Scenic("Palm", coast, "Palm", new Vector3(0.6f, 2f, 0.6f), shadow, 2.2f);
        assets.Treasure = Scenic("Treasure", coast, "Treasure", new Vector3(1.6f, 0.8f, 1.2f), shadow, 2.2f);
        assets.Rowboat = CreateRowboat(coast);

        var splash = new GameObject("Splash");
        AddLoopingSprite(splash, coast, "Splash");
        assets.Splash = SavePrefab(splash, "Splash");

        // The pirates' ship bobs at anchor: 6m long, drawn at that size. Its waterline is 10 texels
        // above the bottom of the frame, so the sprite sits that much below the sea.
        var ship = new GameObject("PirateShip");
        var shipSprite = new GameObject("Sprite");
        shipSprite.transform.SetParent(ship.transform, false);
        shipSprite.transform.localPosition = new Vector3(0f, -0.2f - 10f / ArtStyle.PixelsPerUnit, 0f);
        AddLoopingSprite(shipSprite, SpriteSheetImporter.Import("Ship"), "Bob");
        assets.Ship = SavePrefab(ship, "PirateShip");

        var decals = SpriteSheetImporter.Import("CoveDecals");
        assets.Shell = CreateFlatDecal("Shell", decals, "Shell", 1f);
        assets.Starfish = CreateFlatDecal("Starfish", decals, "Starfish", 1f);
        assets.Foam = CreateFlatDecal("Foam", decals, "Foam", 1f);
    }

    // An enemy that stays in her spot in the water: no chasing, no shoving, no shadow (she's in
    // the sea), and a bolt instead of a melee hit.
    private static GameObject CreateDarkMermaidPrefab(CoinPickup coin, GameObject defeatEffect)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("DarkMermaid"), DarkMermaidStats, coin, defeatEffect);
        Object.DestroyImmediate(go.transform.Find("Shadow").gameObject);
        // Her art has the sea's surface 3 texels up from her pivot: sink it to the water (0.2m down).
        go.transform.Find("Sprite").localPosition = new Vector3(0f, -1.2f - 3f / ArtStyle.PixelsPerUnit, 0f);

        var hand = new GameObject("ThrowPoint").transform;
        hand.SetParent(go.transform, false);
        hand.localPosition = new Vector3(0f, 0.3f, 0f);

        var ai = go.GetComponent<EnemyAI>();
        SetBool(ai, "stationary", true);
        SetRef(ai, "boltPrefab", CreateDarkBoltPrefab());
        SetRef(ai, "throwPoint", hand);
        return SavePrefab(go, "DarkMermaid");
    }

    // The bolt: the same parts as a hero's spell (DarkBolt.png from make_spell_sprites.py), with
    // EnemyBolt instead of Projectile.
    private static EnemyBolt CreateDarkBoltPrefab() => CreateBoltPrefab("DarkBolt", new Color(0.75f, 0.4f, 1f));

    // Any monster bolt: a sheet named after it (Fly + Impact), and the colour of its glow.
    private static EnemyBolt CreateBoltPrefab(string name, Color glow)
    {
        var sheet = SpriteSheetImporter.Import(name);
        var go = new GameObject(name);
        var sphere = go.AddComponent<SphereCollider>();
        sphere.radius = 0.25f;
        sphere.isTrigger = true;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var bolt = go.AddComponent<EnemyBolt>();
        SetRef(bolt, "impactPrefab", CreateImpactPrefab(name, sheet, glow));
        SetRef(bolt, "impactSound", Sound("bolt_impact"));

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.AddComponent<SpriteRenderer>();
        SetFlipbook(sprite.AddComponent<SpriteFlipbook>(), sheet, "Fly", destroyWhenDone: false);
        sprite.AddComponent<FaceTravelDirection>();

        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.type = LightType.Point;
        light.color = glow;
        light.range = 4f;
        light.intensity = 2.5f;
        return SavePrefab<EnemyBolt>(go, name);
    }

    // A door on an outdoor level: a rowboat moored at the end of a jetty. The jetty tile is solid
    // (so nobody walks off its end); the boat floats on the water beyond it (PlaceDoor turns it).
    private static GameObject CreateRowboat(SpriteSheetImporter.SpriteSheet coast)
    {
        var go = new GameObject("Rowboat");
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1f, 0f);
        box.size = new Vector3(Tile, 2f, Tile);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, coast, "Rowboat");
        var door = go.AddComponent<SceneDoor>();
        SetRef(door, "openSound", Sound("oars"));
        return SavePrefab(go, "Rowboat");
    }

    // The boat sits on the water on the far side of the jetty from where you stand. Its waterline
    // is 13 texels above the bottom of its frame.
    private static void MoorBoat(GameObject boat, Vector3 awayFromLand, string targetScene)
    {
        boat.transform.Find("Sprite").localPosition =
            awayFromLand.normalized * 1.3f + Vector3.down * (0.2f + 13f / ArtStyle.PixelsPerUnit);
        SetString(boat.GetComponent<SceneDoor>(), "prompt", $"Row to {SaveSystem.PlaceName(targetScene)}");
    }

    // ---------- Waterfalls ----------

    // A waterfall ('|') on a tall crag: water across its top, falling down its south face (the
    // one the camera sees), and churning white water where it lands. Two side by side make one
    // wide fall.
    private static void AddWaterfall(Transform level, Transform decor, SharedAssets assets, Vector3 pos)
    {
        var top = FlatQuad("FallTop", level, pos + Vector3.up * (CragHeight + 0.01f), new Vector2(Tile, Tile), assets.Materials["Sea"]);
        SetVector2(top.AddComponent<WaterScroll>(), "speed", new Vector2(0f, 0.25f));

        var face = FallQuad(level, pos + Vector3.back * (Tile * 0.5f + 0.02f) + Vector3.up * (CragHeight * 0.5f),
                            Vector3.back, CragHeight, assets.Materials["Waterfall"]);
        face.name = "Waterfall";
        Place(assets.Splash, decor, pos + Vector3.back * (Tile * 0.5f + 0.4f) + Vector3.down * 0.45f);
    }

    // Where the sea reaches the island's edge it spills over: earth below the water, and on the
    // sides the camera sees (south and west) a curtain of water falling down the cliff.
    private static void AddEdgeFall(Transform level, string[] map, int col, int row, Vector3 pos, Dictionary<string, Material> mats)
    {
        Block("Cliff", level, pos + Vector3.down * (0.7f + 2f), new Vector3(Tile, 4f, Tile), mats["EarthSide"]);
        foreach (var (dc, dr) in new[] { (0, 1), (-1, 0) })
        {
            if (MapAt(map, col + dc, row + dr) != ' ') continue;
            var outward = new Vector3(dc, 0f, -dr);
            FallQuad(level, pos + outward * (Tile * 0.5f + 0.02f) + Vector3.down * 2.2f, outward, 4f, mats["Waterfall"]);
        }
    }

    // A sheet of falling water facing `outward`, `height` metres tall, its texture repeating once
    // a metre (so its pixels match everything else) and sliding down.
    private static GameObject FallQuad(Transform parent, Vector3 center, Vector3 outward, float height, Material mat)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "EdgeFall";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(parent);
        quad.transform.position = center;
        quad.transform.rotation = Quaternion.LookRotation(-outward); // a quad's visible side faces -forward
        quad.transform.localScale = new Vector3(Tile, height, 1f);
        var renderer = quad.GetComponent<Renderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        var scroll = quad.AddComponent<WaterScroll>();
        SetVector2(scroll, "speed", new Vector2(0f, 1.5f));
        SetVector2(scroll, "tiling", new Vector2(1f, height));
        return quad;
    }

    private static GameObject FlatQuad(string name, Transform parent, Vector3 center, Vector2 size, Material mat)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(parent);
        quad.transform.position = center;
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // face up
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = quad.GetComponent<Renderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return quad;
    }

    private static void SetVector2(Object target, string field, Vector2 value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
