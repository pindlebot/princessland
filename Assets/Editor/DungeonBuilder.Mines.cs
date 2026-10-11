using System.Linq;
using UnityEditor;
using UnityEngine;

// The Glimmer Mines (Assets/Levels/Mines1-4.txt), the third region, and what came with it:
//   Bat              a fast little flier (marker E): one hit, but it swoops in quickly
//   Pebblin          a slow, tough rock critter (marker L) with an amber crystal on its head
//   Crystal Golem    the Mines' boss (marker M): Ground Slam, a Volley of topaz shards, Pebblins when he's hurt, and
//                    crystals that take turns glowing: he can only be hurt while they glow (GolemCrystals).
//                    Beat him and the Mole Mitts (and the Topaz) appear (BossReward)
//   Digby            the mole foreman (legend "npc digby"): "Lost Moles", and the mine-cart line
//   Molly, Mortimer, Mo   the three lost moles (legend "npc molly" / "mortimer" / "mo"): find them in the dark tunnels
//   block            legend "prop block": a heavy stone block you push with the Mole Mitts (PushBlock)
//   softdirt         legend "prop softdirt": a mound you dig with the Mitts (SoftDirt); "dirtheart" and "dirtshard" hide
//                    a heart piece or a star shard
//   wallcrystal      legend "prop wallcrystal": a glowing blue crystal cluster (solid, with a light)
//   minecart         legend "prop minecart": the fast-travel line (MineCart): press E beside it
// Art: Tools/make_mines_sprites.py. Music: music_mines.
public static partial class DungeonBuilder
{
    private static readonly EnemyStats BatStats = new EnemyStats
    {
        Name = "Bat", Health = 1, Experience = 12, MinCoins = 1, MaxCoins = 3,
        MoveSpeed = 4.4f, AggroRange = 9f, AttackRange = 1.2f, AttackCooldown = 0.9f,
        HitSound = "bat_squeak", DeathSound = "bat_squeak", AttackSound = "bat_squeak",
    };

    private static readonly EnemyStats PebblinStats = new EnemyStats
    {
        Name = "Pebblin", Health = 4, Experience = 25, MinCoins = 3, MaxCoins = 5,
        MoveSpeed = 1.9f, AggroRange = 7f, AttackRange = 1.3f, AttackCooldown = 1.4f,
        HitSound = "rock_hit", DeathSound = "rock_death", AttackSound = "rock_hit",
    };

    // 36 health, hits for 2, worth 450 XP. He can only be hurt while his crystals glow, so it takes a few rounds.
    private static readonly EnemyStats CrystalGolemStats = new EnemyStats
    {
        Name = "CrystalGolem", Health = 36, Experience = 450, Damage = 2,
        MinCoins = 18, MaxCoins = 24, MinCoinValue = 2, MaxCoinValue = 5,
        MoveSpeed = 1.6f, AggroRange = 11f, AttackRange = 2.8f, AttackCooldown = 1.6f,
        HitSound = "rock_hit", DeathSound = "rock_death", AttackSound = "slam_land",
    };

    // ---------- Conversations ----------

