using System.Collections.Generic;
using UnityEngine;

// Floor hazards: lava ('~') and spike traps ('^'). Both are walkable and hurt the hero
// (Hazard.cs does the hurting). Lava is a glowing, churning floor with bubbles and embers;
// a spike trap is an iron plate with nine spikes that slide up through it on a loop.
public static partial class DungeonBuilder
{
    // Spikes sit 9 texture pixels apart, exactly over the holes in SpikePlate.png (16 px per metre).
    private const float SpikeSpacing = 9f / 16f;

    private static void CreateHazardPrefabs(SharedAssets assets, SpriteSheetImporter.SpriteSheet props)
    {
        var decals = SpriteSheetImporter.Import("WaterDecals");
        assets.LavaBubble = CreateFlatDecal("LavaBubble", decals, "LavaBubble", 1f);
        assets.Ember = CreateEmberPrefab(props);
        assets.SpikeTrap = CreateSpikeTrapPrefab(assets.Materials);
    }

    // A spark drifting up off the lava: the fountain's magic mote, tinted orange.
    private static GameObject CreateEmberPrefab(SpriteSheetImporter.SpriteSheet props)
    {
        var ember = new GameObject("Ember");
        AddLoopingSprite(ember, props, "Mote");
        ember.GetComponent<SpriteRenderer>().color = new Color(1f, 0.55f, 0.2f);
        var drift = ember.AddComponent<AmbientWander>();
        SetFloat(drift, "radius", 0.5f);
        SetFloat(drift, "speed", 0.5f);
        SetFloat(drift, "height", 0.2f);
        SetFloat(drift, "bob", 0.05f);
        SetFloat(drift, "rise", 1.4f);
        return SavePrefab(ember, "Ember");
    }

    private static GameObject CreateSpikeTrapPrefab(Dictionary<string, Material> mats)
    {
        var go = new GameObject("SpikeTrap");

        // The iron plate is this tile's floor, so the trap brings its own.
        var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Plate";
        plate.transform.SetParent(go.transform, false);
        plate.transform.localPosition = Vector3.down * 0.25f;
        plate.transform.localScale = new Vector3(Tile, 0.5f, Tile);
        plate.GetComponent<Renderer>().sharedMaterial = mats["SpikePlate"];
        plate.isStatic = true;

        // Nine little pyramids (the castle roofs' mesh), hidden inside the plate until they rise.
        var spikes = new GameObject("Spikes").transform;
        spikes.SetParent(go.transform, false);
        var mesh = PyramidMesh();
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                var spike = new GameObject("Spike");
                spike.transform.SetParent(spikes, false);
                spike.transform.localPosition = new Vector3(x * SpikeSpacing, 0f, z * SpikeSpacing);
                spike.transform.localScale = new Vector3(0.32f, 0.95f, 0.32f);
                spike.AddComponent<MeshFilter>().sharedMesh = mesh;
                spike.AddComponent<MeshRenderer>().sharedMaterial = mats["Spike"];
            }
        }

        var hazard = go.AddComponent<Hazard>();
        SetInt(hazard, "kind", (int)Hazard.Kind.Spikes);
        SetRef(hazard, "spikes", spikes);
        SetRef(hazard, "riseSound", Sound("spikes"));
        return SavePrefab(go, "SpikeTrap");
    }

    // A lava tile: sunk a little below the floor, glowing (an emissive material, so it's bright
    // even in the dark dungeon) and slowly churning, with the odd bubble, ember and warm light.
    private static void BuildLava(Transform level, Transform decor, SharedAssets assets,
                                  int col, int row, Vector3 pos, System.Random rng)
    {
        var lava = Block("Lava", level, pos + Vector3.down * 0.3f, new Vector3(Tile, 0.5f, Tile), assets.Materials["Lava"]);
        lava.isStatic = false; // its texture drifts
        lava.AddComponent<WaterScroll>();
        SetRef(lava.AddComponent<Hazard>(), "hurtSound", Sound("sizzle"));

        if (rng.Next(2) == 0)
            Place(assets.LavaBubble, decor, pos + new Vector3(rng.Next(-5, 6) * 0.1f, -0.03f, rng.Next(-5, 6) * 0.1f));
        if (rng.Next(3) == 0)
            Place(assets.Ember, decor, pos + new Vector3(rng.Next(-5, 6) * 0.1f, 0f, rng.Next(-5, 6) * 0.1f));

        // A light on every third tile or so (in a diagonal pattern) is enough to light a pool.
        if ((col + row) % 3 == 0)
        {
            var light = new GameObject("LavaGlow").AddComponent<Light>();
            light.transform.SetParent(decor, false);
            light.transform.position = pos + Vector3.up;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.15f);
            light.range = 5f;
            light.shadows = LightShadows.None;
            var glow = light.gameObject.AddComponent<FlickerLight>(); // a slow, molten pulse
            SetFloat(glow, "baseIntensity", 1.5f * LightBoost);
            SetFloat(glow, "flickerAmount", 0.3f);
            SetFloat(glow, "speed", 1.2f);
        }
    }

    private static void PlaceSpikeTrap(Transform level, SharedAssets assets, int col, Vector3 pos)
    {
        var trap = Place(assets.SpikeTrap, level, pos);
        // Each column runs 0.4s behind the one to its west, so a row of traps ripples eastward
        // and you can follow the wave across.
        SetFloat(trap.GetComponent<Hazard>(), "phase", -col * 0.4f);
    }
}
