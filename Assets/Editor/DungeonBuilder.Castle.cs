using UnityEditor;
using UnityEngine;

// The castle in Level 0, made to sit alongside Hollyhock's cathedral (same lavender stone, teal roofs,
// glowing stained glass). It's built over the rectangle of 'K' tiles on the map from the same
// 2 x 1.2 x 2 stone blocks as the dungeon walls, stacked up, so the brick texture fits without stretching:
//   corners      towers, 5 blocks high, with overhanging pointed teal roofs, a gold finial and arrow slits
//   other edges  curtain walls, 3 blocks high, with a crenellated parapet along the outer edge and arrow slits
//   front middle the same wall with a stone-framed wooden gate set into it, a banner either side
//   inside       a 3-tile keep, 5 blocks high, under a gabled roof like the cathedral's, with stained-glass
//                lancets and a rose window, and a flag on the ridge
// It's solid (every block has a collider; the windows and trim are thin panels without). The gate is a
// SceneDoor into the hero's home.
public static partial class DungeonBuilder
{
    private const float BlockHeight = WallHeight; // one course of castle wall

    // gateDoor (from the map's legend, "K = castle House") says where the gate leads.
    // Returns the arrival spot just outside the gate, e.g. "FromHouse" (null if the map has no castle).
    private static Transform BuildCastle(string[] map, Transform parent, SharedAssets assets, MapFile.Door gateDoor)
    {
        Transform outside = null;
        // Find the 'K' rectangle.
        int minC = int.MaxValue, maxC = -1, minR = int.MaxValue, maxR = -1;
        for (int r = 0; r < map.Length; r++)
            for (int c = 0; c < map[r].Length; c++)
                if (map[r][c] == 'K')
                {
                    minC = Mathf.Min(minC, c); maxC = Mathf.Max(maxC, c);
                    minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r);
                }
        if (maxC < 0) return null; // no castle on this map

        var castle = new GameObject("Castle").transform;
        castle.SetParent(parent);
        var mats = assets.Materials;
        var roof = PyramidMesh();
        int w = maxC - minC + 1, h = maxR - minR + 1;
        int middle = w / 2;
        const int towerCourses = 5, wallCourses = 3, keepCourses = 5;
        float towerTop = towerCourses * BlockHeight, wallTop = wallCourses * BlockHeight;

        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                int col = minC + i, row = minR + j;
                var basePos = new Vector3(col * Tile, 0f, (map.Length - 1 - row) * Tile);
                bool west = i == 0, east = i == w - 1, north = j == 0, south = j == h - 1;

