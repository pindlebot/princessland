using UnityEditor;
using UnityEngine;

// Friendly NPCs share one prefab recipe (CreateNpcPrefab) and one component (Npc).
// This file has that recipe, Amethyra the dragon (map character 'D') and her lines;
// DungeonBuilder.Friends.cs has Coralie the mermaid and Bonesy the skeleton.
public static partial class DungeonBuilder
{
    // One conversation (see Npc.Conversation for what the fields mean). Lines are written
    // with N("...") for the NPC and H("...") for the hero; "{hero}" becomes the hero's name.
    private class Talk
    {
        public string Requires = "", NotIf = "", Sets = "";
        public int Gold;
        public bool SmallTalk;
        public (bool heroSpeaks, string text)[] Lines;
    }

    private static (bool, string) N(string text) => (false, text);
    private static (bool, string) H(string text) => (true, text);

    private static Talk[] DragonTalks() => new[]
    {
        new Talk
        {
            NotIf = "met:Amethyra", Sets = "met:Amethyra",
            Lines = new[]
            {
                N("Well now, a visitor! You found my cave! Don't be frightened, {hero}. I don't bite... often."),
                H("A dragon?! Hiding up here, by the castle?"),
                N("I am Amethyra, last of the amethyst dragons. I've watched over this castle for three hundred years."),
                N("Those rattling skeletons crawled up from the dungeon below. Clear them away, and the way down will open."),
                H("Won't you come and help?"),
                N("My fighting days are long behind me, little one. But I'll be right here in my cave, cheering you on. Go on, {hero}!"),
            },
        },
        // After that she takes turns between these, one per visit. (Dragon jokes: an easter egg.)
        new Talk
        {
            SmallTalk = true,
            Lines = new[] { N("Back again, {hero}? The way down lies east of the fountain. Mind those skeletons: all bones and bad manners.") },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[]
            {
                N("Knock knock."), H("Who's there?"), N("Dragon."), H("Dragon who?"),
                N("Dragon your feet won't open those stairs! Off you go! Ha!"),
            },
        },
        new Talk
        {
            SmallTalk = true,
            Lines = new[]
            {
                N("Why didn't the skeleton go to the castle ball?"), H("Why?"),
                N("Because he had no BODY to go with! Hoo hoo hoo!"),
            },
        },
        new Talk
        {
            SmallTalk = true, Requires = WishingFountain.GrantedFlag,
            Lines = new[] { N("I saw the fountain sparkle! Someone's wish came true. Was it yours, {hero}?") },
        },
    };

    private static GameObject CreateDragonPrefab(Sprite shadowSprite) =>
        // Her collider is smaller than her sprite, so you can walk close enough to talk.
        CreateNpcPrefab("Dragon", "Dragon", "Amethyra", "Assets/Art/UI/PortraitDragon.png", "voice_dragon",
                        DragonTalks(), new Vector3(2.4f, 3f, 1.6f), shadowSprite, 3.2f);

    // A talking character: a billboard sprite with Idle/Talk animations, an Npc component
    // with its conversations, and (optionally) a solid collider and a shadow.
    // npcType: Npc, or a subclass such as Merchant; configure sets that subclass's own fields.
    private static GameObject CreateNpcPrefab(string prefabName, string sheetName, string npcName, string portraitPath,
                                              string voice, Talk[] talks, Vector3? solidSize, Sprite shadowSprite, float shadowSize,
                                              System.Type npcType = null, System.Action<Npc> configure = null)
    {
        var sheet = SpriteSheetImporter.Import(sheetName);
        var go = new GameObject(prefabName);
        if (solidSize.HasValue)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, solidSize.Value.y / 2f, 0f);
            box.size = solidSize.Value;
        }

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(go.transform, false);
        sprite.AddComponent<SpriteRenderer>();
        var flipbook = sprite.AddComponent<SpriteFlipbook>();
        SetFlipbook(flipbook, sheet, "Idle", destroyWhenDone: false);
        SetBool(flipbook, "unscaledTime", true); // keeps breathing (and talking) while the game is paused
        sprite.AddComponent<Billboard>();
        if (shadowSprite != null) AddShadow(go, shadowSprite, shadowSize);

        var npc = (Npc)go.AddComponent(npcType ?? typeof(Npc));
        SetString(npc, "npcName", npcName);
        SetRef(npc, "portrait", SpriteSheetImporter.ImportSingle(portraitPath, 16));
        SetRef(npc, "voice", Sound(voice));
        SetRef(npc, "giftSound", Sound("coin"));
        SetRef(npc, "flipbook", flipbook);
        SetRefs(npc, "idleFrames", sheet.Frames("Idle"));
        SetRefs(npc, "talkFrames", sheet.Frames("Talk"));
        SetFloat(npc, "idleFps", sheet.Anim("Idle").fps);
        SetFloat(npc, "talkFps", sheet.Anim("Talk").fps);
        SetConversations(npc, talks);
        configure?.Invoke(npc);
        return SavePrefab(go, prefabName);
    }

    // Arrays of serialized classes are edited through SerializedProperty: one element per
    // conversation, then each field by name (and the lines inside it the same way).
    private static void SetConversations(Npc npc, Talk[] talks)
    {
        var so = new SerializedObject(npc);
        var list = so.FindProperty("conversations");
        list.arraySize = talks.Length;
        for (int i = 0; i < talks.Length; i++)
        {
            var t = talks[i];
            var c = list.GetArrayElementAtIndex(i);
            c.FindPropertyRelative("requires").stringValue = t.Requires;
            c.FindPropertyRelative("notIf").stringValue = t.NotIf;
            c.FindPropertyRelative("sets").stringValue = t.Sets;
            c.FindPropertyRelative("giveGold").intValue = t.Gold;
            c.FindPropertyRelative("smallTalk").boolValue = t.SmallTalk;
            var lines = c.FindPropertyRelative("lines");
            lines.arraySize = t.Lines.Length;
            for (int j = 0; j < t.Lines.Length; j++)
            {
                var line = lines.GetArrayElementAtIndex(j);
                line.FindPropertyRelative("heroSpeaks").boolValue = t.Lines[j].heroSpeaks;
                line.FindPropertyRelative("text").stringValue = t.Lines[j].text;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
