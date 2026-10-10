using UnityEditor;
using UnityEngine;

// The monsters that Hollow Farm and Mermaid Cove's captain add (their art is Tools/make_farm_monster_sprites.py
// and Tools/make_captain_sprites.py). A level picks which monster each of its E, L, J and M markers stands for
// with a "monsters:" header line (MapFile.cs), and MonsterPrefab turns the id into a prefab.
//
//   Gourdling       an angry little jack-o'-lantern that hops after you. Hollow Farm's pumpkin patch.
//   Strawman        a scarecrow gone bad, with a pitchfork that reaches a little further than a sword.
//   Pumpkin King    Hollow Farm's boss, in the graveyard: Ground Slam, a Volley of purple bolts, and a
//                   scatter of Gourdlings when he's hurt.
//   Captain         Mermaid Cove's final boss, on the beach below the sea cave: Ground Slam, a cannon
//   Grumblebeard    Barrage, and "all hands on deck!" (two more pirates) when he's hurt.
public static partial class DungeonBuilder
{
    // Shaped like the slime and the skeleton: a bit of health, a bit of XP.
    private static readonly EnemyStats GourdlingStats = new EnemyStats
    {
        Name = "Gourdling", Health = 2, Experience = 20, MinCoins = 2, MaxCoins = 4,
        MoveSpeed = 2.8f, AggroRange = 7f, AttackRange = 1.2f, AttackCooldown = 1.2f,
        HitSound = "slime_hit", DeathSound = "slime_death", AttackSound = "slime_attack",
    };

    private static readonly EnemyStats StrawmanStats = new EnemyStats
    {
        Name = "Strawman", Health = 3, Experience = 20, MinCoins = 3, MaxCoins = 5,
        MoveSpeed = 2.6f, AggroRange = 8f, AttackRange = 1.6f, AttackCooldown = 1.3f,
        HitSound = "enemy_hit", DeathSound = "enemy_death", AttackSound = "enemy_attack",
    };

    // 28 health, hits for 2, worth 300 XP (15x a Gourdling): most of the way to level 2 by himself.
    private static readonly EnemyStats PumpkinKingStats = new EnemyStats
    {
        Name = "PumpkinKing", Health = 28, Experience = 300, Damage = 2,
        MinCoins = 12, MaxCoins = 18, MinCoinValue = 2, MaxCoinValue = 4,
        MoveSpeed = 2.2f, AggroRange = 10f, AttackRange = 2.4f, AttackCooldown = 1.4f,
        HitSound = "slime_hit", DeathSound = "slime_death", AttackSound = "slime_attack",
    };

    // The last boss of the cove: 34 health, hits for 2, worth 400 XP (20x a pirate), and a pocketful of gold.
    private static readonly EnemyStats CaptainStats = new EnemyStats
    {
        Name = "PirateCaptain", Health = 34, Experience = 400, Damage = 2,
        MinCoins = 18, MaxCoins = 24, MinCoinValue = 2, MaxCoinValue = 5,
        MoveSpeed = 2.7f, AggroRange = 11f, AttackRange = 2.3f, AttackCooldown = 1.2f,
        HitSound = "pirate_hit", DeathSound = "pirate_death", AttackSound = "enemy_attack",
    };

    // The prefab for a monster id from a level's map (MapFile.MonsterIds).
    private static GameObject MonsterPrefab(SharedAssets assets, string id)
    {
        switch (id)
        {
            case "skeleton": return assets.Skeleton;
            case "slime": return assets.Slime;
            case "pirate": return assets.Pirate;
            case "slimeking": return assets.SlimeKing;
            case "gourdling": return assets.Gourdling;
            case "strawman": return assets.Strawman;
            case "pumpkinking": return assets.PumpkinKing;
            case "piratecaptain": return assets.PirateCaptain;
            case "sporepuff": return assets.SporePuff;
            case "mothermushroom": return assets.MotherMushroom;
            default: throw new System.Exception($"[DungeonBuilder] unknown monster '{id}'");
        }
    }

    private static void CreateMonsterPrefabs(SharedAssets assets, CoinPickup coin, GameObject defeatEffect)
    {
        assets.Gourdling = CreateEnemyPrefab(CharacterSpriteBuilder.Build("Gourdling"), GourdlingStats, coin, defeatEffect);
        assets.Strawman = CreateEnemyPrefab(CharacterSpriteBuilder.Build("Strawman"), StrawmanStats, coin, defeatEffect);
        assets.PumpkinKing = CreatePumpkinKingPrefab(coin, defeatEffect, assets.Gourdling);
        assets.PirateCaptain = CreateCaptainPrefab(coin, defeatEffect, assets.Pirate);
    }

