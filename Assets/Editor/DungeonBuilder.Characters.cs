using UnityEditor;
using UnityEngine;

// Players, enemies and the spells they cast.
public static partial class DungeonBuilder
{
    // Everything that makes the two heroes different, in one place.
    private class HeroStats
    {
        public string PrefabName, DisplayName, Description, Portrait;
        public int Health;
        public float Mana, ManaRegen;
        public string Spell, SpellIcon, CastSound, ImpactSound;
        public float Cooldown, ManaCost;
        public Color SpellLight;
        public System.Action<GameObject, Projectile> AddAbilities; // the skill path's abilities (DungeonBuilder.Abilities.cs)
    }

    private static CharacterDefinition CreateWizard(CharacterSpriteBuilder.Result art, GameObject levelUpEffect, ItemDatabase items) => CreateHero(art, levelUpEffect, items, new HeroStats
    {
        PrefabName = "Player_Wizard",
        DisplayName = "Aldric the Wizard",
        Description = "Hurls blazing Fireballs.\nHealth 5 · Mana 50 · casts every 0.6s",
        Portrait = "PortraitWizard",
        Health = 5, Mana = 50f, ManaRegen = 8f,
        Spell = "Fireball", SpellIcon = "IconFireball", CastSound = "cast_fire", ImpactSound = "impact_fire",
        Cooldown = 0.6f, ManaCost = 10f,
        SpellLight = new Color(1f, 0.5f, 0.1f),
        AddAbilities = AddWizardAbilities,
    });

    private static CharacterDefinition CreatePrincess(CharacterSpriteBuilder.Result art, GameObject levelUpEffect, ItemDatabase items) => CreateHero(art, levelUpEffect, items, new HeroStats
    {
        PrefabName = "Player_Princess",
        DisplayName = "Princess Marina",
        Description = "Casts swift Tidal Orbs.\nHealth 6 · Mana 40 · casts every 0.45s",
        Portrait = "PortraitPrincess",
        Health = 6, Mana = 40f, ManaRegen = 9f,
        Spell = "TidalOrb", SpellIcon = "IconTidalOrb", CastSound = "cast_water", ImpactSound = "impact_water",
        Cooldown = 0.45f, ManaCost = 8f,
        SpellLight = new Color(0.3f, 0.95f, 0.9f),
        AddAbilities = AddPrincessAbilities,
    });

    // A hero = a player prefab + a CharacterDefinition asset pointing at it.
    private static CharacterDefinition CreateHero(CharacterSpriteBuilder.Result art, GameObject levelUpEffect, ItemDatabase items, HeroStats stats)
    {
        var projectile = CreateSpellPrefab(stats.Spell, stats.SpellLight, Sound(stats.ImpactSound));
        var prefab = CreatePlayerPrefab(art, projectile, stats, levelUpEffect, items);

        var hero = LoadOrCreateAsset<CharacterDefinition>($"Assets/Characters/{stats.PrefabName.Replace("Player_", "")}.asset");
        var so = new SerializedObject(hero);
        so.FindProperty("displayName").stringValue = stats.DisplayName;
        so.FindProperty("description").stringValue = stats.Description;
        so.FindProperty("portrait").objectReferenceValue = SpriteSheetImporter.ImportSingle($"Assets/Art/UI/{stats.Portrait}.png", 16);
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        return hero;
    }

    private static GameObject CreatePlayerPrefab(CharacterSpriteBuilder.Result art, Projectile projectile, HeroStats stats, GameObject levelUpEffect, ItemDatabase items)
    {
        var go = NewCharacter(stats.PrefabName, stats.Health, Sound("player_hurt"), Sound("player_death"));
        go.AddComponent<PlayerController>();
        var mana = go.AddComponent<Mana>();
        SetFloat(mana, "max", stats.Mana);
        SetFloat(mana, "regenPerSecond", stats.ManaRegen);
        go.AddComponent<PlayerInteractor>();
        SetRef(go.AddComponent<Inventory>(), "database", items); // the bag itself lives in GameSession
        var progression = go.AddComponent<PlayerProgression>(); // level-ups raise health and mana
        SetRef(progression, "levelUpSound", Sound("level_up"));
        SetRef(progression, "levelUpEffect", levelUpEffect);

        var hop = go.AddComponent<HopAbility>(); // does nothing until the Bouncy Boots are in the treasures tab
        SetRef(hop, "puffPrefab", levelUpEffect);
        SetRef(hop, "hopSound", Sound("hop"));
        SetRef(hop, "landSound", Sound("hop_land"));

        var swim = go.AddComponent<SwimAbility>(); // ...nor does the Bubble Charm (swimming)
        SetRef(swim, "ripplePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ripple.prefab"));
        SetRef(swim, "splashSound", Sound("splash"));
        go.AddComponent<PushAbility>(); // ...and neither do the Mole Mitts, until they're in the treasures tab

