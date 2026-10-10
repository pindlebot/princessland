using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Frostpeak (Assets/Levels/Frost1-4.txt), the fifth region, and what came with it:
//   Ice Slime        a blue slime (marker E) that leaves a trail of slippery ice (IceTrail, IceZone)
//   Snow Imp         a small imp (marker L) that lobs snowballs from a distance
//   Snow Yeti        the mountain's boss (marker M): Ground Slam, lanes of rolling snowballs (SnowballLanes), and ice slimes
//                    when he's hurt, in an arena that's all ice. Beat him and the Rainbow Chalk (and the Sapphire) appear
//   Mr. Frost        a shivering snowman (legend "npc frost"): "A Scarf for Mr. Frost", a trade chain. Barnaby sells the yarn
//   Granny Purl      the knitting granny (legend "npc purl"): knits the scarf from the yarn
//   floor ice        legend "<symbol> = floor ice": slippery ground (PlayerController keeps your momentum)
//   chasm            legend "prop chasm": a pit too wide to hop. rainbowpost: legend "prop rainbowpost": with the Rainbow Chalk, press E and
//                    a bridge of rainbow planks is drawn to the post across the chasm (RainbowPost, RainbowBridge)
//   snowman, igloo, icespire, drift   scenery
// Header keys: "ground: snow", "walls: snow", "mood: snow". Art: Tools/make_frost_sprites.py. Music: music_frost.
public static partial class DungeonBuilder
{
    private static readonly EnemyStats IceSlimeStats = new EnemyStats
    {
        Name = "IceSlime", Health = 3, Experience = 24, MinCoins = 3, MaxCoins = 5,
        MoveSpeed = 2.3f, AggroRange = 8f, AttackRange = 1.2f, AttackCooldown = 1.3f,
        HitSound = "slime_hit", DeathSound = "slime_death", AttackSound = "slime_attack",
    };

    private static readonly EnemyStats SnowImpStats = new EnemyStats
    {
        Name = "SnowImp", Health = 2, Experience = 22, MinCoins = 2, MaxCoins = 5,
        MoveSpeed = 2.4f, AggroRange = 9f, AttackRange = 6f, AttackCooldown = 1.9f,
        HitSound = "imp_hit", DeathSound = "imp_hit", AttackSound = "snowball_throw",
    };

    // 44 health, hits for 2, worth 550 XP.
    private static readonly EnemyStats SnowYetiStats = new EnemyStats
    {
        Name = "SnowYeti", Health = 44, Experience = 550, Damage = 2,
        MinCoins = 22, MaxCoins = 28, MinCoinValue = 2, MaxCoinValue = 6,
        MoveSpeed = 2.2f, AggroRange = 11f, AttackRange = 3f, AttackCooldown = 1.5f,
        HitSound = "rock_hit", DeathSound = "boss_roar", AttackSound = "slam_land",
    };

    // ---------- Conversations ----------

