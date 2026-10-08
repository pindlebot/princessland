using UnityEditor;
using UnityEngine;

// The abilities each hero learns at the end of her skill path (SkillCatalog), added to the
// player prefabs. Each is a HeroAbility component (off until its skill is learned) plus,
// for the ones that leave something in the world, a prefab of its own.
// Art: Tools/make_ability_sprites.py.
public static partial class DungeonBuilder
{
    // The wizard: Flame Wave (slot 2) and Meteor (slot 3).
    private static void AddWizardAbilities(GameObject player, Projectile fireball)
    {
        var flameWave = player.AddComponent<FlameWave>();
        SetUpAbility(flameWave, "Flame Wave", "IconFlameWave", SkillCatalog.FlameWave, slot: 2, cooldown: 4f, manaCost: 20f, "cast_fire");
        SetRef(flameWave, "burstPrefab", ImpactOf(fireball));

        var meteor = player.AddComponent<Meteor>();
        SetUpAbility(meteor, "Meteor", "IconMeteor", SkillCatalog.Meteor, slot: 3, cooldown: 9f, manaCost: 30f, "cast_fire");
        SetRef(meteor, "strikePrefab", CreateMeteorStrikePrefab());
    }

    // The princess: Bubble Shield (slot 2) and Whirlpool (slot 3).
    private static void AddPrincessAbilities(GameObject player, Projectile tidalOrb)
    {
        var shield = player.AddComponent<BubbleShield>();
        SetUpAbility(shield, "Bubble Shield", "IconBubbleShield", SkillCatalog.BubbleShield, slot: 2, cooldown: 12f, manaCost: 15f, "cast_water");
        SetRef(shield, "bubble", CreateBubble(player.transform));
        SetRef(shield, "popPrefab", ImpactOf(tidalOrb));
        SetRef(shield, "blockSound", Sound("impact_water"));

        var whirlpool = player.AddComponent<Whirlpool>();
        SetUpAbility(whirlpool, "Whirlpool", "IconWhirlpool", SkillCatalog.Whirlpool, slot: 3, cooldown: 9f, manaCost: 25f, "cast_water");
        SetRef(whirlpool, "poolPrefab", CreateWhirlpoolPrefab(ImpactOf(tidalOrb)));
    }

    private static void SetUpAbility(HeroAbility ability, string name, string icon, string skillId, int slot, float cooldown, float manaCost, string sound)
    {
        SetString(ability, "abilityName", name);
        SetRef(ability, "icon", SpriteSheetImporter.ImportSingle($"Assets/Art/UI/{icon}.png", 16));
        SetString(ability, "skillId", skillId);
        SetInt(ability, "slot", slot);
        SetFloat(ability, "cooldown", cooldown);
        SetFloat(ability, "manaCost", manaCost);
        SetRef(ability, "useSound", Sound(sound));
    }

    // A spell's impact burst (FireballImpact, TidalOrbImpact), reused for the abilities' effects.
    private static GameObject ImpactOf(Projectile spell) =>
        (GameObject)new SerializedObject(spell).FindProperty("impactPrefab").objectReferenceValue;

    // A meteor: a warning circle on the floor and a big fireball that falls onto it.
    private static MeteorStrike CreateMeteorStrikePrefab()
    {
        var go = new GameObject("MeteorStrike");
        var strike = go.AddComponent<MeteorStrike>();

        // The Slime King's warning circle: this time it's the monsters who should step out of it.
        var warning = new GameObject("Warning").AddComponent<SpriteRenderer>();
        warning.sprite = SpriteSheetImporter.ImportSingle("Assets/Art/SlamWarning.png", 16);
        warning.transform.SetParent(go.transform, false);
        warning.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        warning.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // The rock: the Fireball's flying flames, twice the size, pointed along its fall.
        var rock = new GameObject("Rock");
        rock.transform.SetParent(go.transform, false);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(rock.transform, false);
        sprite.transform.localScale = Vector3.one * 2f;
        sprite.AddComponent<SpriteRenderer>();
        SetFlipbook(sprite.AddComponent<SpriteFlipbook>(), SpriteSheetImporter.Import("Fireball"), "Fly", destroyWhenDone: false);
        sprite.AddComponent<FaceTravelDirection>();
        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(rock.transform, false);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.1f);
        light.range = 7f;
        light.intensity = 3f;

        SetRef(strike, "warning", warning.transform);
        SetRef(strike, "rock", rock.transform);
        SetRef(strike, "impactPrefab", CreateImpactPrefab("Meteor", SpriteSheetImporter.Import("Meteor"), new Color(1f, 0.5f, 0.1f)));
        SetRef(strike, "impactSound", Sound("slam_land"));
        return SavePrefab<MeteorStrike>(go, "MeteorStrike");
    }

    // A swirling pool lying flat on the floor (the ability spawns it rotated 90° about x).
    private static WhirlpoolZone CreateWhirlpoolPrefab(GameObject splash)
    {
        var go = new GameObject("WhirlpoolZone");
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.color = new Color(1f, 1f, 1f, 0.85f);
        SetFlipbook(go.AddComponent<SpriteFlipbook>(), SpriteSheetImporter.Import("Whirlpool"), "Spin", destroyWhenDone: false);
        var zone = go.AddComponent<WhirlpoolZone>();
        SetRef(zone, "splashPrefab", splash);
        SetRef(zone, "splashSound", Sound("impact_water"));

        var glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0f, -1f); // the root lies flat, so local -z is up
        glow.type = LightType.Point;
        glow.color = new Color(0.3f, 0.95f, 0.9f);
        glow.range = 6f;
        glow.intensity = 2f;
        return SavePrefab<WhirlpoolZone>(go, "WhirlpoolZone");
    }

    // The shield bubble: a child of the princess, hidden until she uses it. Drawn after her
    // (sortingOrder 1) so it's always in front of her sprite.
    private static GameObject CreateBubble(Transform player)
    {
        var bubble = new GameObject("Bubble");
        bubble.transform.SetParent(player, false);
        bubble.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        var renderer = bubble.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 1;
        SetFlipbook(bubble.AddComponent<SpriteFlipbook>(), SpriteSheetImporter.Import("Bubble"), "Wobble", destroyWhenDone: false);
        bubble.AddComponent<Billboard>();
        bubble.SetActive(false);
        return bubble;
    }
}