        var castPoint = new GameObject("CastPoint").transform;
        castPoint.SetParent(go.transform, false);
        castPoint.localPosition = new Vector3(0f, 0.2f, 0.9f);

        var ability = go.AddComponent<SpellAbility>();
        SetString(ability, "spellName", ObjectNames.NicifyVariableName(stats.Spell)); // "TidalOrb" -> "Tidal Orb"
        SetRef(ability, "icon", SpriteSheetImporter.ImportSingle($"Assets/Art/UI/{stats.SpellIcon}.png", 16));
        SetRef(ability, "projectilePrefab", projectile);
        SetRef(ability, "castPoint", castPoint);
        SetFloat(ability, "cooldown", stats.Cooldown);
        SetFloat(ability, "manaCost", stats.ManaCost);
        SetRef(ability, "castSound", Sound(stats.CastSound));
        stats.AddAbilities?.Invoke(go, projectile);

        // A dim "torch" that travels with the player for dungeon atmosphere.
        // LevelBootstrap switches it off outdoors.
        var torch = new GameObject("Torch").AddComponent<Light>();
        torch.transform.SetParent(go.transform, false);
        torch.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        torch.type = LightType.Point;
        torch.color = new Color(1f, 0.85f, 0.6f);
        torch.range = 10f;
        torch.intensity = 1.5f;

