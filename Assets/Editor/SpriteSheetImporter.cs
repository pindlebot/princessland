using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Imports a generated sprite sheet (Assets/Art/<Sheet>.png + <Sheet>.json) as pixel art and
// slices it into one Sprite per frame, which is what the Sprite Editor does by hand.
// Used for characters (CharacterSpriteBuilder) and spell effects (DungeonBuilder).
public static class SpriteSheetImporter
{
    // Mirrors the JSON written by Tools/sprite_common.py.
    [Serializable]
    public class Layout
    {
        public int frameSize;
        public int pixelsPerUnit;
        public string pivot;  // "bottom" (characters stand on it) or "center" (effects)
        public string action; // characters only: name of the Cast/Attack state
        public AnimInfo[] animations;
    }

    [Serializable]
    public class AnimInfo { public string name; public int row; public int frames; public int fps; public bool loop; }

    public class SpriteSheet
    {
        public Layout Layout;
        public Dictionary<string, Sprite> Sprites; // keyed "<Animation>_<frame>"

        public AnimInfo Anim(string name) => Layout.animations.First(a => a.name == name);
        public Sprite[] Frames(string anim) =>
            Enumerable.Range(0, Anim(anim).frames).Select(i => Sprites[$"{anim}_{i}"]).ToArray();
    }

    public static SpriteSheet Import(string sheet)
    {
        string path = $"Assets/Art/{sheet}.png";
        var layout = JsonUtility.FromJson<Layout>(File.ReadAllText($"Assets/Art/{sheet}.json"));
        Slice(path, layout);
        return new SpriteSheet
        {
            Layout = layout,
            Sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name),
        };
    }

    // A texture that is a single sprite (e.g. the blob shadow).
    public static Sprite ImportSingle(string path, int ppu)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        ConfigurePixelArt(importer, ppu);
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Slice(string path, Layout layout)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        ConfigurePixelArt(importer, layout.pixelsPerUnit);
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // Keep each sprite's ID stable across rebuilds so existing references survive.
        var existingIds = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);

        int f = layout.frameSize;
        int sheetHeight = (layout.animations.Max(a => a.row) + 1) * f;
        var alignment = layout.pivot == "center" ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;
        var rects = new List<SpriteRect>();
        foreach (var anim in layout.animations)
        {
            for (int i = 0; i < anim.frames; i++)
            {
                string name = $"{anim.name}_{i}";
                rects.Add(new SpriteRect
                {
                    name = name,
                    // Texture coordinates start at the bottom-left; the sheet's row 0 is the top.
                    rect = new Rect(i * f, sheetHeight - (anim.row + 1) * f, f, f),
                    alignment = alignment,
                    spriteID = existingIds.TryGetValue(name, out var id) ? id : GUID.Generate(),
                });
            }
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    // Pixel art needs Point filtering (no blur) and no compression (no color smearing).
    private static void ConfigurePixelArt(TextureImporter importer, int ppu)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
    }
}
