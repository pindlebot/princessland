using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The metroidvania gates (Phase 2 of the roadmap) and what hides behind them. Art: Tools/make_gate_sprites.py.
//
//   gap          legend "prop gap": a pit tile too wide to step over. The Bouncy Boots hop across (HopAbility).
//                A thought bubble with the boots and a "?" tells you to come back later (HintBubble).
//   bramble      legend "prop bramble": thorns across the way. Fire burns them, water makes them bloom (Bramble).
//   brazier      legend "prop brazier": a spell lights it (fire) or fills it (water). Light every one on a level and
//                an "item <id> hidden braziers" appears (Brazier, BossReward).
//   heartpiece   legend "prop heartpiece": walk into it. Four make a heart (Collectible).
//   starshard    legend "prop starshard": walk into it. For the wardrobe at home, later (Collectible).
public static partial class DungeonBuilder
{
    private static void CreateGatePrefabs(SharedAssets assets, Sprite shadow, GameObject sparkle)
    {
        var gate = SpriteSheetImporter.Import("GateProps");
        var props = assets.PropPrefabs;
        props["bramble"] = CreateBramblePrefab(gate, sparkle);
        props["brazier"] = CreateBrazierPrefab(gate, shadow, sparkle);

        var collectibles = SpriteSheetImporter.Import("Collectibles");
        props["heartpiece"] = CreateCollectiblePrefab(collectibles, "HeartPiece", Collectible.Kind.HeartPiece,
                                                      new Color(1f, 0.4f, 0.5f), sparkle, "skill_learn");
        props["starshard"] = CreateCollectiblePrefab(collectibles, "StarShard", Collectible.Kind.StarShard,
                                                     new Color(1f, 0.85f, 0.4f), sparkle, "lantern_get");

        var plague = SpriteSheetImporter.Import("PlagueCrystals");
        assets.PlagueCrystals = new[] { "Small", "Cluster", "Spire" }
            .Select(name => Scenic("Plague" + name, plague, name, null, null, 0f)).ToArray();

        assets.RoomEdge = CreateRoomEdgePrefab();
        CreateWorkshopPrefabs(assets, shadow, sparkle);

        var bubbles = SpriteSheetImporter.Import("HintBubbles");
        assets.HintBoots = CreateHintPrefab(bubbles, "Boots", "HintBoots", Abilities.BouncyBoots,
                                            "A gap! I can't jump that far... Maybe something bouncy would help?");
        assets.HintLantern = CreateHintPrefab(bubbles, "Lantern", "HintLantern", Abilities.FairyLantern,
                                              "It's too dark to see in there. Maybe a light would help?");
    }

    private static GameObject CreateBramblePrefab(SpriteSheetImporter.SpriteSheet sheet, GameObject sparkle)
    {
        var go = new GameObject("Bramble");
        var box = go.AddComponent<BoxCollider>(); // solid, and what a spell hits
        box.size = new Vector3(Tile, 2f, Tile);
        box.center = new Vector3(0f, 1f, 0f);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        var renderer = sprite.AddComponent<SpriteRenderer>();
        renderer.sprite = sheet.Frames("Bramble")[0];
        sprite.AddComponent<Billboard>();

        var bramble = go.AddComponent<Bramble>();
        SetRef(bramble, "sprite", renderer);
        SetRef(bramble, "solid", box);
        SetRefs(bramble, "burnFrames", sheet.Frames("Burn"));
        SetRefs(bramble, "bloomFrames", sheet.Frames("Bloom"));
        SetFloat(bramble, "burnFps", sheet.Anim("Burn").fps);
        SetFloat(bramble, "bloomFps", sheet.Anim("Bloom").fps);
        SetRef(bramble, "sparkle", sparkle);
        SetRef(bramble, "burnSound", Sound("bramble_burn"));
        SetRef(bramble, "bloomSound", Sound("bramble_bloom"));
        return SavePrefab(go, "Bramble");
    }

    private static GameObject CreateBrazierPrefab(SpriteSheetImporter.SpriteSheet sheet, Sprite shadow, GameObject sparkle)
    {
        var go = new GameObject("Brazier");
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.4f, 1.8f, 1.4f);
        box.center = new Vector3(0f, 0.9f, 0f);
        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        AddLoopingSprite(sprite, sheet, "BrazierUnlit");
        AddShadow(go, shadow, 1.6f);