    // ---------- Pumpkin King ----------

    private static GameObject CreatePumpkinKingPrefab(CoinPickup coin, GameObject defeatEffect, GameObject gourdling)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("PumpkinKing"), PumpkinKingStats, coin, defeatEffect);
        MakeBig(go, radius: 1.3f, height: 2.6f, center: 0.3f, shadowSize: 3f);

        var boss = AddBossAbilities(go, "The Pumpkin King", engageRange: 9f);
        SetString(boss, "engageMessage", "{0} rises from the graveyard!");
        SetString(boss, "splitMessage", "{0} shakes loose angry Gourdlings!");
        SetString(boss, "defeatMessage", "{0} is beaten! Hollow Farm is safe again!");
        SetBool(boss, "clearsLevel", true); // the farm has no exit crystal: beating him clears it
        SetFloat(boss, "slamCooldown", 7f);
        SetRef(boss, "minionPrefab", gourdling);
        SetInt(boss, "minionCount", 4);

        // His Volley: the same slow purple bolts the dark mermaids throw, five in a fan.
        SetRef(boss, "boltPrefab", AssetDatabase.LoadAssetAtPath<EnemyBolt>("Assets/Prefabs/DarkBolt.prefab") ?? CreateDarkBoltPrefab());
        SetInt(boss, "volleyBolts", 5);
        SetFloat(boss, "volleyArc", 60f);
        SetFloat(boss, "volleyCooldown", 8f);
        SetRef(boss, "volleySound", Sound("siren_cast"));
        return SavePrefab(go, "PumpkinKing");
    }

    // ---------- Captain Grumblebeard ----------

    private static GameObject CreateCaptainPrefab(CoinPickup coin, GameObject defeatEffect, GameObject pirate)
    {
        var go = BuildEnemy(CharacterSpriteBuilder.Build("PirateCaptain"), CaptainStats, coin, defeatEffect);
        MakeBig(go, radius: 0.8f, height: 2.4f, center: 0.2f, shadowSize: 2f);

        var boss = AddBossAbilities(go, "Captain Grumblebeard", engageRange: 10f);
        SetString(boss, "engageMessage", "{0}: \"Nobody touches me treasure!\"");
        SetString(boss, "splitMessage", "{0}: \"All hands on deck!\"");
        SetString(boss, "barrageMessage", "Cannons ready... FIRE!");
        SetString(boss, "defeatMessage", "{0} is beaten! The sea-spell is broken!");
        SetFloat(boss, "slamCooldown", 8f);
        SetRef(boss, "minionPrefab", pirate);
        SetInt(boss, "minionCount", 2);

        // His Barrage: cannonballs that burst in the same fiery flash as Aldric's Fireball.
        SetRef(boss, "barrageImpactPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FireballImpact.prefab"));
        SetRef(boss, "barrageSound", Sound("cannon"));
        SetInt(boss, "barrageShots", 4);
        SetFloat(boss, "barrageCooldown", 9f);
        return SavePrefab(go, "PirateCaptain");
    }

    // ---------- Shared by the bosses ----------

    // A bigger body than the normal 1 x 2 capsule, with its feet still on the floor, and a bigger shadow.
    private static void MakeBig(GameObject go, float radius, float height, float center, float shadowSize)
    {
        SetFloat(go.GetComponent<EnemyAI>(), "corpseLifetime", 1f); // a moment longer to savour the win
        var body = go.GetComponent<CharacterController>();
        body.radius = radius;
        body.height = height;
        body.center = new Vector3(0f, center, 0f);
        go.transform.Find("Shadow").GetComponent<SpriteRenderer>().sprite = ShadowSprite(shadowSize);
    }

    // BossAbilities with the Ground Slam every boss has: the red circle and the landing shockwave are the
    // Slime King's (SlamWarning and SlamShockwave, saved when it was built), and so are the sounds.
    private static BossAbilities AddBossAbilities(GameObject go, string bossName, float engageRange)
    {
        var boss = go.AddComponent<BossAbilities>();
        SetString(boss, "bossName", bossName);
        SetFloat(boss, "engageRange", engageRange);
        SetRef(boss, "roarSound", Sound("boss_roar"));
        SetRef(boss, "warningPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SlamWarning.prefab"));
        SetRef(boss, "shockwavePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SlamShockwave.prefab"));
        SetRef(boss, "windupSound", Sound("slam_windup"));
        SetRef(boss, "landSound", Sound("slam_land"));
        SetRef(boss, "summonSound", Sound("summon"));
        return boss;
    }
}
