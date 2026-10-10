using UnityEditor;
using UnityEngine;

// The Whispering Woods (Assets/Levels/Woods1-4.txt) and the systems that came with it:
//   Spore Puff       a stationary puffball (marker E) that coughs spores when you come close
//   Mother Mushroom  the Woods' boss (marker M): Ground Slam, a Volley of spore bolts, and Spore Puffs
//                    when she's hurt. Beat her and the Fairy Lantern appears (BossReward)
//   Old Moss         the gardener (legend "npc oldmoss"): "Wake the Trees", and the way to the Grove
//   sleepy trees     legend "prop sleepytree": zap one and it wakes (SleepyTree)
//   pots             legend "prop pot": breakable (BreakablePot), anywhere
// Art: Tools/make_woods_sprites.py. Music: music_woods.
public static partial class DungeonBuilder
{
    private static readonly EnemyStats SporePuffStats = new EnemyStats
    {
        Name = "SporePuff", Health = 2, Experience = 20, MinCoins = 2, MaxCoins = 4,
        MoveSpeed = 0f, AggroRange = 6f, AttackRange = 2.6f, AttackCooldown = 1.6f,
        HitSound = "spore_hit", DeathSound = "slime_death", AttackSound = "spore_puff",
    };

    // 32 health, hits for 2, worth 400 XP like the other region bosses.
    private static readonly EnemyStats MotherMushroomStats = new EnemyStats
    {
        Name = "MotherMushroom", Health = 32, Experience = 400, Damage = 2,
        MinCoins = 16, MaxCoins = 22, MinCoinValue = 2, MaxCoinValue = 4,
        MoveSpeed = 1.8f, AggroRange = 11f, AttackRange = 3f, AttackCooldown = 1.5f,
        HitSound = "spore_hit", DeathSound = "slime_death", AttackSound = "spore_puff",
    };