        AddSpriteVisuals(go, art);
        return SavePrefab(go, stats.PrefabName);
    }

    // Everything that makes the two enemy types different.
    private class EnemyStats
    {
        public string Name, HitSound, DeathSound, AttackSound;
        public int Health, Experience, MinCoins, MaxCoins, MinCoinValue = 1, MaxCoinValue = 3, Damage = 1;
        public float MoveSpeed, AggroRange, AttackRange, AttackCooldown;
    }

    private static readonly EnemyStats SkeletonStats = new EnemyStats
    {
        Name = "Skeleton", Health = 2, Experience = 15, MinCoins = 2, MaxCoins = 4,
        MoveSpeed = 3.2f, AggroRange = 8f, AttackRange = 1.3f, AttackCooldown = 1f,
        HitSound = "enemy_hit", DeathSound = "enemy_death", AttackSound = "enemy_attack",
    };

    // Tougher but slower, and worth more.
    private static readonly EnemyStats SlimeStats = new EnemyStats
    {
        Name = "Slime", Health = 3, Experience = 20, MinCoins = 3, MaxCoins = 5,
        MoveSpeed = 2.3f, AggroRange = 7f, AttackRange = 1.2f, AttackCooldown = 1.3f,
        HitSound = "slime_hit", DeathSound = "slime_death", AttackSound = "slime_attack",
    };

    private static GameObject CreateEnemyPrefab(CharacterSpriteBuilder.Result art, EnemyStats stats, CoinPickup coin, GameObject defeatEffect) =>
        SavePrefab(BuildEnemy(art, stats, coin, defeatEffect), stats.Name);

    // An enemy, not yet saved as a prefab (so the boss can add more to it first).
    private static GameObject BuildEnemy(CharacterSpriteBuilder.Result art, EnemyStats stats, CoinPickup coin, GameObject defeatEffect)
    {
        var go = NewCharacter(stats.Name, stats.Health, Sound(stats.HitSound), Sound(stats.DeathSound));
        var ai = go.AddComponent<EnemyAI>();
        SetRef(ai, "attackSound", Sound(stats.AttackSound));
        SetFloat(ai, "moveSpeed", stats.MoveSpeed);
        SetFloat(ai, "aggroRange", stats.AggroRange);
        SetFloat(ai, "attackRange", stats.AttackRange);
        SetFloat(ai, "attackCooldown", stats.AttackCooldown);
        SetInt(ai, "attackDamage", stats.Damage);
        SetRef(ai, "defeatEffect", defeatEffect); // vanishes in a friendly puff of stars
        SetRef(ai, "defeatSound", Sound("poof"));

        var loot = go.AddComponent<Loot>(); // experience and gold when it dies
        SetInt(loot, "experience", stats.Experience);
        SetRef(loot, "coinPrefab", coin);
        SetInt(loot, "minCoins", stats.MinCoins);
        SetInt(loot, "maxCoins", stats.MaxCoins);
        SetInt(loot, "minCoinValue", stats.MinCoinValue);
        SetInt(loot, "maxCoinValue", stats.MaxCoinValue);

        AddSpriteVisuals(go, art);
        return go;
    }

    // ---------- The boss ----------

    // The Slime King: 30 health, hits for 2, and worth 400 XP (20x a slime, ~27x a skeleton).
    private static readonly EnemyStats SlimeKingStats = new EnemyStats
    {
        Name = "SlimeKing", Health = 30, Experience = 400, Damage = 2,
        MinCoins = 15, MaxCoins = 20, MinCoinValue = 2, MaxCoinValue = 4,
        MoveSpeed = 2f, AggroRange = 10f, AttackRange = 2.4f, AttackCooldown = 1.4f,
        HitSound = "slime_hit", DeathSound = "slime_death", AttackSound = "slime_attack",
    };

    private static GameObject CreateSlimeKingPrefab(CharacterSpriteBuilder.Result art, CoinPickup coin, GameObject slimePrefab, GameObject defeatEffect)
    {
        var go = BuildEnemy(art, SlimeKingStats, coin, defeatEffect);
        SetFloat(go.GetComponent<EnemyAI>(), "corpseLifetime", 1f); // a moment longer to savour the win

        // A much bigger body than the normal 1 x 2 capsule, with its feet still on the floor.
        var body = go.GetComponent<CharacterController>();
        body.radius = 1.3f;
        body.height = 2.6f;
        body.center = new Vector3(0f, 0.3f, 0f);
        go.transform.Find("Shadow").GetComponent<SpriteRenderer>().sprite = ShadowSprite(3f);

        // The slam's telegraph: a flat red circle on the floor.
        var warning = new GameObject("SlamWarning").AddComponent<SpriteRenderer>();
        warning.sprite = SpriteSheetImporter.ImportSingle("Assets/Art/SlamWarning.png", 16);
        var warningPrefab = SavePrefab(warning.gameObject, "SlamWarning");

        // The landing: a purple goo ring spreading across the floor, with a flash.
        var shockwave = new GameObject("SlamShockwave"); // drawn 96px across, so placed at scale 1 like every sprite
        shockwave.AddComponent<SpriteRenderer>();
        SetFlipbook(shockwave.AddComponent<SpriteFlipbook>(), SpriteSheetImporter.Import("Shockwave"), "Impact", destroyWhenDone: true);
        var flash = new GameObject("Flash").AddComponent<Light>();
        flash.transform.SetParent(shockwave.transform, false);
        flash.type = LightType.Point;
        flash.color = new Color(0.7f, 0.4f, 1f);
        flash.range = 4f;
        flash.intensity = 4f;
        flash.gameObject.AddComponent<FadeOutLight>();
        var shockwavePrefab = SavePrefab(shockwave, "SlamShockwave");

        var boss = go.AddComponent<BossAbilities>();
        SetRef(boss, "roarSound", Sound("boss_roar"));
        SetRef(boss, "warningPrefab", warningPrefab);
        SetRef(boss, "shockwavePrefab", shockwavePrefab);
        SetRef(boss, "windupSound", Sound("slam_windup"));
        SetRef(boss, "landSound", Sound("slam_land"));
        SetRef(boss, "minionPrefab", slimePrefab);
        SetRef(boss, "summonSound", Sound("summon"));
        return SavePrefab(go, "SlimeKing");
    }

    // The root holds the gameplay components and the collider (centered 1m up, like a capsule).
    private static GameObject NewCharacter(string name, int maxHealth, AudioClip hurtSound, AudioClip deathSound)
    {
        var go = new GameObject(name);
        go.AddComponent<CharacterController>();
        var health = go.AddComponent<Health>();
        SetInt(health, "maxHealth", maxHealth);
        SetRef(health, "hurtSound", hurtSound);
        SetRef(health, "deathSound", deathSound);
        return go;
    }

    // Shared by the players and enemies: a camera-facing animated sprite, a blob shadow,
    // and the CharacterAnimator that drives the Animator from gameplay. The visuals live
    // on child objects so they can face the camera while the root turns to aim.
    private static void AddSpriteVisuals(GameObject go, CharacterSpriteBuilder.Result art)
    {
        // Sprite + Animator. Pivot is at the feet, so drop it 1m to the floor.
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.transform.localPosition = new Vector3(0f, -1f, 0f);
        var spriteRenderer = sprite.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = art.DefaultSprite;
        sprite.AddComponent<Animator>().runtimeAnimatorController = art.Controller;
        sprite.AddComponent<Billboard>();

        // Blob shadow lying flat on the floor (the root is 1m up), just above it to avoid z-fighting.
        AddShadow(go, art.Shadow, 1f, lift: -0.98f);

        var animator = go.AddComponent<CharacterAnimator>();
        SetRef(animator, "animator", sprite.GetComponent<Animator>());
        SetRef(animator, "spriteRenderer", spriteRenderer);
    }

    // ---------- Spells ----------

    // One projectile prefab per spell sheet (Assets/Art/<spell>.png from make_spell_sprites.py).
    private static Projectile CreateSpellPrefab(string spell, Color lightColor, AudioClip impactSound)
    {
        var sheet = SpriteSheetImporter.Import(spell);
        var impact = CreateImpactPrefab(spell, sheet, lightColor);

        // Root: the physics. A small trigger sphere plus a kinematic Rigidbody
        // (trigger events need a Rigidbody on at least one side).
        var go = new GameObject(spell);
        go.AddComponent<SphereCollider>().radius = 0.2f;
        go.GetComponent<SphereCollider>().isTrigger = true;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var projectile = go.AddComponent<Projectile>();
        SetRef(projectile, "impactPrefab", impact);
        SetRef(projectile, "impactSound", impactSound);
        SetInt(projectile, "element", (int)(spell == "TidalOrb" ? SpellElement.Water : SpellElement.Fire)); // brambles and braziers care

        // Child: the looping sprite, turned to point where it's flying.
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.AddComponent<SpriteRenderer>();
        SetFlipbook(sprite.AddComponent<SpriteFlipbook>(), sheet, "Fly", destroyWhenDone: false);
        sprite.AddComponent<FaceTravelDirection>();

        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.type = LightType.Point;
        light.color = lightColor;
        light.range = 5f;
        light.intensity = 3f;

        return SavePrefab<Projectile>(go, spell);
    }

    // The burst left behind where a projectile hits: plays once with a fading flash of
    // light, then deletes itself.
    private static GameObject CreateImpactPrefab(string spell, SpriteSheetImporter.SpriteSheet sheet, Color lightColor)
    {
        var go = new GameObject(spell + "Impact");
        go.AddComponent<SpriteRenderer>();
        SetFlipbook(go.AddComponent<SpriteFlipbook>(), sheet, "Impact", destroyWhenDone: true);
        go.AddComponent<Billboard>();

        var flash = new GameObject("Flash").AddComponent<Light>();
        flash.transform.SetParent(go.transform, false);
        flash.type = LightType.Point;
        flash.color = lightColor;
        flash.range = 7f;
        flash.intensity = 4f;
        flash.gameObject.AddComponent<FadeOutLight>();

        return SavePrefab(go, spell + "Impact");
    }
}
