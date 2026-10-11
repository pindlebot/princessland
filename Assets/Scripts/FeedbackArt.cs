using System;
using System.Collections.Generic;
using UnityEngine;

// Small pictures drawn in code (no import step, so they need no scene rebuild): the failed-action icons the HUD
// shows, the pops over a boss when a hit lands or bounces off, and the patterns on the floor warnings.
// Everything is drawn in flat colours with a plum outline, like the rest of the game, and nothing relies on
// colour alone: each warning has its own *shape* (rings, stripes, chevrons, lanes) that still reads in grey.
public static class FeedbackArt
{
    public static readonly Color32 Ink = new Color32(74, 37, 69, 255);        // the HUD's --ink
    public static readonly Color32 Honey = new Color32(250, 214, 120, 255);   // --honey-light
    public static readonly Color32 Danger = new Color32(255, 84, 70, 255);
    public static readonly Color32 Cream = new Color32(255, 246, 224, 255);

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // ---------- A tiny pixel canvas ----------

    private sealed class Canvas
    {
        public readonly int W, H;
        public readonly Color32[] Px;

        public Canvas(int w, int h)
        {
            W = w;
            H = h;
            Px = new Color32[w * h];
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x >= 0 && y >= 0 && x < W && y < H) Px[y * W + x] = c;
        }

        public Color32 Get(int x, int y) => x >= 0 && y >= 0 && x < W && y < H ? Px[y * W + x] : default;

