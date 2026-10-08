using UnityEditor;
using UnityEngine;

// The decorative castle in Level 0. It's built over the rectangle of 'K' tiles on the map
// from the same 2 x 1.2 x 2 stone blocks as the dungeon walls, stacked up, so the brick
// texture fits without stretching:
//   corners      towers, 4 blocks high, with pointed aquamarine roofs
//   other edges  curtain walls, 3 blocks high, with crenellations (merlons) on top
//   front middle the same wall with a wooden gate set into it
//   inside       a 3-tile keep, 5 blocks high, with a long roof and a flag
// It's solid (every block has a collider). The gate is a SceneDoor into the hero's home.
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

        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                int col = minC + i, row = minR + j;
                var basePos = new Vector3(col * Tile, 0f, (map.Length - 1 - row) * Tile);
                bool westOrEast = i == 0 || i == w - 1;
                bool northOrSouth = j == 0 || j == h - 1;

                if (westOrEast && northOrSouth) // corner tower
                {
                    Stack(castle, basePos, 4, mats);
                    Roof(castle, roof, basePos + Vector3.up * 4 * BlockHeight, Tile + 0.5f, Tile + 0.5f, 2.4f, mats);
                }
                else if (westOrEast || northOrSouth) // curtain wall
                {
                    Stack(castle, basePos, 3, mats);
                    Merlons(castle, basePos + Vector3.up * 3 * BlockHeight, mats);
                    if (j == h - 1 && i == middle)
                    {
                        Gate(castle, basePos, mats, gateDoor);
                        // Arriving back through the gate: one tile south of it, facing away from it.
                        if (gateDoor != null)
                            outside = SpawnPoint(MapFile.SpawnNameFor(gateDoor.TargetScene),
                                                 basePos + new Vector3(0f, 1f, -Tile), Quaternion.LookRotation(Vector3.back));
                    }
                }
                else if (j == 1 && Mathf.Abs(i - middle) <= 1) // keep
                {
                    Stack(castle, basePos, 5, mats);
                    if (i == middle)
                    {
                        float top = 5 * BlockHeight;
                        Roof(castle, roof, basePos + Vector3.up * top, 3 * Tile + 0.5f, Tile + 0.5f, 2.6f, mats);
                        Place(assets.Flag, castle, basePos + Vector3.up * (top + 2.3f));
                    }
                }
                // Anything else is open courtyard (paved floor, laid by BuildLevel).
            }
        }
        return outside;
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

    // Four small stone teeth on a wall top: the classic castle silhouette.
    private static void Merlons(Transform parent, Vector3 topPos, System.Collections.Generic.Dictionary<string, Material> mats)
    {
        foreach (var x in new[] { -0.62f, 0.62f })
            foreach (var z in new[] { -0.62f, 0.62f })
                Block("Merlon", parent, topPos + new Vector3(x, 0.25f, z), new Vector3(0.55f, 0.5f, 0.55f), mats["WallSide"]);
    }

    // A wooden gate standing just proud of the wall's south (camera-facing) face.
    // Press E at it to go inside: a SceneDoor to wherever the map's legend says (the House).
    private static void Gate(Transform parent, Vector3 basePos, System.Collections.Generic.Dictionary<string, Material> mats,
                             MapFile.Door gateDoor)
    {
        var gate = Block("Gate", parent, basePos + new Vector3(0f, 1f, -Tile / 2f - 0.08f), new Vector3(1.3f, 2f, 0.2f), mats["Gate"]);
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