                if ((west || east) && (north || south)) // corner tower
                {
                    Stack(castle, basePos, towerCourses, mats);
                    var apex = basePos + Vector3.up * (towerTop + 3.4f);
                    Roof(castle, roof, basePos + Vector3.up * towerTop, Tile + 0.9f, Tile + 0.9f, 3.4f, mats);
                    Finial(castle, apex, mats);
                    // Arrow slits on the two faces the camera sees.
                    Panel("Slit", castle, basePos + new Vector3(0f, 3.5f, -Tile / 2f), 0.34f, 1.1f, Vector3.back, mats["Belfry"]);
                    Panel("Slit", castle, basePos + new Vector3(-Tile / 2f, 3.5f, 0f), 0.34f, 1.1f, Vector3.left, mats["Belfry"]);
                }
                else if (west || east || north || south) // curtain wall
                {
                    Stack(castle, basePos, wallCourses, mats);
                    Parapet(castle, basePos + Vector3.up * wallTop, west, east, north, south, mats);
                    bool gate = south && i == middle;
                    if (gate)
                    {
                        Gate(castle, basePos, mats, gateDoor);
                        // Arriving back through the gate: one tile south of it, facing away from it.
                        if (gateDoor != null)
                            outside = SpawnPoint(MapFile.SpawnNameFor(gateDoor.TargetScene),
                                                 basePos + new Vector3(0f, 1f, -Tile), Quaternion.LookRotation(Vector3.back));
                    }
                    else if (south && Mathf.Abs(i - middle) == 1)
                    {
                        // A banner hangs either side of the gate.
                        Panel("Banner", castle, basePos + new Vector3(0f, 2.3f, -Tile / 2f), 0.8f, 1.9f, Vector3.back, mats["Banner"]);
                    }
                    else if (south && i % 2 == 0)
                        Panel("Slit", castle, basePos + new Vector3(0f, 2.4f, -Tile / 2f), 0.3f, 0.95f, Vector3.back, mats["Belfry"]);
                    else if (west && j % 2 == 0)
                        Panel("Slit", castle, basePos + new Vector3(-Tile / 2f, 2.4f, 0f), 0.3f, 0.95f, Vector3.left, mats["Belfry"]);
                }
                // Anything else is open courtyard (paved floor, laid by BuildLevel); the keep is built below.
            }
        }

        BuildKeep(castle, map, minC + middle - 1, minC + middle + 1, minR + 1, assets, keepCourses);
        return outside;
    }

    // The keep: three tiles of tall stone behind the gate, with a gabled roof and glowing stained glass.
    private static void BuildKeep(Transform castle, string[] map, int minCol, int maxCol, int row, SharedAssets assets, int courses)
    {
        var mats = assets.Materials;
        var f = new Footprint
        {
            MinCol = minCol, MaxCol = maxCol, MinRow = row, MaxRow = row,
            West = minCol * Tile - Tile / 2f, East = maxCol * Tile + Tile / 2f,
            South = (map.Length - 1 - row) * Tile - Tile / 2f, North = (map.Length - 1 - row) * Tile + Tile / 2f,
        };
        float top = courses * BlockHeight;
        Walls(castle, f, courses, mats["WallSide"]);
        const float roofHeight = 3.2f;
        GableRoof(castle, f, top, roofHeight, mats["Roof"], mats["WallSide"], ridgeAlongZ: false);
        Place(assets.Flag, castle, new Vector3(f.Center.x, top + roofHeight, f.Center.z));

        // Lancets either side of a rose window on the front, and one on the west end.
        for (float x = f.West + Tile / 2f; x < f.East; x += Tile)
        {
            if (Mathf.Abs(x - f.Center.x) < 0.5f)
                Panel("RoseWindow", castle, new Vector3(x, top - 1.9f, f.South), 1.5f, 1.5f, Vector3.back, mats["RoseWindow"]);
            else
                Panel("Lancet", castle, new Vector3(x, top - 1.9f, f.South), 0.75f, 2f, Vector3.back, mats["Lancet"]);
        }
        Panel("Lancet", castle, new Vector3(f.West, top - 1.9f, f.Center.z), 0.75f, 2f, Vector3.left, mats["Lancet"]);
    }

    // The crenellated parapet: two merlons per tile along each outer edge, leaving the inner side a walkway.
    private static void Parapet(Transform parent, Vector3 topPos, bool west, bool east, bool north, bool south,
                                System.Collections.Generic.Dictionary<string, Material> mats)
    {
        foreach (float along in new[] { -0.5f, 0.5f })
        {
            if (north || south)
                Block("Merlon", parent, topPos + new Vector3(along, 0.28f, (north ? 1f : -1f) * 0.75f), new Vector3(0.7f, 0.56f, 0.5f), mats["WallSide"]);
            if (west || east)
                Block("Merlon", parent, topPos + new Vector3((west ? -1f : 1f) * 0.75f, 0.28f, along), new Vector3(0.5f, 0.56f, 0.7f), mats["WallSide"]);
        }
    }

    // The gold star on a tower's point, like the cathedral spire's.
    private static void Finial(Transform parent, Vector3 apex, System.Collections.Generic.Dictionary<string, Material> mats)
    {
        var star = Block("Finial", parent, apex + Vector3.up * 0.2f, Vector3.one * 0.34f, mats["Gold"]);
        star.transform.rotation = Quaternion.Euler(0f, 45f, 45f);
        Object.DestroyImmediate(star.GetComponent<BoxCollider>());
    }

    // A column of wall blocks, capped on top.
    private static void Stack(Transform parent, Vector3 basePos, int blocks, System.Collections.Generic.Dictionary<string, Material> mats)
    {
        GameObject top = null;
        for (int k = 0; k < blocks; k++)
            top = Block("CastleBlock", parent, basePos + Vector3.up * BlockHeight * (k + 0.5f),
                        new Vector3(Tile, BlockHeight, Tile), mats["WallSide"]);
        AddCap(top, mats["WallTop"]);
    }

    // A wooden gate in a pale stone surround, standing just proud of the wall's south (camera-facing) face.
    // Press E at it to go inside: a SceneDoor to wherever the map's legend says (the House).
    private static void Gate(Transform parent, Vector3 basePos, System.Collections.Generic.Dictionary<string, Material> mats,
                             MapFile.Door gateDoor)
    {
        float face = basePos.z - Tile / 2f;
        Panel("GateSurround", parent, new Vector3(basePos.x, 1.45f, face), 1.9f, 2.9f, Vector3.back, mats["WallTop"]);
        Panel("GateLintel", parent, new Vector3(basePos.x, 3.0f, face - 0.04f), 2.1f, 0.4f, Vector3.back, mats["WallTop"]);
        var gate = Block("Gate", parent, basePos + new Vector3(0f, 1.1f, -Tile / 2f - 0.08f), new Vector3(1.4f, 2.2f, 0.2f), mats["Gate"]);
        if (gateDoor == null) return; // a castle without a legend entry is just scenery
        var door = gate.AddComponent<SceneDoor>();
        SetString(door, "prompt", "Enter the castle");
        SetString(door, "targetScene", gateDoor.TargetScene);
        SetString(door, "targetSpawn", gateDoor.TargetSpawn);
        SetRef(door, "openSound", Sound("door_open"));
    }

    private static void Roof(Transform parent, Mesh mesh, Vector3 basePos, float width, float depth, float height,
                             System.Collections.Generic.Dictionary<string, Material> mats)
    {
        var go = new GameObject("Roof");
        go.transform.SetParent(parent);
        go.transform.position = basePos;
        go.transform.localScale = new Vector3(width, height, depth);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mats["Roof"];
        go.isStatic = true;
    }

    // A four-sided pyramid (base 1x1 at y=0, point at y=1), built by hand: a mesh is just
    // a list of corner positions (vertices), triangles that join them, and texture
    // coordinates (UVs). Each face gets its own three vertices so it's lit as a flat surface.
    private static Mesh PyramidMesh()
    {
        const string path = "Assets/Meshes/Pyramid.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;

        var apex = new Vector3(0f, 1f, 0f);
        var corners = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f),
        };
        var vertices = new Vector3[12];
        var uvs = new Vector2[12];
        var triangles = new int[12];
        for (int f = 0; f < 4; f++)
        {
            // Clockwise as seen from outside, which is the side Unity draws.
            vertices[f * 3] = corners[f];
            vertices[f * 3 + 1] = apex;
            vertices[f * 3 + 2] = corners[(f + 1) % 4];
            uvs[f * 3] = new Vector2(0f, 0f);
            uvs[f * 3 + 1] = new Vector2(0.5f, 1f);
            uvs[f * 3 + 2] = new Vector2(1f, 0f);
            for (int k = 0; k < 3; k++) triangles[f * 3 + k] = f * 3 + k;
        }

        mesh = new Mesh { name = "Pyramid", vertices = vertices, uv = uvs, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
            AssetDatabase.CreateFolder("Assets", "Meshes");
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