    private static Talk[] MrFrostTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Mr. Frost", Sets = "met:Mr. Frost",
            Lines = new[]
            {
                N("B-b-b-brr! H-hello there! Mr. Frost, snowman, at your service. Pardon the chattering. It's been winter on Frostpeak for three hundred years."),
                H("Aren't snowmen supposed to like the cold?"),
                N("We like it c-cool! Not this cold! My poor carrot's gone numb. What I need is a scarf. A big, warm, woolly scarf!"),
                N("Granny Purl knits the finest scarves on the mountain. She's just by the campfire there. But she's all out of yarn, and so am I."),
                N("Barnaby Badger sells yarn at his stall in Hollyhock, by the castle. Fetch a ball of it, {hero}, take it to Granny Purl, and bring me the scarf. Brrr! I'll be right here. I can't feel my feet to leave."),
            },
        },
        new Talk
        {
            Requires = "has:warm_scarf", NotIf = "thanked:frost", Sets = "thanked:frost", TakeItem = "warm_scarf", GiveItem = "snow_hat",
            Lines = new[]
            {
                N("A SCARF! A red one! Oh, it's warm. It's WARM! I can feel my carrot again!"),
                N("You've made a snowman very happy, {hero}. Here, take this: a Snow Hat, the warmest hat on the mountain. It's got a pom-pom!"),
                N("And in return, a song! Ahem."),
                N("♪ Oh, I'm a little snowman, round and white and neat, with a carrot for a nose and a scarf to keep me sweet! ♪"),
                N("♪ And when the winter's over, I won't melt away, I'll sit here by the campfire, and I'll sing to you all day! ♪"),
                H("That was lovely."),
                N("Thank you! I wrote it myself. It took three hundred years."),
            },
        },
        new Talk
        {
            Requires = "cleared:Frost4", NotIf = "frost:cheered", Sets = "frost:cheered",
            Lines = new[]
            {
                N("Did you feel that? The whole mountain just let out its breath! The Snow Yeti's himself again!"),
                N("And he's left you the Rainbow Chalk. Draw with it on a rainbow post, and a bridge of colour appears across the chasm. There are chasms like that all over Gemhold, I hear."),
            },
        },
        new Talk
        {
            NotIf = "thanked:frost",
            Lines = new[] { N("Br-brr... a ball of yarn from Barnaby Badger in Hollyhock, then Granny Purl knits it, then the scarf's mine. I mean, ours. Mine. B-brr!") },
        },
        new Talk { SmallTalk = true, Lines = new[] { N("♪ Oh, I'm a little snowman, round and white and neat... ♪ Oh! I've forgotten the rest. Give me a minute. It'll come.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("What do you call a snowman in the summer?"), H("What?"), N("A PUDDLE! Hahaha! ...Please don't tell me that's what happens.") } },
        new Talk { SmallTalk = true, Requires = "thanked:frost", Lines = new[] { N("Warm as toast! Well. Warm as a snowman. Cool, but cosy.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("Careful on the ice, {hero}. You keep going after you stop. Like a sleepy cat on a polished floor.") } },
    };

    private static Talk[] PurlTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Granny Purl", Sets = "met:Granny Purl",
            Lines = new[]
            {
                N("Well, hello, dear! Granny Purl. I knit, you know. Scarves, hats, mittens, the odd tea cosy."),
                N("Mr. Frost over there is wanting a scarf, poor lamb. But I haven't a scrap of wool left. The Snow Yeti flung my whole basket off the mountain."),
                N("If you find me a ball of yarn, dear, I'll knit him the loveliest scarf you've ever seen. Barnaby Badger sells wool in Hollyhock, down at the castle."),
            },
        },
        new Talk
        {
            Requires = "has:ball_of_yarn", NotIf = "purl:knit", Sets = "purl:knit", TakeItem = "ball_of_yarn", GiveItem = "warm_scarf",
            Lines = new[]
            {
                N("A ball of red wool! Oh, you dear thing. Give it here."),
                N("Click-clack, click-clack... knit one, purl one... and a long loop for luck... and..."),
                N("There! A warm scarf, red and cream. Take it to Mr. Frost, quick, before he rattles to bits!"),
            },
        },
        new Talk
        {
            Requires = "cleared:Frost4", NotIf = "purl:cheered", Sets = "purl:cheered",
            Lines = new[]
            {
                N("It's gone ever so quiet up the mountain. You've beaten the Yeti! He's not wicked, you know. Just hungry for company."),
                N("If you've time, dear, I could knit you a hat. But you've a Snow Hat already, I expect. Good for you."),
            },
        },
        new Talk
        {
            Requires = "met:Mr. Frost", NotIf = "purl:knit",
            Lines = new[] { N("A ball of yarn, dear. Barnaby Badger's stall, down in Hollyhock by the castle. Then I'll knit away.") },
        },
        new Talk { SmallTalk = true, Lines = new[] { N("Knit one, purl one. That's how I got my name, you know. Granny Purl. It was Granny Plain before. Dull.") } },
        new Talk { SmallTalk = true, Lines = new[] { N("The secret of good knitting is patience. And a cat to chase the wool. I've no cat. Pity.") } },
    };

    // ---------- Prefabs ----------

    private static void CreateFrostPrefabs(SharedAssets assets, Sprite shadow, CoinPickup coin, GameObject sparkle)
    {
        var slime = BuildEnemy(CharacterSpriteBuilder.Build("IceSlime"), IceSlimeStats, coin, sparkle);
        SetRef(slime.AddComponent<IceTrail>(), "patchPrefab", CreateIcePatchPrefab());
        assets.IceSlime = SavePrefab(slime, "IceSlime");

        assets.SnowImp = CreateSnowImpPrefab(coin, sparkle);
        assets.SnowYeti = CreateYetiPrefab(coin, sparkle, assets.IceSlime);

        assets.MrFrost = CreateNpcPrefab("MrFrost", "MrFrost", "Mr. Frost", "Assets/Art/UI/PortraitMrFrost.png", "voice_frost",
                                         MrFrostTalks(), new Vector3(1.6f, 2.6f, 1.2f), shadow, 2.2f);
        assets.Purl = CreateNpcPrefab("Purl", "Purl", "Granny Purl", "Assets/Art/UI/PortraitPurl.png", "voice_purl",
                                      PurlTalks(), new Vector3(1.2f, 2f, 0.9f), shadow, 1.8f);

        var bubbles = SpriteSheetImporter.Import("HintBubbles");
        assets.HintChalk = CreateHintPrefab(bubbles, "Chalk", "HintChalk", Abilities.RainbowChalk,
                                            "A chasm too wide to cross! Maybe something to draw a bridge with?");

        var props = assets.PropPrefabs;
        var frost = SpriteSheetImporter.Import("FrostProps");
        props["rainbowpost"] = CreatePostPrefab(frost, shadow, assets);
        props["chasm"] = props["rainbowpost"]; // (only the map legend's kind matters: chasm tiles are built, not placed)
        props["snowman"] = CreateFixture(frost, "Snowman", new Vector3(1.4f, 2.4f, 1.2f), shadow, 2f, "Look at the snowman",
            "A snowman. A very ordinary one, with a carrot nose and two coal eyes. | " +
            "Somebody has stuck a note on him: \"NOT Mr. Frost. (Mr. Frost has a hat.)\" | " +
            "You say hello anyway. He doesn't say anything. He's very polite about it.",
            HouseFixture.Effect.Read, "paper", f => SetString(f, "useFlag", "found:snowman"));
        props["igloo"] = Scenic("Igloo", frost, "Igloo", new Vector3(3.4f, 2.4f, 2.6f), shadow, 3.4f);
        props["icespire"] = Scenic("IceSpire", frost, "IceSpire", new Vector3(1.4f, 2.6f, 1.2f), shadow, 2f);
        AddGlow(props["icespire"], new Vector3(0f, 1.3f, -0.3f), new Color(0.6f, 0.85f, 1f), 5f, 1.1f);
        props["drift"] = CreateFixture(frost, "Drift", new Vector3(2.2f, 1.2f, 1.2f), shadow, 2.4f, "Look at the snow drift",
            "A drift of snow with a ski pole stuck in it. | " +
            "A tiny sign, nailed to the pole, says: \"Dug here Tuesday. Found: snow.\" | You add a second line, in your head: \"Wednesday: still snow.\"",
            HouseFixture.Effect.Read, "paper", f => SetString(f, "useFlag", "found:drift"));
    }

    private static GameObject CreateIcePatchPrefab()
    {
        var sheet = SpriteSheetImporter.Import("IcePatch");
        var go = new GameObject("IcePatch");
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sheet.Frames("Patch")[0];
        sr.sortingOrder = -1;
        go.AddComponent<IceZone>();
        return SavePrefab(go, "IcePatch");
    }

    private static GameObject CreateSnowImpPrefab(CoinPickup coin, GameObject defeatEffect)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("SnowImp"), SnowImpStats, coin, defeatEffect);
        var hand = new GameObject("ThrowPoint").transform;
        hand.SetParent(go.transform, false);
        hand.localPosition = new Vector3(0f, 0.4f, 0f);
        var ai = go.GetComponent<EnemyAI>();
        SetRef(ai, "boltPrefab", AssetDatabase.LoadAssetAtPath<EnemyBolt>("Assets/Prefabs/SnowBolt.prefab")
                                 ?? CreateBoltPrefab("SnowBolt", new Color(0.8f, 0.9f, 1f)));
        SetRef(ai, "throwPoint", hand);
        return SavePrefab(go, "SnowImp");
    }

    private static GameObject CreateYetiPrefab(CoinPickup coin, GameObject defeatEffect, GameObject iceSlime)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("SnowYeti"), SnowYetiStats, coin, defeatEffect);
        MakeBig(go, radius: 1.5f, height: 3f, center: 0.4f, shadowSize: 3.6f);

        var boss = AddBossAbilities(go, "The Snow Yeti", engageRange: 10f);
        SetString(boss, "engageMessage", "{0} roars!");
        SetString(boss, "splitMessage", "{0} shakes loose Ice Slimes!");
        SetString(boss, "defeatMessage", "{0} is beaten! The mountain can breathe again!");
        SetBool(boss, "clearsLevel", true);
        SetFloat(boss, "slamCooldown", 8f);
        SetRef(boss, "minionPrefab", iceSlime);
        SetInt(boss, "minionCount", 3);

        var sheet = SpriteSheetImporter.Import("Snowball");
        var lanes = go.AddComponent<SnowballLanes>();
        SetRefs(lanes, "ballFrames", sheet.Frames("Roll"));
        SetRef(lanes, "rollSound", Sound("snowball_roll"));
        return SavePrefab(go, "SnowYeti");
    }

    private static GameObject CreatePostPrefab(SpriteSheetImporter.SpriteSheet sheet, Sprite shadow, SharedAssets assets)
    {
        // Not solid: a post stands at each end of its bridge, right where the planks start, so the hero has to be able to walk through it.
        var go = new GameObject("RainbowPost");
        AddStaticSprite(go, sheet.Frames("RainbowPost")[0]);
        AddShadow(go, shadow, 1.2f);
        var post = go.AddComponent<RainbowPost>();
        SetRef(post, "sprite", go.transform.Find("Sprite").GetComponent<SpriteRenderer>());
        SetRef(post, "plainSprite", sheet.Frames("RainbowPost")[0]);
        SetRef(post, "litSprite", sheet.Frames("RainbowPostLit")[0]);
        Place(assets.HintChalk, go.transform, go.transform.position + Vector3.up * 3.4f);
        return SavePrefab(go, "RainbowPost");
    }

    // ---------- Chasms and rainbow bridges ----------

    private static readonly Dictionary<(int col, int row), Collider> ChasmWalls = new Dictionary<(int, int), Collider>();
    private static readonly Dictionary<(int col, int row), GameObject> PostObjects = new Dictionary<(int, int), GameObject>();

    private static void ClearChasmBookkeeping()
    {
        ChasmWalls.Clear();
        PostObjects.Clear();
    }

    private static void RegisterPost(int col, int row, GameObject post) => PostObjects[(col, row)] = post;

    // One tile of a chasm: like a gap (a dark pit and an invisible wall to keep everyone out), but with no Gap on it, so the boots
    // can't hop it (chasms are wide anyway). A rainbow bridge removes its wall; its hint bubble asks for the Rainbow Chalk.
    private static void BuildChasm(Transform level, Transform decor, SharedAssets assets, LevelSpec spec, Vector3 pos, int col, int row, string gateId)
    {
        var chasm = new GameObject("Chasm");
        chasm.transform.SetParent(level);
        chasm.transform.position = pos;

        var floor = Block("PitFloor", chasm.transform, pos + Vector3.down * 1.25f, new Vector3(Tile, 0.5f, Tile), assets.Materials["Pit"]);
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        var wallMat = assets.Materials[spec.Theme == Theme.Outdoor ? "PitWallEarth" : "PitWallStone"];
        foreach (var (offset, yaw) in new[] { (new Vector3(0f, 0f, Tile / 2f - 0.02f), 0f), (new Vector3(Tile / 2f - 0.02f, 0f, 0f), 90f) })
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "PitWall";
            Object.DestroyImmediate(wall.GetComponent<Collider>());
            wall.transform.SetParent(chasm.transform);
            wall.transform.position = pos + offset + Vector3.down * 0.5f;
            wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            wall.transform.localScale = new Vector3(Tile, 1f, 1f);
            wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            wall.isStatic = true;
        }
        var edge = new GameObject("Edge").AddComponent<BoxCollider>();
        edge.gameObject.layer = LevelMap.WaterRimLayer; // even a swimmer can't walk into it
        edge.transform.SetParent(chasm.transform);
        edge.transform.position = pos + Vector3.up * 1.5f;
        edge.size = new Vector3(Tile, 3f, Tile);
        ChasmWalls[(col, row)] = edge;

        var hint = Place(assets.HintChalk, decor, pos + Vector3.up * 2.6f);
        SetString(hint.GetComponent<HintBubble>(), "gateId", gateId);
    }

    // For every pair of rainbow posts that face each other across a row (or column) of chasm tiles: a RainbowBridge holding a
    // plank for each chasm tile between them, and linked to both posts.
    private static void BuildRainbowBridges(MapFile file, Transform level, SharedAssets assets, string sceneName)
    {
        var done = new HashSet<string>();
        foreach (var ((col, row), postObject) in PostObjects.ToList())
        {
            foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int k = 1;
                var tiles = new List<(int, int)>();
                while (file.IsChasm(file.At(col + dc * k, row + dr * k))) { tiles.Add((col + dc * k, row + dr * k)); k++; }
                if (tiles.Count == 0) continue;
                var (oc, orow) = (col + dc * k, row + dr * k);
                if (!PostObjects.TryGetValue((oc, orow), out var other)) continue;
                string id = $"{sceneName}/bridge{System.Math.Min(col, oc)},{System.Math.Min(row, orow)}";
                if (!done.Add(id)) continue;

                var bridge = new GameObject("RainbowBridge");
                bridge.transform.SetParent(level);
                var component = bridge.AddComponent<RainbowBridge>();
                var planks = new List<GameObject>();
                foreach (var (tc, tr) in tiles)
                {
                    var at = new Vector3(tc * Tile, 0f, (file.Rows.Length - 1 - tr) * Tile);
                    var plank = new GameObject("Plank");
                    plank.transform.SetParent(bridge.transform);
                    plank.transform.position = at;
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
                    quad.transform.SetParent(plank.transform, false);
                    quad.transform.localPosition = Vector3.up * 0.01f;
                    quad.transform.localRotation = Quaternion.Euler(90f, dc != 0 ? 0f : 90f, 0f); // the stripes run along the bridge
                    quad.transform.localScale = new Vector3(Tile, Tile, 1f);
                    quad.GetComponent<Renderer>().sharedMaterial = assets.Materials["RainbowBridge"];
                    var floor = plank.AddComponent<BoxCollider>(); // something to stand on over the pit
                    floor.size = new Vector3(Tile, 0.2f, Tile);
                    floor.center = new Vector3(0f, -0.1f, 0f);
                    plank.SetActive(false);
                    planks.Add(plank);
                }
                SetString(component, "persistentId", id);
                SetRefs(component, "walls", tiles.Select(t => (Object)ChasmWalls[t]).ToArray());
                SetRefs(component, "planks", planks.Cast<Object>().ToArray());
                SetRef(component, "drawSound", Sound("chalk_draw"));
                SetRef(component, "sparkle", assets.Sparkle);
                SetRef(postObject.GetComponent<RainbowPost>(), "bridge", component);
                SetRef(other.GetComponent<RainbowPost>(), "bridge", component);
            }
        }
    }
}
