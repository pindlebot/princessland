using UnityEditor;
using UnityEngine;

// Puddlebrook Lake (Assets/Levels/Lake1-4.txt), the fourth region, and what came with it:
//   Crab             a scuttling shore crab (marker E): it only ever moves sideways across the screen (EnemyAI.sidewaysOnly)
//   Jellyfish        a slow drifter (marker L) that spits bubbles from a distance
//   King Crabbington the Lake's boss (marker M): hides in a great shell (spells tink off) while the tide sweeps the court
//                    (TideWaves), then peeks out, and only then can be hurt (ShellCycle). Beat him and the Bubble Charm (and
//                    the Aquamarine) appear (BossReward)
//   Captain Clamshell the retired crab sailor (legend "npc clamshell"): "Fishing Lesson", then the Fishing Rod
//   fishingspot      legend "prop fishingspot": press E, wait for the bobber to dip, press E again (FishingSpot)
//   swimhint         legend "prop swimhint": the "come back later" bubble over deep water (the Bubble Charm lets you swim)
//   reeds, anchor, lakesign, buoy   scenery (reeds are not solid; the rest are)
// Swimming: SwimAbility. Art: Tools/make_lake_sprites.py. Music: music_lake.
public static partial class DungeonBuilder
{
    private static readonly EnemyStats CrabStats = new EnemyStats
    {
        Name = "Crab", Health = 3, Experience = 20, MinCoins = 2, MaxCoins = 4,
        MoveSpeed = 2.6f, AggroRange = 8f, AttackRange = 1.5f, AttackCooldown = 1.1f,
        HitSound = "crab_click", DeathSound = "crab_click", AttackSound = "crab_click",
    };

    private static readonly EnemyStats JellyStats = new EnemyStats
    {
        Name = "Jelly", Health = 2, Experience = 22, MinCoins = 2, MaxCoins = 5,
        MoveSpeed = 1.3f, AggroRange = 9f, AttackRange = 6.5f, AttackCooldown = 2.2f,
        HitSound = "jelly_bloop", DeathSound = "jelly_bloop", AttackSound = "jelly_bloop",
    };

    // 40 health, hits for 2, worth 500 XP. He can only be hurt while he peeks out of his shell.
    private static readonly EnemyStats KingCrabbingtonStats = new EnemyStats
    {
        Name = "KingCrabbington", Health = 40, Experience = 500, Damage = 2,
        MinCoins = 20, MaxCoins = 26, MinCoinValue = 2, MaxCoinValue = 5,
        MoveSpeed = 2.0f, AggroRange = 11f, AttackRange = 2.9f, AttackCooldown = 1.6f,
        HitSound = "crab_click", DeathSound = "crab_click", AttackSound = "slam_land",
    };

    // ---------- Conversations ----------

