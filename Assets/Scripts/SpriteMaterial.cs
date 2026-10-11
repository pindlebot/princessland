using UnityEngine;

// The material every sprite in the game uses (Assets/Resources/Materials/SpriteUnlit, the Tidecrown/Sprite shader), and
// the white "flash" copy of it. Sprites made in the editor get it from DungeonBuilder (SpriteMaterials); sprites made
// while playing (rings, pops, warnings) ask for it here.
public static class SpriteMaterial
{
    private static Material normal, flash;

    public static Material Normal
    {
        get
        {
            if (normal == null) normal = Resources.Load<Material>("Materials/SpriteUnlit");
            return normal;
        }
    }

    // The same shader at full flash: a flat white copy of the picture.
    public static Material Flash
    {
        get
        {
            if (flash == null)
            {
                var shader = Resources.Load<Shader>("Shaders/Sprite");
                if (shader != null) flash = new Material(shader) { name = "SpriteFlash (shared)" };
                if (flash != null) flash.SetFloat("_Flash", 1f);
            }
            return flash;
        }
    }

    // Give a sprite the game's material (does nothing if it isn't there, so the default is used).
    public static SpriteRenderer Apply(SpriteRenderer renderer)
    {
        if (renderer != null && Normal != null) renderer.sharedMaterial = Normal;
        return renderer;
    }
}