        // Paint every pixel whose centre satisfies `inside`.
        public void Paint(Func<float, float, bool> inside, Color32 c)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (inside(x + 0.5f, y + 0.5f)) Px[y * W + x] = c;
        }

        public void Disc(float cx, float cy, float r, Color32 c) =>
            Paint((x, y) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r, c);

        public void Ring(float cx, float cy, float r0, float r1, Color32 c) =>
            Paint((x, y) =>
            {
                float d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                return d2 >= r0 * r0 && d2 <= r1 * r1;
            }, c);

        public void Rect(float x0, float y0, float x1, float y1, Color32 c) =>
            Paint((x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1, c);

        // A thick line segment.
        public void Line(float x0, float y0, float x1, float y1, float thickness, Color32 c) =>
            Paint((x, y) =>
            {
                float dx = x1 - x0, dy = y1 - y0;
                float len2 = dx * dx + dy * dy;
                float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(((x - x0) * dx + (y - y0) * dy) / len2);
                float px = x0 + t * dx - x, py = y0 + t * dy - y;
                return px * px + py * py <= thickness * thickness / 4f;
            }, c);

        // Put a 1px outline round everything that's drawn.
        public void Outline(Color32 ink)
        {
            var copy = (Color32[])Px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a != 0) continue;
                    if (Opaque(copy, x - 1, y) || Opaque(copy, x + 1, y) || Opaque(copy, x, y - 1) || Opaque(copy, x, y + 1))
                        Px[y * W + x] = ink;
                }
        }

        private bool Opaque(Color32[] from, int x, int y) => x >= 0 && y >= 0 && x < W && y < H && from[y * W + x].a != 0;

        public Sprite ToSprite(float pixelsPerUnit, Vector2 pivot, string name, bool fullRect = false)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name,
            };
            tex.SetPixels32(Px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), pivot, pixelsPerUnit, 0,
                                 fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight);
        }
    }

    private static Sprite Cached(string key, Func<Sprite> make)
    {
        if (!cache.TryGetValue(key, out var sprite) || sprite == null)
            cache[key] = sprite = make();
        return sprite;
    }

    // ---------- Failed-action icons (24 x 24) ----------

    // The picture for each way an action can fail. The HUD lays the same little red "no" badge (NoBadge) over every
    // one, so the family reads as "that didn't work" even before you read the picture.
    public static Sprite FailIcon(FailReason reason) => Cached("fail:" + reason, () =>
    {
        var c = new Canvas(24, 24);
        switch (reason)
        {
            case FailReason.BagFull: DrawBag(c); break;
            case FailReason.LowMana: DrawDroplet(c); break;
            case FailReason.MissingTool: DrawGear(c); break;
            case FailReason.Locked: DrawLock(c); break;
            case FailReason.ShellClosed: DrawShield(c); break;
            default: DrawGear(c); break;
        }
        c.Outline(Ink);
        return c.ToSprite(24f, new Vector2(0.5f, 0.5f), "Fail" + reason);
    });

    // A red disc with a white cross, for the corner of any failed-action picture (a tool's own icon included).
    public static Sprite NoBadge() => Cached("nobadge", () =>
    {
        var c = new Canvas(12, 12);
        c.Disc(6f, 6f, 5.2f, new Color32(214, 56, 56, 255));
        c.Line(3.6f, 3.6f, 8.4f, 8.4f, 1.8f, Cream);
        c.Line(3.6f, 8.4f, 8.4f, 3.6f, 1.8f, Cream);
        c.Outline(Ink);
        return c.ToSprite(12f, new Vector2(0.5f, 0.5f), "NoBadge");
    });

    private static void DrawBag(Canvas c)
    {
        var tan = new Color32(226, 172, 100, 255);
        var dark = new Color32(160, 98, 52, 255);
        c.Disc(11f, 9.5f, 7.5f, tan);
        c.Rect(8f, 14f, 14f, 18f, tan);
        c.Line(8f, 17f, 5.5f, 21f, 2f, tan);
        c.Line(14f, 17f, 16.5f, 21f, 2f, tan);
        c.Rect(7.5f, 15f, 14.5f, 16.6f, dark);   // the tie
        c.Disc(11f, 8f, 1.4f, dark);
    }

    private static void DrawDroplet(Canvas c)
    {
        var blue = new Color32(150, 170, 215, 255);
        c.Disc(11f, 8f, 6.5f, blue);
        c.Paint((x, y) => y >= 8f && y <= 20f && Mathf.Abs(x - 11f) <= (20f - y) * 0.55f, blue);
        c.Disc(8.6f, 7.6f, 1.5f, Cream);                   // a highlight: it's empty, not broken
    }

    private static void DrawGear(Canvas c)
    {
        var steel = new Color32(176, 182, 200, 255);
        c.Disc(11.5f, 12f, 6.2f, steel);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            c.Line(11.5f + Mathf.Cos(a) * 5f, 12f + Mathf.Sin(a) * 5f, 11.5f + Mathf.Cos(a) * 8.6f, 12f + Mathf.Sin(a) * 8.6f, 3f, steel);
        }
        c.Disc(11.5f, 12f, 2.6f, default);
    }

    private static void DrawLock(Canvas c)
    {
        var gold = new Color32(244, 194, 84, 255);
        c.Ring(11f, 13f, 3.4f, 5.8f, new Color32(190, 196, 210, 255));       // the shackle
        c.Rect(5f, 13f, 17f, 14f, default);
        c.Rect(4.5f, 3f, 17.5f, 12.5f, gold);                              // the body
        c.Disc(11f, 8.4f, 1.7f, Ink);
        c.Rect(10.2f, 4.8f, 11.8f, 8.2f, Ink);                              // the keyhole
    }

    private static void DrawShield(Canvas c)
    {
        var shell = new Color32(238, 132, 96, 255);
        // A scallop shell: a fan with ridges.
        c.Paint((x, y) => y >= 4f && (x - 11f) * (x - 11f) + (y - 4f) * (y - 4f) <= 15f * 15f * 0.78f, shell);
        c.Rect(8f, 2f, 14f, 5f, shell);
        for (int i = -2; i <= 2; i++)
        {
            float a = Mathf.PI / 2f + i * 0.5f;
            c.Line(11f, 4f, 11f + Mathf.Cos(a) * 11f, 4f + Mathf.Sin(a) * 11f, 1.2f, Cream);
        }
    }

    // ---------- Hit pops (world space, 24 x 24 art pixels) ----------

    // A gold starburst: "that hurt him" (his weak spot was open).
    public static Sprite ImpactBurst() => Cached("impact", () =>
    {
        var c = new Canvas(24, 24);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            float len = i % 2 == 0 ? 11f : 7f;
            c.Line(12f, 12f, 12f + Mathf.Cos(a) * len, 12f + Mathf.Sin(a) * len, i % 2 == 0 ? 3.2f : 2.2f, Honey);
        }
        c.Disc(12f, 12f, 4.6f, Cream);
        c.Outline(Ink);
        return c.ToSprite(ArtStyle.PixelsPerUnit, new Vector2(0.5f, 0.5f), "ImpactBurst");
    });

    // A little grey shield with a slash across it: "it bounced off".
    public static Sprite DeflectShield() => Cached("deflect", () =>
    {
        var c = new Canvas(24, 24);
        var steel = new Color32(190, 198, 218, 255);
        c.Paint((x, y) => y >= 8f && y <= 21f && Mathf.Abs(x - 12f) <= 8f, steel);
        c.Paint((x, y) => y < 8f && y >= 1f && Mathf.Abs(x - 12f) <= (y - 1f) * 8f / 7f, steel);
        c.Rect(11f, 5f, 13f, 19f, Cream);                    // a cross on the shield
        c.Rect(6f, 12.5f, 18f, 14.5f, Cream);
        c.Line(3f, 21f, 21f, 3f, 2f, Danger);                // the "no" slash
        c.Outline(Ink);
        return c.ToSprite(ArtStyle.PixelsPerUnit, new Vector2(0.5f, 0.5f), "DeflectShield");
    });

    // ---------- Floor warnings ----------

    // A ring for the edge of a danger circle, drawn at 1 unit radius. Chunky dashes (not a continuous line), so
    // the shape is distinctive on its own and survives snow, water and spell effects underneath it.
    public static Sprite DashedRing() => Cached("ring", () =>
    {
        const int size = 128;
        var c = new Canvas(size, size);
        float mid = size / 2f;
        c.Paint((x, y) =>
        {
            float dx = x - mid, dy = y - mid;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r < mid - 7f || r > mid - 1f) return false;
            float a = Mathf.Atan2(dy, dx) + Mathf.PI;                 // 0 .. 2pi
            return Mathf.FloorToInt(a / (2f * Mathf.PI) * 24f) % 2 == 0;
        }, Danger);
        c.Paint((x, y) =>
        {
            float dx = x - mid, dy = y - mid;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r < mid - 7f || r > mid - 1f) return false;
            float a = Mathf.Atan2(dy, dx) + Mathf.PI;
            return Mathf.FloorToInt(a / (2f * Mathf.PI) * 24f) % 2 == 1;
        }, Cream);
        c.Outline(Ink);
        return c.ToSprite(size / 2f, new Vector2(0.5f, 0.5f), "DashedRing"); // 2 units across at scale 1
    });

    // Diagonal stripes inside a disc (1 unit radius at scale 1): a pattern, not just a tint.
    public static Sprite StripedDisc() => Cached("disc", () =>
    {
        const int size = 128;
        var c = new Canvas(size, size);
        float mid = size / 2f;
        c.Paint((x, y) =>
        {
            float dx = x - mid, dy = y - mid;
            if (dx * dx + dy * dy > (mid - 8f) * (mid - 8f)) return false;
            return Mathf.FloorToInt((x + y) / 10f) % 2 == 0;
        }, new Color32(255, 84, 70, 120));
        return c.ToSprite(size / 2f, new Vector2(0.5f, 0.5f), "StripedDisc");
    });

    // A tiling strip of chevrons for a lane or a tide band: the points show which way it will travel (+x),
    // and the plum rails along both edges mark exactly where the danger ends. 32 x 32 px = 2 x 2 units.
    public static Sprite Chevrons() => Cached("chevrons", () =>
    {
        const int size = 32;
        var c = new Canvas(size, size);
        c.Rect(0, 0, size, size, new Color32(255, 84, 70, 70));
        for (int k = 0; k < 2; k++)
        {
            float x0 = 4f + k * 16f;
            c.Line(x0, 3f, x0 + 8f, 16f, 3.2f, new Color32(255, 214, 120, 230));
            c.Line(x0, 29f, x0 + 8f, 16f, 3.2f, new Color32(255, 214, 120, 230));
        }
        c.Rect(0, 0, size, 2.5f, Ink);
        c.Rect(0, size - 2.5f, size, size, Ink);
        return c.ToSprite(16f, new Vector2(0.5f, 0.5f), "Chevrons", fullRect: true);
    });

    // A fan on the floor for a volley: the apex at the left edge, `bolts` lanes spreading to the right, 1 unit long.
    // The lanes are solid; the gaps between them are where it's safe to stand.
    public static Sprite Fan(int bolts, float arcDegrees) => Cached($"fan:{bolts}:{arcDegrees:0}", () =>
    {
        const int length = 128;
        float half = Mathf.Clamp(arcDegrees, 10f, 140f) * 0.5f * Mathf.Deg2Rad;
        int height = Mathf.CeilToInt(2f * length * Mathf.Sin(half)) + 6;
        var c = new Canvas(length, height);
        float cy = height / 2f;
        // The faint wedge...
        c.Paint((x, y) =>
        {
            float dx = x, dy = y - cy;
            return dx > 0f && Mathf.Abs(Mathf.Atan2(dy, dx)) <= half;
        }, new Color32(255, 84, 70, 46));
        // ...its two edges, as dashes...
        for (int side = -1; side <= 1; side += 2)
            for (int s = 0; s < length; s += 16)
                c.Line(s * Mathf.Cos(half), cy + side * s * Mathf.Sin(half),
                       (s + 9f) * Mathf.Cos(half), cy + side * (s + 9f) * Mathf.Sin(half), 3f, Cream);
        // ...and one solid lane per bolt.
        for (int i = 0; i < bolts; i++)
        {
            float f = bolts == 1 ? 0f : i / (float)(bolts - 1) * 2f - 1f;
            float a = f * half;
            c.Line(8f, cy + 8f * Mathf.Tan(a), length * Mathf.Cos(a), cy + length * Mathf.Sin(a), 6f, Ink);     // a plum edge...
        }
        for (int i = 0; i < bolts; i++)
        {
            float f = bolts == 1 ? 0f : i / (float)(bolts - 1) * 2f - 1f;
            float a = f * half;
            c.Line(8f, cy + 8f * Mathf.Tan(a), length * Mathf.Cos(a), cy + length * Mathf.Sin(a), 3.4f, Danger);   // ...round the lane
        }
        return c.ToSprite(length, new Vector2(0f, 0.5f), "Fan", fullRect: true);
    });
}