    private static Talk[] ClamshellTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Clamshell", Sets = "met:Clamshell",
            Lines = new[]
            {
                N("Harr! A landlubber on my shore! Captain Clamshell, retired, of the good ship Pinch. Welcome to Puddlebrook."),
                H("Why is the lake so murky?"),
                N("The Grey Gloom stirred it up three hundred years ago, and the merfolk sank away to get out of the muck. Nothing but crabs and jellies about now."),
                N("But there's still fish, if you've the patience. Come, I'll teach you. Press the button at a fishing spot, wait for the bobber to dip, then PRESS AGAIN before he gets away. Not too early, mind!"),
                N("Catch three fish, {hero}, and I'll give you a proper rod."),
            },
        },
        new Talk
        {
            Requires = "fish_caught>=" + QuestCatalog.FishingGoal, NotIf = "thanked:clamshell", Sets = "thanked:clamshell", Gold = 20, GiveItem = "fishing_rod",
            Lines = new[]
            {
                N("Three fish! Shipshape! You've the hands of a born angler."),
                N("Here: my old rod, and twenty coins for the bait. With it you get a bit longer to react, and the big fish like it better."),
                N("Keep an eye out for the Golden Carp. Legend says one lives in the lake. Never caught him myself. Harr!"),
            },
        },
        new Talk
        {
            Requires = "cleared:Lake4", NotIf = "clamshell:cheered", Sets = "clamshell:cheered",
            Lines = new[]
            {
                N("The whole lake just sighed! King Crabbington's himself again. He's my old first mate, you know. Gets crabby when the Gloom's about."),
                N("And you've the Bubble Charm! Wear it and you can swim, {hero}: out over the deep water, where the heavy things hide. There's islands in this lake no boat ever reached."),
                N("The old castle pond, the cove, and the sea: there's treasure where only swimmers go."),
            },
        },
        new Talk
        {
            NotIf = "fish_caught>=" + QuestCatalog.FishingGoal,
            Lines = new[] { N("Three fish, {hero}. Cast, wait for the bobber to dip, then press again. Patience is half of fishing, and the other half is fish.") },
        },
        new Talk { SmallTalk = true, Lines = new[] { N("Why don't crabs share their toys?"), H("Why?"), N("Because they're SHELL-fish! Harr harr harr!") } },
        new Talk { SmallTalk = true, Requires = "found:goldencarp", Lines = new[] { N("You caught the GOLDEN CARP?! In all my years! Take a bow, {hero}. Take two.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("That anchor on the beach? Mine. Lost it in '94. It's still a good anchor. Just not a good boat.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("A crab can only walk sideways, you know. Doesn't matter. I get where I'm going eventually.") } },
    };

    // The way (in world x/z) from a tile to its first water neighbour: where a fishing spot's bobber goes.
    private static Vector3 WaterDirection(string[] map, int col, int row)
    {
        foreach (var (dc, dr) in new[] { (0, -1), (1, 0), (-1, 0), (0, 1) })
            if (LevelMap.IsWater(MapAt(map, col + dc, row + dr))) return new Vector3(dc, 0f, -dr);
        return new Vector3(0f, 0f, -1f);
    }

    // ---------- Prefabs ----------

    private static void CreateLakePrefabs(SharedAssets assets, Sprite shadow, CoinPickup coin, GameObject sparkle)
    {
        assets.Crab = CreateEnemyPrefab(CharacterSpriteBuilder.Build("Crab"), CrabStats, coin, sparkle);
        SetBool(assets.Crab.GetComponent<EnemyAI>(), "sidewaysOnly", true);
        assets.Jelly = CreateJellyPrefab(coin, sparkle);
        assets.KingCrabbington = CreateKingPrefab(coin, sparkle, assets.Crab);
        assets.Clamshell = CreateNpcPrefab("Clamshell", "Clamshell", "Captain Clamshell", "Assets/Art/UI/PortraitClamshell.png", "voice_clamshell",
                                           ClamshellTalks(), new Vector3(1.4f, 2f, 0.9f), shadow, 1.8f);

        var bubbles = SpriteSheetImporter.Import("HintBubbles");
        assets.HintCharm = CreateHintPrefab(bubbles, "Charm", "HintCharm", Abilities.BubbleCharm,
                                            "That's deep water! Maybe a bubble to float on would help?");
        var hint = new GameObject("SwimHint");
        Place(assets.HintCharm, hint.transform, Vector3.up * 2.6f);

        var props = assets.PropPrefabs;
        var lake = SpriteSheetImporter.Import("LakeProps");
        props["swimhint"] = SavePrefab(hint, "SwimHint");
        props["reeds"] = Scenic("Reeds", lake, "Reeds", null, shadow, 0f);
        props["anchor"] = Scenic("Anchor", lake, "Anchor", new Vector3(1.3f, 2.2f, 0.8f), shadow, 1.8f);
        props["lakesign"] = CreateFixture(lake, "LakeSign", new Vector3(1.2f, 2.2f, 0.6f), shadow, 1.2f, "Read the sign",
            "A wooden sign with an arrow pointing at the water. | " +
            "\"FISHING: this way. NO FISHING: that way. (Ask the Captain which way is which.)\"",
            HouseFixture.Effect.Read, "paper", f => SetString(f, "useFlag", "found:lakesign"));
        props["buoy"] = CreateFixture(lake, "Buoy", new Vector3(1.0f, 2.0f, 1.0f), shadow, 1.4f, "Look at the buoy",
            "A red-and-white buoy with a little yellow flag. It bobs, even though it's on land. | " +
            "Somebody has written on it in marker: \"This buoy is NOT a toy.\" You give it a poke. It wobbles. Fun!",
            HouseFixture.Effect.Read, "bell", f => SetString(f, "useFlag", "found:buoy"));
        props["fishingspot"] = CreateFishingSpotPrefab(lake, shadow);
    }

    private static GameObject CreateFishingSpotPrefab(SpriteSheetImporter.SpriteSheet lake, Sprite shadow)
    {
        var bobberSheet = SpriteSheetImporter.Import("Bobber");
        var go = new GameObject("FishingSpot");
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.0f, 1.2f, 1.0f);
        box.center = new Vector3(0f, 0.6f, 0f);
        AddStaticSprite(go, lake.Frames("FishingSpot")[0]);
        AddShadow(go, shadow, 1.4f);

        var bobber = new GameObject("Bobber");
        bobber.transform.SetParent(go.transform, false);
        var renderer = bobber.AddComponent<SpriteRenderer>();
        renderer.sprite = bobberSheet.Frames("Float")[0];
        renderer.sortingOrder = 10;
        bobber.AddComponent<Billboard>();

        var spot = go.AddComponent<FishingSpot>();
        SetRef(spot, "bobber", renderer);
        SetRefs(spot, "floatFrames", bobberSheet.Frames("Float"));
        SetRefs(spot, "dipFrames", bobberSheet.Frames("Dip"));
        SetRef(spot, "castSound", Sound("oars"));
        SetRef(spot, "biteSound", Sound("fish_bite"));
        SetRef(spot, "catchSound", Sound("fish_catch"));
        return SavePrefab(go, "FishingSpot");
    }

    private static GameObject CreateJellyPrefab(CoinPickup coin, GameObject defeatEffect)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("Jelly"), JellyStats, coin, defeatEffect);
        // It hovers: the sprite floats a little above the floor, and it spits bubbles from its middle.
        go.transform.Find("Sprite").localPosition = new Vector3(0f, -0.6f, 0f);
        var mouth = new GameObject("ThrowPoint").transform;
        mouth.SetParent(go.transform, false);
        mouth.localPosition = new Vector3(0f, 0.3f, 0f);
        var ai = go.GetComponent<EnemyAI>();
        SetRef(ai, "boltPrefab", AssetDatabase.LoadAssetAtPath<EnemyBolt>("Assets/Prefabs/BubbleBolt.prefab")
                                 ?? CreateBoltPrefab("BubbleBolt", new Color(0.8f, 0.7f, 1f)));
        SetRef(ai, "throwPoint", mouth);
        return SavePrefab(go, "Jelly");
    }

    private static GameObject CreateKingPrefab(CoinPickup coin, GameObject defeatEffect, GameObject crab)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("KingCrabbington"), KingCrabbingtonStats, coin, defeatEffect);
        MakeBig(go, radius: 1.5f, height: 2.8f, center: 0.3f, shadowSize: 3.6f);

        var boss = AddBossAbilities(go, "King Crabbington", engageRange: 10f);
        SetString(boss, "engageMessage", "{0} scuttles out of the reeds!");
        SetString(boss, "splitMessage", "{0} calls his crabby crew!");
        SetString(boss, "defeatMessage", "{0} is beaten! The Lake can breathe again!");
        SetBool(boss, "clearsLevel", true);
        SetFloat(boss, "slamCooldown", 7f);
        SetRef(boss, "minionPrefab", crab);
        SetInt(boss, "minionCount", 3);

        // His shell: shown (and he vanishes) while he hides.
        var shellSheet = SpriteSheetImporter.Import("KingShell");
        var shell = new GameObject("Shell");
        shell.transform.SetParent(go.transform, false);
        shell.transform.localPosition = new Vector3(0f, -1f, 0f);
        AddLoopingSprite(shell, shellSheet, "Shell");
        shell.AddComponent<Billboard>();
        var cycle = go.AddComponent<ShellCycle>();
        SetRef(cycle, "shell", shell);
        SetRef(cycle, "body", go.transform.Find("Sprite").GetComponent<SpriteRenderer>());
        SetRef(cycle, "hideSound", Sound("shell_close"));
        SetRef(cycle, "peekSound", Sound("boss_roar"));
        SetRef(cycle, "clinkSound", Sound("clink"));

        var tide = go.AddComponent<TideWaves>();
        SetRef(tide, "waveSound", Sound("tide_wave"));
        return SavePrefab(go, "KingCrabbington");
    }
}