        var glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        glow.type = LightType.Point;
        glow.range = 6f;
        glow.intensity = 1.8f * LightBoost;
        glow.shadows = LightShadows.None;
        glow.enabled = false;
        var flicker = glow.gameObject.AddComponent<FlickerLight>();
        SetFloat(flicker, "baseIntensity", 1.8f * LightBoost);
        SetFloat(flicker, "flickerAmount", 0.4f);

        var brazier = go.AddComponent<Brazier>();
        SetRef(brazier, "flipbook", sprite.GetComponent<SpriteFlipbook>());
        SetRefs(brazier, "unlitFrames", sheet.Frames("BrazierUnlit"));
        SetRefs(brazier, "fireFrames", sheet.Frames("BrazierFire"));
        SetRefs(brazier, "waterFrames", sheet.Frames("BrazierWater"));
        SetFloat(brazier, "fireFps", sheet.Anim("BrazierFire").fps);
        SetFloat(brazier, "waterFps", sheet.Anim("BrazierWater").fps);
        SetRef(brazier, "glow", glow);
        SetRef(brazier, "sparkle", sparkle);
        SetRef(brazier, "lightSound", Sound("brazier_light"));
        return SavePrefab(go, "Brazier");
    }

    private static GameObject CreateCollectiblePrefab(SpriteSheetImporter.SpriteSheet sheet, string anim, Collectible.Kind kind,
                                                      Color glowColor, GameObject sparkle, string sound)
    {
        var go = new GameObject(anim);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        AddLoopingSprite(visual, sheet, anim);

        var glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1f, 0f);
        glow.type = LightType.Point;
        glow.color = glowColor;
        glow.range = 3f;
        glow.intensity = 1.6f * LightBoost;
        glow.shadows = LightShadows.None;

        var collectible = go.AddComponent<Collectible>();
        SetInt(collectible, "kind", (int)kind);
        SetRef(collectible, "visual", visual.transform);
        SetRef(collectible, "sparkle", sparkle);
        SetRef(collectible, "collectSound", Sound(sound));
        return SavePrefab(go, anim);
    }

    // The "come back later" bubble over a gate: a picture of what you need, and a "?".
    private static GameObject CreateHintPrefab(SpriteSheetImporter.SpriteSheet sheet, string anim, string name, string ability, string message)
    {
        var go = new GameObject(name);
        var bubble = new GameObject("Bubble");
        bubble.transform.SetParent(go.transform, false);
        var renderer = bubble.AddComponent<SpriteRenderer>();
        renderer.sprite = sheet.Frames(anim)[0];
        renderer.sortingOrder = 30;
        bubble.AddComponent<Billboard>();

        var hint = go.AddComponent<HintBubble>();
        SetString(hint, "ability", ability);
        SetString(hint, "message", message);
        SetRef(hint, "bubble", renderer);
        SetRef(hint, "hintSound", Sound("hint"));
        return SavePrefab(go, name);
    }

    // ---------- Dad's Workshop: the secret room behind a fake wall in the dungeon ----------
    //   fakewall       legend "prop fakewall": looks like a wall, has no collider; walk through it (FakeWall)
    //   workshopdesk   a computer showing the Unity editor (read it: Effect.Read, sets found:workshop)
    //   workshopnote   a signed note on an easel. The words are Dad's: change them here.

    private static void CreateWorkshopPrefabs(SharedAssets assets, Sprite shadow, GameObject sparkle)
    {
        var sheet = SpriteSheetImporter.Import("WorkshopProps");
        var props = assets.PropPrefabs;
        props["workshopdesk"] = CreateFixture(sheet, "WorkshopDesk", new Vector3(2.8f, 1.5f, 1.4f), shadow, 3f,
            "Look at the computer",
            "A computer, glowing. On the screen is a picture of the Unity editor, with a little castle made of cubes in the middle. | " +
            "The project is called Tidecrown. A sticky note on the monitor says: \"TODO: more dragons.\" | " +
            "There's a mug of coffee beside the keyboard. It's still warm. Someone was working here very late at night!",
            HouseFixture.Effect.Read, "paper",
            fixture =>
            {
                SetString(fixture, "useFlag", "found:workshop");
                SetString(fixture, "useCounter", "workshop_reads");
            });
        AddGlow(props["workshopdesk"], new Vector3(0f, 1.6f, -0.4f), new Color(0.65f, 0.82f, 1f), 7f, 1.5f); // the screen's light
        props["workshopnote"] = CreateFixture(sheet, "WorkshopNote", new Vector3(1.6f, 1.6f, 1.0f), shadow, 1.8f,
            "Read the note",
            "\"To my favourite player: | " +
            "I built this whole world for you, one tile at a time. Every dragon, every gap, every secret. | " +
            "You found my workshop! Thank you for playing. I love you. Love, Dad\"",
            HouseFixture.Effect.Read, "paper",
            fixture => SetString(fixture, "useFlag", "found:workshop"));

        // The fake wall: a wall block and its cap, exactly like the real ones, but nothing solid.
        var wall = new GameObject("FakeWall");
        var block = Block("Wall", wall.transform, Vector3.up * WallHeight * 0.5f, new Vector3(Tile, WallHeight, Tile), assets.Materials["WallSide"]);
        Object.DestroyImmediate(block.GetComponent<Collider>());
        block.transform.localPosition = Vector3.up * WallHeight * 0.5f;
        AddCap(block, assets.Materials["WallTop"]);
        var zone = wall.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 1f, 0f);
        zone.size = new Vector3(Tile, 2f, Tile);
        var fake = wall.AddComponent<FakeWall>();
        SetRef(fake, "sparkle", sparkle);
        SetRef(fake, "foundSound", Sound("secret_found"));
        props["fakewall"] = SavePrefab(wall, "FakeWall");
    }

    // The opening at a room's edge: a trigger over the tile and a pulsing arrow on the floor pointing out of the room.
    private static GameObject CreateRoomEdgePrefab()
    {
        var sheet = SpriteSheetImporter.Import("EdgeArrow");
        var go = new GameObject("RoomEdge");
        var zone = go.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 1f, 0f);
        zone.size = new Vector3(Tile, 2f, Tile);

        var arrow = new GameObject("Arrow");
        arrow.transform.SetParent(go.transform, false);
        arrow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        arrow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // lying flat; its top points along the root's forward
        arrow.transform.localScale = Vector3.one * 1.4f;
        var renderer = arrow.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = -1;
        SetFlipbook(arrow.AddComponent<SpriteFlipbook>(), sheet, "Arrow", destroyWhenDone: false);

        go.AddComponent<RoomEdge>();
        return SavePrefab(go, "RoomEdge");
    }

    // Pots, sleepy trees, brambles, braziers and treasures remember being used by where they stand.
    private static void AssignPersistentIds(GameObject placed, string id)
    {
        foreach (var pot in placed.GetComponentsInChildren<BreakablePot>()) SetString(pot, "persistentId", id);
        foreach (var tree in placed.GetComponentsInChildren<SleepyTree>()) SetString(tree, "persistentId", id);
        foreach (var bramble in placed.GetComponentsInChildren<Bramble>()) SetString(bramble, "persistentId", id);
        foreach (var brazier in placed.GetComponentsInChildren<Brazier>()) SetString(brazier, "persistentId", id);
        foreach (var collectible in placed.GetComponentsInChildren<Collectible>()) SetString(collectible, "persistentId", id);
        foreach (var dirt in placed.GetComponentsInChildren<SoftDirt>()) SetString(dirt, "persistentId", id);
    }

    // Everything standing in a secret room (behind a fake wall) goes under a SecretRoom, which keeps it out of sight
    // until the fake wall has been walked through.
    private static void HideSecretRoom(MapFile file, Transform level, Transform decor)
    {
        var tiles = file.SecretTiles();
        if (tiles.Count == 0) return;
        var set = new HashSet<(int, int)>(tiles);
        var room = new GameObject("SecretRoom");
        var contents = new GameObject("Contents");
        contents.transform.SetParent(room.transform, false);
        SetRef(room.AddComponent<SecretRoom>(), "contents", contents);
        foreach (var parent in new[] { level, decor })
        {
            foreach (var child in parent.Cast<Transform>().ToList())
            {
                var tile = (Mathf.RoundToInt(child.position.x / Tile), file.Rows.Length - 1 - Mathf.RoundToInt(child.position.z / Tile));
                if (set.Contains(tile)) child.SetParent(contents.transform, true);
            }
        }
    }

    // The world is overrun with dark green crystals until the condition holds (the Castle Grounds, until you bring
    // the Amethyst home). CrystalPlague owns the crystals: they are its children, so it can shatter them.
    private static Transform CreatePlague(string condition)
    {
        var plague = new GameObject("Plague").AddComponent<CrystalPlague>();
        SetString(plague, "clearedWhen", condition);
        SetRef(plague, "clearSound", Sound("color_return"));
        return plague.transform;
    }

    // Now and then, on open ground: a little cluster most often, a tall spire rarely. Walk-through decoration
    // (no colliders), so the plague can never wall anything in. Hedges, water, doors and props are left alone.
    private static void PlantPlagueCrystal(Transform plague, SharedAssets assets, char tile, Vector3 pos, System.Random rng)
    {
        if (tile != '.' && tile != ',' && tile != ';') return;
        if (rng.Next(tile == '.' ? 6 : 9) != 0) return;
        int kind = rng.Next(10);
        var prefab = assets.PlagueCrystals[kind < 6 ? 0 : kind < 9 ? 1 : 2];
        var crystal = Place(prefab, plague, pos + new Vector3(rng.Next(-3, 4) * 0.1f, 0f, rng.Next(-3, 4) * 0.1f));
        crystal.transform.localScale = Vector3.one * (0.85f + rng.Next(0, 4) * 0.1f);
    }

    // ---------- Gaps ----------

    private static bool IsGapTile(MapFile file, char c) =>
        file.Legend.TryGetValue(c, out var e) && e.Kind == "prop" && e.Args.Length > 0 && e.Args[0] == "gap";

    // One tile of a gap: no floor, a dark pit one metre down (with its far walls showing, so it reads as
    // a hole rather than a painted square), an invisible wall on the Water layer to keep everyone out (spells
    // fly over it), and a "come back later" bubble.
    private static void BuildGap(Transform level, Transform decor, SharedAssets assets, LevelSpec spec, Vector3 pos, string gateId)
    {
        var gap = new GameObject("Gap");
        gap.transform.SetParent(level);
        gap.transform.position = pos;
        gap.AddComponent<Gap>();

        var floor = Block("PitFloor", gap.transform, pos + Vector3.down * 1.25f, new Vector3(Tile, 0.5f, Tile), assets.Materials["Pit"]);
        Object.DestroyImmediate(floor.GetComponent<Collider>());

        // The north and east inner walls are the ones the camera sees (the others face away from it).
        var wallMat = assets.Materials[spec.Theme == Theme.Outdoor ? "PitWallEarth" : "PitWallStone"];
        foreach (var (offset, yaw) in new[] { (new Vector3(0f, 0f, Tile / 2f - 0.02f), 0f), (new Vector3(Tile / 2f - 0.02f, 0f, 0f), 90f) })
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "PitWall";
            Object.DestroyImmediate(wall.GetComponent<Collider>());
            wall.transform.SetParent(gap.transform);
            wall.transform.position = pos + offset + Vector3.down * 0.5f;
            wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            wall.transform.localScale = new Vector3(Tile, 1f, 1f);
            wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            wall.isStatic = true;
        }

        var edge = new GameObject("Edge").AddComponent<BoxCollider>();
        edge.gameObject.layer = LevelMap.WaterRimLayer; // spells fly over it, and not even a swimmer may walk into the pit
        edge.transform.SetParent(gap.transform);
        edge.transform.position = pos + Vector3.up * 1.5f;
        edge.size = new Vector3(Tile, 3f, Tile);

        var hint = Place(assets.HintBoots, decor, pos + Vector3.up * 2.6f);
        SetString(hint.GetComponent<HintBubble>(), "gateId", gateId);
    }
}