    private static Talk[] MossTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Old Moss", Sets = "met:Old Moss",
            Lines = new[]
            {
                N("Mmm? Oh! A visitor. Pardon me, I was just watering the lupins. Old Moss, they call me. Gardener of the Whispering Woods."),
                H("The trees are all wearing nightcaps!"),
                N("Aye. The Grey Gloom's drizzle sent the whole wood to sleep, three hundred years ago. They've been snoring ever since."),
                N("Give four of the sleepy ones a good zap of magic, {hero}. Nicely, mind. They wake up ever so cheerful."),
                N("Wake four, and come and tell me. I might have something for you."),
            },
        },
        new Talk
        {
            Requires = "trees_woken>=" + QuestCatalog.SleepyTreeGoal, NotIf = "thanked:moss",
            Sets = "thanked:moss", Gold = 25, GiveItem = "clover_charm",
            Lines = new[]
            {
                N("Hear that? That's the sound of a wood waking up. Birdsong! I'd forgotten what it sounded like."),
                N("Here, a Lucky Clover Charm. It grew where the first tree woke. Wear it, and your magic will come back quicker."),
                N("Now, if you're brave: the big red mushroom in the Grove, east of the meadow, is Mother Mushroom. She's been guarding something precious since the Gloom came."),
                N("She isn't wicked, just very cross. Dodge her red circles, and mind the spores."),
            },
        },
        new Talk
        {
            Requires = "cleared:Woods4", NotIf = "moss:cheered", Sets = "moss:cheered",
            Lines = new[]
            {
                N("You did it! I felt the whole wood sigh. Mother Mushroom is herself again."),
                N("And the Fairy Lantern! It's yours, {hero}. It lights up the dark places. There's a hollow south of the meadow that's been dark ever since the Gloom."),
            },
        },
        new Talk
        {
            NotIf = "trees_woken>=" + QuestCatalog.SleepyTreeGoal,
            Lines = new[] { N("Four sleepy trees, dear. Look for the ones in the blue nightcaps, and give each a good zap of magic.") },
        },
        new Talk { SmallTalk = true, Lines = new[] { N("A wood's like a dragon: grumpy until it's had its nap, and twice as grumpy if you wake it early.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("Why did the mushroom get invited to every party?"), H("Why?"), N("Because he was a real FUNGI! Heh heh heh.") } },
        new Talk { SmallTalk = true, Requires = "found:toilet_frog", Lines = new[] { N("Is that a frog hat? Splendid. Frogs are the best gardeners. They eat the slugs.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("Pots! Folk leave them lying about the Woods. Break one and see what's inside. Gently, mind.") } },
    };

    private static void CreateWoodsPrefabs(SharedAssets assets, Sprite shadow, CoinPickup coin, GameObject sparkle)
    {
        assets.SporePuff = CreateEnemyPrefab(CharacterSpriteBuilder.Build("SporePuff"), SporePuffStats, coin, sparkle);
        SetBool(assets.SporePuff.GetComponent<EnemyAI>(), "stationary", true); // rooted to its spot; it only puffs
        assets.MotherMushroom = CreateMotherPrefab(coin, sparkle, assets.SporePuff);
        assets.OldMoss = CreateNpcPrefab("OldMoss", "OldMoss", "Old Moss", "Assets/Art/UI/PortraitOldMoss.png",
                                         "voice_moss", MossTalks(), new Vector3(1.2f, 2.2f, 1f), shadow, 1.8f);

        var props = assets.PropPrefabs;
        var woods = SpriteSheetImporter.Import("WoodsProps");
        props["giantmushroom"] = Scenic("GiantMushroom", woods, "GiantMushroom", new Vector3(1.4f, 2.4f, 1.4f), shadow, 2.4f);
        props["glowcaps"] = Scenic("Glowcaps", woods, "Glowcaps", null, null, 0f);
        AddGlow(props["glowcaps"], new Vector3(0f, 0.8f, 0f), new Color(0.45f, 0.8f, 1f), 5f, 1.4f);
        props["pot"] = CreatePotPrefab(assets, coin, shadow);
        props["sleepytree"] = CreateSleepyTreePrefab(shadow, sparkle);
    }

    // ---------- Mother Mushroom ----------

    private static GameObject CreateMotherPrefab(CoinPickup coin, GameObject defeatEffect, GameObject sporePuff)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("MotherMushroom"), MotherMushroomStats, coin, defeatEffect);
        MakeBig(go, radius: 1.3f, height: 2.8f, center: 0.4f, shadowSize: 3f);

        var boss = AddBossAbilities(go, "Mother Mushroom", engageRange: 9f);
        SetString(boss, "engageMessage", "{0} rises, very cross!");
        SetString(boss, "splitMessage", "{0} puffs out Spore Puffs!");
        SetString(boss, "volleyMessage", "Mind the spores!");
        SetString(boss, "defeatMessage", "{0} is beaten! The Woods can breathe again!");
        SetBool(boss, "clearsLevel", true); // no exit crystal in the Grove: beating her clears it
        SetFloat(boss, "slamCooldown", 7f);
        SetRef(boss, "minionPrefab", sporePuff);
        SetInt(boss, "minionCount", 3);

        // Her Volley: slow green spore bolts, five in a fan.
        SetRef(boss, "boltPrefab", AssetDatabase.LoadAssetAtPath<EnemyBolt>("Assets/Prefabs/SporeBolt.prefab")
                                   ?? CreateBoltPrefab("SporeBolt", new Color(0.6f, 1f, 0.4f)));
        SetInt(boss, "volleyBolts", 5);
        SetFloat(boss, "volleyArc", 70f);
        SetFloat(boss, "volleyCooldown", 8f);
        SetRef(boss, "volleySound", Sound("spore_puff"));
        return SavePrefab(go, "MotherMushroom");
    }

    // ---------- Pots and sleepy trees ----------

    private static GameObject CreatePotPrefab(SharedAssets assets, CoinPickup coin, Sprite shadow)
    {
        var go = new GameObject("Pot");
        var box = go.AddComponent<BoxCollider>(); // solid, and what a spell hits
        box.size = new Vector3(1.1f, 1.2f, 1.1f);
        box.center = new Vector3(0f, 0.6f, 0f);
        AddStaticSprite(go, SpriteSheetImporter.Import("Pot").Frames("Idle")[0]);
        AddShadow(go, shadow, 1.2f);

        var shards = new GameObject("PotShards");
        shards.AddComponent<SpriteRenderer>();
        SetFlipbook(shards.AddComponent<SpriteFlipbook>(), SpriteSheetImporter.Import("PotShards"), "Break", destroyWhenDone: true);
        shards.AddComponent<Billboard>();

        var pot = go.AddComponent<BreakablePot>();
        SetRef(pot, "coinPrefab", coin);
        SetRef(pot, "treatPrefab", assets.ItemPickups["healing_apple"]);
        SetRef(pot, "shatterEffect", SavePrefab(shards, "PotShards"));
        SetRef(pot, "breakSound", Sound("pot_break"));
        return SavePrefab(go, "Pot");
    }

    private static GameObject CreateSleepyTreePrefab(Sprite shadow, GameObject sparkle)
    {
        var sheet = SpriteSheetImporter.Import("SleepyTree");
        var go = new GameObject("SleepyTree");
        var box = go.AddComponent<BoxCollider>(); // the trunk
        box.size = new Vector3(1.6f, 3f, 1.6f);
        box.center = new Vector3(0f, 1.5f, 0f);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, sheet, "Sleep");
        AddShadow(go, shadow, 3f);

        var tree = go.AddComponent<SleepyTree>();
        SetRef(tree, "flipbook", sprite.GetComponent<SpriteFlipbook>());
        SetRefs(tree, "sleepFrames", sheet.Frames("Sleep"));
        SetRefs(tree, "awakeFrames", sheet.Frames("Awake"));
        SetFloat(tree, "sleepFps", sheet.Anim("Sleep").fps);
        SetFloat(tree, "awakeFps", sheet.Anim("Awake").fps);
        SetRef(tree, "sparkle", sparkle);
        SetRef(tree, "wakeSound", Sound("tree_wake"));
        return SavePrefab(go, "SleepyTree");
    }

    // A prize hidden until the boss falls ("item <id> hidden"): the pickup goes inside a BossReward.
    private static void PlaceHiddenReward(GameObject pickupPrefab, Transform parent, Vector3 pos, string persistentId, GameObject sparkle,
                                          bool byBraziers = false)
    {
        var holder = new GameObject("BossReward");
        holder.transform.SetParent(parent);
        holder.transform.position = pos;
        var pickup = Place(pickupPrefab, holder.transform, pos);
        var itemPickup = pickup.GetComponent<ItemPickup>();
        if (itemPickup != null) SetString(itemPickup, "persistentId", persistentId);
        else AssignPersistentIds(pickup, persistentId);
        var reward = holder.AddComponent<BossReward>();
        SetRef(reward, "reward", pickup);
        SetRef(reward, "sparkle", sparkle);
        SetRef(reward, "appearSound", Sound("lantern_get"));
        SetBool(reward, "byBraziers", byBraziers);
    }

    // Everything in the quest log's pictures: each NPC's portrait and a few small icons.
    private static QuestPictures CreateQuestPictures()
    {
        var book = LoadOrCreateAsset<QuestPictures>("Assets/Items/QuestPictures.asset");
        var entries = new (string key, string path)[]
        {
            ("npc:Coralie", "Assets/Art/UI/PortraitMermaid.png"), ("npc:Pearl", "Assets/Art/UI/PortraitPearl.png"),
            ("npc:Pippin", "Assets/Art/UI/PortraitPippin.png"), ("npc:Old Stitches", "Assets/Art/UI/PortraitStitches.png"),
            ("npc:Old Moss", "Assets/Art/UI/PortraitOldMoss.png"), ("npc:Digby", "Assets/Art/UI/PortraitDigby.png"), ("npc:Mr. Frost", "Assets/Art/UI/PortraitMrFrost.png"), ("npc:Granny Purl", "Assets/Art/UI/PortraitPurl.png"), ("npc:Captain Clamshell", "Assets/Art/UI/PortraitClamshell.png"), ("icon:fish", "Assets/Art/UI/IconFish.png"), ("icon:mole", "Assets/Art/UI/IconMole.png"), ("npc:Amethyra", "Assets/Art/UI/PortraitDragon.png"), ("icon:frog", "Assets/Art/UI/IconFrog.png"),
            ("icon:tree", "Assets/Art/UI/IconTree.png"), ("icon:monster", "Assets/Art/UI/IconMonster.png"),
        };
        var so = new SerializedObject(book);
        var list = so.FindProperty("entries");
        list.arraySize = entries.Length;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("key").stringValue = entries[i].key;
            e.FindPropertyRelative("sprite").objectReferenceValue = SpriteSheetImporter.ImportSingle(entries[i].path, 16);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return book;
    }
}
