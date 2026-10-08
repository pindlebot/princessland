using UnityEditor;
using UnityEngine;

// Amethyra the dragon: a big friendly NPC in Level 0 (map character 'D').
public static partial class DungeonBuilder
{
    // Her conversations. "{hero}" becomes the hero's name ("Aldric" or "Princess Marina").
    // false = Amethyra speaks, true = the hero speaks.
    private static readonly (bool heroSpeaks, string text)[] DragonIntroduction =
    {
        (false, "Well now, a visitor! Don't be frightened, {hero}. I don't bite... often."),
        (true, "A dragon?! Here, at the castle?"),
        (false, "I am Amethyra, last of the amethyst dragons. I've watched over this castle for three hundred years."),
        (false, "Those rattling skeletons crawled up from the dungeon below. Clear them away, and the way down will open."),
        (true, "Won't you come and help?"),
        (false, "My fighting days are long behind me, little one. But I'll be right here, cheering you on. Go on, {hero}!"),
    };

    private static readonly (bool heroSpeaks, string text)[] DragonLaterChat =
    {
        (false, "Back again, {hero}? The way down lies past my castle. Mind those skeletons: all bones and bad manners."),
    };

    private static GameObject CreateDragonPrefab(Sprite shadowSprite)
    {
        var sheet = SpriteSheetImporter.Import("Dragon");

        var go = new GameObject("Dragon");
        // Smaller than her sprite, so you can walk close enough to talk (PlayerInteractor's range).
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.5f, 0f);
        box.size = new Vector3(2.4f, 3f, 1.6f);

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.AddComponent<SpriteRenderer>();
        var flipbook = sprite.AddComponent<SpriteFlipbook>();
        SetFlipbook(flipbook, sheet, "Idle", destroyWhenDone: false);
        SetBool(flipbook, "unscaledTime", true); // keeps breathing (and talking) while the game is paused
        sprite.AddComponent<Billboard>();

        AddShadow(go, shadowSprite, 3.2f);

        var npc = go.AddComponent<DragonNpc>();
        SetRef(npc, "portrait", SpriteSheetImporter.ImportSingle("Assets/Art/UI/PortraitDragon.png", 16));
        SetRef(npc, "flipbook", flipbook);
        SetRefs(npc, "idleFrames", sheet.Frames("Idle"));
        SetRefs(npc, "talkFrames", sheet.Frames("Talk"));
        SetFloat(npc, "idleFps", sheet.Anim("Idle").fps);
        SetFloat(npc, "talkFps", sheet.Anim("Talk").fps);
        SetDialogue(npc, "introduction", DragonIntroduction);
        SetDialogue(npc, "laterChat", DragonLaterChat);

        return SavePrefab(go, "Dragon");
    }

    // Arrays of structs are edited through SerializedProperty too: one element per line,
    // then each field of the struct by name.
    private static void SetDialogue(Object target, string field, (bool heroSpeaks, string text)[] lines)
    {
        var so = new SerializedObject(target);
        var list = so.FindProperty(field);
        list.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = list.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("heroSpeaks").boolValue = lines[i].heroSpeaks;
            line.FindPropertyRelative("text").stringValue = lines[i].text;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