    private static Talk[] DigbyTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Digby", Sets = "met:Digby",
            Lines = new[]
            {
                N("Well, dig me sideways! A visitor! Digby's the name, foreman of the Glimmer Mines. Mind the bats, they've no manners."),
                H("Why is it so dark in here?"),
                N("The Grey Gloom's gloom, that's why. It put out every lamp in the mine, and moles can't see a thing without a lamp."),
                N("Three of my crew wandered off into the tunnels when the lights went out: Molly, Mortimer and Mo. They'll be lost and a bit scared."),
                N("Find all three and tell them to come home, {hero}, and I'll have a surprise for you. Try the dark tunnels, east of here."),
            },
        },
        new Talk
        {
            Requires = "moles_found>=" + QuestCatalog.LostMoleGoal, NotIf = "thanked:digby", Sets = "thanked:digby", Gold = 40,
            Lines = new[]
            {
                N("There they are! Molly, Mortimer and Mo, safe and sound! I could hug you, {hero}. And I'd better, with these paws."),
                H("Is everyone all right?"),
                N("Right as rain, and twice as muddy. Here, a little something for your trouble. Forty coins!"),
                N("And I've cleared the rubble off the old mine-cart line. Climb in any cart and press E: it rattles you down the line from the Entrance to the Tunnels to the Crystal Cavern, and round again."),
            },
        },
        new Talk
        {
            Requires = "thanked:digby", NotIf = "digby:golem", Sets = "digby:golem",
            Lines = new[]
            {
                N("Now, listen. Past the Crystal Cavern is the Golem's Chamber, and Rocky lives there. He's a lovely lad, really, only grumpy: the Gloom got into his crystals."),
                N("His crystals glow, then go dark, then glow again. Hit him while they GLOW: when they're dark, his hide is like a mountain."),
                N("And when it goes dark, the whole cave does. That Fairy Lantern of yours would be handy, if you have one."),
            },
        },
        new Talk
        {
            Requires = "cleared:Mines4", NotIf = "digby:cheered", Sets = "digby:cheered",
            Lines = new[]
            {
                N("The mine's gone quiet! You did it! Rocky's himself again. Oh, and look: he left the Mole Mitts."),
                N("Wear 'em and lean on a big stone block, and you can shove it along. Press E at a mound of loose soil to dig it up."),
                N("Plenty of those about the island, I'd wager. Blocks too. Funny how you only spot 'em when you can do something about 'em."),
            },
        },
        new Talk
        {
            NotIf = "moles_found>=" + QuestCatalog.LostMoleGoal,
            Lines = new[] { N("Three of them, {hero}: Molly, Mortimer and Mo. Try the tunnels, east of the entrance. Bring a light!") },
        },
        new Talk { SmallTalk = true, Lines = new[] { N("Why did the mole bring a ladder to work?"), H("Why?"), N("To get to the top soil! Heh heh heh. Oh, I'm good.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("A mole's favourite music? Anything with a good BASS-ment. Haha!") } },
        new Talk { SmallTalk = true, Requires = "dirt_dug>=3", Lines = new[] { N("Dug up three mounds already? You've a natural talent, {hero}. Ever think of a career in the soil trade?") } },
        new Talk { SmallTalk = true, Lines = new[] { N("Mind the carts. Last week one rolled off on its own. Nobody was in it. Spooky, eh?") } },
    };

    private static Talk[] MoleTalks(string id, string first, string again) => new[]
    {
        new Talk { NotIf = "found:" + id, Sets = "found:" + id + ",+moles_found", Lines = new[] { N(first) } },
        new Talk { SmallTalk = true, Lines = new[] { N(again) } },
    };

    // ---------- Prefabs ----------

    private static void CreateMinesPrefabs(SharedAssets assets, Sprite shadow, CoinPickup coin, GameObject sparkle)
    {
        assets.Bat = CreateEnemyPrefab(CharacterSpriteBuilder.Build("Bat"), BatStats, coin, sparkle);
        assets.Pebblin = CreateEnemyPrefab(CharacterSpriteBuilder.Build("Pebblin"), PebblinStats, coin, sparkle);
        assets.CrystalGolem = CreateGolemPrefab(coin, sparkle, assets.Pebblin);

        assets.Digby = CreateNpcPrefab("Digby", "Digby", "Digby", "Assets/Art/UI/PortraitDigby.png", "voice_digby",
                                       DigbyTalks(), new Vector3(0.8f, 2f, 0.8f), shadow, 1.4f);
        assets.Molly = CreateNpcPrefab("Molly", "Molly", "Molly", "Assets/Art/UI/PortraitMole.png", "voice_mole",
            MoleTalks("molly", "Eep! A light! Oh, thank goodness. I've been counting the stones to stay calm: four thousand, three hundred and eighty-two. Tell Digby I'm fine, and I'll follow the rails home!",
                      "Four thousand, three hundred and eighty-three. No, wait, that's the same stone."),
            new Vector3(0.8f, 1.8f, 0.8f), shadow, 1.2f);
        assets.Mortimer = CreateNpcPrefab("Mortimer", "Mortimer", "Mortimer", "Assets/Art/UI/PortraitMole.png", "voice_mole",
            MoleTalks("mortimer", "Mmm? Is it Tuesday? I sat down for a tiny nap behind this rock. Digby sent you? Tell him I'll be right along. Just... five more minutes.",
                      "It IS Tuesday. I knew it."),
            new Vector3(0.8f, 1.8f, 0.8f), shadow, 1.2f);
        assets.Mo = CreateNpcPrefab("Mo", "Mo", "Mo", "Assets/Art/UI/PortraitMole.png", "voice_mole",
            MoleTalks("mo", "Shh! Not so loud! I'm hiding from the bats. They're very rude, and they've got fangs. Is it safe? ...Is it REALLY safe? Oh, thank you! I'll tell Digby I'm on my way.",
                      "Is it safe? ...I'll just stay here a bit longer."),
            new Vector3(0.8f, 1.8f, 0.8f), shadow, 1.2f);

        var props = assets.PropPrefabs;
        var mines = SpriteSheetImporter.Import("MinesProps");
        var bubbles = SpriteSheetImporter.Import("HintBubbles");
        assets.HintMitts = CreateHintPrefab(bubbles, "Mitts", "HintMitts", Abilities.MoleMitts,
                                            "It's too heavy to move by hand. Maybe something with big, strong paws would help?");

        props["block"] = CreateBlockPrefab(mines, shadow, assets);
        props["softdirt"] = CreateDirtPrefab(mines, shadow, sparkle, assets);
        props["dirtheart"] = props["softdirt"];  // the same mound: PlaceBuriedReward adds the treasure
        props["dirtshard"] = props["softdirt"];
        props["wallcrystal"] = Scenic("WallCrystal", mines, "WallCrystal", new Vector3(1.4f, 2.2f, 1.4f), shadow, 2f);
        AddGlow(props["wallcrystal"], new Vector3(0f, 1.2f, -0.3f), new Color(0.45f, 0.8f, 1f), 6f, 1.3f);
        props["minecart"] = CreateMineCartPrefab(mines, shadow);
        props["minesign"] = CreateFixture(mines, "MineSign", new Vector3(1.4f, 2.2f, 0.6f), shadow, 1.4f,
            "Read the sign",
            "A wooden sign with a bat drawn on it, crossed out in red. | " +
            "\"PLEASE DO NOT FEED THE BATS. They are full. They are always full. Ask Digby.\"",
            HouseFixture.Effect.Read, "paper", f => SetString(f, "useFlag", "found:minesign"));
        props["moledoor"] = CreateFixture(mines, "MoleDoor", new Vector3(1.8f, 2.2f, 1.2f), shadow, 2f,
            "Look at the little door",
            "A tiny round door in the rock, with a welcome mat. | " +
            "A note on the door says: \"Gone digging. Back never. Please leave biscuits.\" | " +
            "You leave a coin. Somewhere inside, someone says \"ooh!\"",
            HouseFixture.Effect.Read, "paper", f => SetString(f, "useFlag", "found:moledoor"));
        props["petrock"] = CreateFixture(mines, "PetRock", new Vector3(1.4f, 1.2f, 1.0f), shadow, 1.6f,
            "Pat the rock",
            "A rock with two googly eyes, and a tiny flower tucked beside it. | " +
            "You pat it. It does not move. But somehow, you can tell it is very happy.",
            HouseFixture.Effect.Read, "bell", f => { SetString(f, "useFlag", "found:petrock"); SetString(f, "useCounter", "pets"); });
    }

    private static GameObject CreateBlockPrefab(SpriteSheetImporter.SpriteSheet sheet, Sprite shadow, SharedAssets assets)
    {
        var go = new GameObject("StoneBlock");
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(Tile * 0.9f, 1.8f, Tile * 0.9f);
        box.center = new Vector3(0f, 0.9f, 0f);
        AddStaticSprite(go, sheet.Frames("Block")[0]);
        AddShadow(go, shadow, 2.2f);
        var block = go.AddComponent<PushBlock>();
        SetRef(block, "pushSound", Sound("clunk"));
        Place(assets.HintMitts, go.transform, go.transform.position + Vector3.up * 2.9f);
        return SavePrefab(go, "StoneBlock");
    }

    private static GameObject CreateDirtPrefab(SpriteSheetImporter.SpriteSheet sheet, Sprite shadow, GameObject sparkle, SharedAssets assets)
    {
        var go = new GameObject("SoftDirt");
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(Tile * 0.9f, 1.4f, Tile * 0.9f);
        box.center = new Vector3(0f, 0.7f, 0f);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, sheet, "SoftDirt");
        var dirt = go.AddComponent<SoftDirt>();
        SetRef(dirt, "solid", box);
        SetRef(dirt, "dust", sparkle);
        SetRef(dirt, "digSound", Sound("dig"));
        Place(assets.HintMitts, go.transform, go.transform.position + Vector3.up * 2.3f);
        return SavePrefab(go, "SoftDirt");
    }

    private static GameObject CreateMineCartPrefab(SpriteSheetImporter.SpriteSheet sheet, Sprite shadow)
    {
        var go = new GameObject("MineCart");
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(2.4f, 1.4f, 1.4f);
        box.center = new Vector3(0f, 0.7f, 0f);
        AddStaticSprite(go, sheet.Frames("MineCart")[0]);
        AddShadow(go, shadow, 2.6f);
        var cart = go.AddComponent<MineCart>();
        SetRef(cart, "rideSound", Sound("cart_ride"));
        SetRef(cart, "blockedSound", Sound("door_locked"));
        return SavePrefab(go, "MineCart");
    }

    // ---------- The Crystal Golem ----------

    private static GameObject CreateGolemPrefab(CoinPickup coin, GameObject defeatEffect, GameObject pebblin)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("CrystalGolem"), CrystalGolemStats, coin, defeatEffect);
        MakeBig(go, radius: 1.4f, height: 3f, center: 0.4f, shadowSize: 3.4f);

        var boss = AddBossAbilities(go, "The Crystal Golem", engageRange: 10f);
        SetString(boss, "engageMessage", "{0} rumbles awake!");
        SetString(boss, "splitMessage", "{0} shakes loose Pebblins!");
        SetString(boss, "volleyMessage", "Topaz shards! Step between them!");
        SetString(boss, "defeatMessage", "{0} is beaten! The Mines can breathe again!");
        SetBool(boss, "clearsLevel", true);
        SetFloat(boss, "slamCooldown", 8f);
        SetRef(boss, "minionPrefab", pebblin);
        SetInt(boss, "minionCount", 3);

        // His Volley: a fan of amber shards.
        SetRef(boss, "boltPrefab", AssetDatabase.LoadAssetAtPath<EnemyBolt>("Assets/Prefabs/ShardBolt.prefab")
                                   ?? CreateBoltPrefab("ShardBolt", new Color(1f, 0.75f, 0.3f)));
        SetInt(boss, "volleyBolts", 5);
        SetFloat(boss, "volleyArc", 60f);
        SetFloat(boss, "volleyCooldown", 9f);
        SetRef(boss, "volleySound", Sound("slam_land"));

        // The crystals' glow: a warm light that's only on while he can be hurt.
        var light = new GameObject("CrystalGlow").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0f, 1.4f, -0.4f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.72f, 0.28f);
        light.range = 12f;
        light.intensity = 3f * LightBoost;
        light.shadows = LightShadows.None;
        var crystals = go.AddComponent<GolemCrystals>();
        SetRef(crystals, "glowLight", light);
        SetRef(crystals, "sprite", go.transform.Find("Sprite").GetComponent<SpriteRenderer>());
        SetRef(crystals, "glowSound", Sound("golem_glow"));
        SetRef(crystals, "dimSound", Sound("golem_dim"));
        SetRef(crystals, "clinkSound", Sound("clink"));
        return SavePrefab(go, "CrystalGolem");
    }

    // A "dirtheart" or "dirtshard" mound: the dirt, and the treasure buried under it (waiting, hidden, beside it).
    private static GameObject PlaceBuriedReward(string kind, SharedAssets assets, Transform parent, Vector3 pos, string persistentId, Transform decor)
    {
        var dirt = Place(assets.PropPrefabs[kind], parent, pos);
        var treasure = Place(assets.PropPrefabs[kind == "dirtheart" ? "heartpiece" : "starshard"], decor, pos);
        treasure.name += "Buried";
        foreach (var c in treasure.GetComponentsInChildren<Collectible>()) SetString(c, "persistentId", persistentId + "/buried");
        treasure.SetActive(false);
        SetRef(dirt.GetComponent<SoftDirt>(), "reward", treasure);
        return dirt;
    }
}
