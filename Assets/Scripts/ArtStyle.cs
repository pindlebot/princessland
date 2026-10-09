using UnityEngine;

// The game's shared visual rules, in one place: a pixel-art storybook diorama. The same
// values drive the art generators (Tools/palette.py) and the HUD (:root in Assets/UI/Hud.uss),
// so keep the three in step.
//   - One pixel size: every sprite and texture is 16 pixels per unit and placed at scale 1.
//     The camera picks a whole number of screen pixels per art pixel (IsoCameraFollow) and
//     sprites are snapped to that grid while drawing (PixelSnap), so edges stay crisp.
//   - One sun: blob shadows lie flat and lean a little the same way the sun's real shadows fall.
//   - A quiet world (sage, sand, lavender-grey stone) so characters and magic stand out.
public static class ArtStyle
{
    public const int PixelsPerUnit = 16;

    // The view is about 16 units tall; the exact size is nudged so the pixel scale is whole.
    public const float TargetOrthoSize = 8f;

    // The sun: high in the south-west, so shadows fall away from the camera and a little left.
    public static readonly Quaternion OutdoorSun = Quaternion.Euler(50f, 30f, 0f);
    public static readonly Quaternion IndoorSun = Quaternion.Euler(55f, 20f, 0f);

    // How far a blob shadow leans away from the sun, in units: just enough to read as cast,
    // not so much that it detaches from the feet.
    public const float ShadowLean = 0.1f;

    // The ground-plane direction shadows fall in, for a given sun.
    public static Vector3 ShadowDirection(Quaternion sun)
    {
        Vector3 d = sun * Vector3.forward;
        d.y = 0f;
        return d.normalized;
    }

    public static Vector3 ShadowOffset(Quaternion sun) => ShadowDirection(sun) * ShadowLean;

    // Around the floating island: a soft, slightly warm sky (palette.SKY).
    public static readonly Color Sky = new Color32(176, 212, 228, 255);
    public static readonly Color Night = new Color(0.03f, 0.03f, 0.05f);
    public static readonly Color DuskSky = new Color32(92, 70, 112, 255); // Hollow Farm: violet, going dark
}
