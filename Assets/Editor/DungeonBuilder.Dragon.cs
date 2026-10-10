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
        public string Requires = "", NotIf = "", Sets = "", GiveItem = "", TakeItem = "";
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
        // The story: the Amethyst, the Bouncy Boots, and the lost eggs (QuestCatalog: "The Lost Amethyst", "The Lost Egg").
        new Talk
        {
            Requires = "cleared:Level0", NotIf = "told:amethyst", Sets = "told:amethyst",
            Lines = new[]
            {
                N("The grounds are clear! Well done, {hero}. Now sit a moment, because I owe you the truth."),
                N("The Grey Gloom stole the Amethyst, the heart of my crown, and without it a plague of dark crystals is spreading over this island. You've seen them: they're everywhere. It's why I can't fly."),
                H("Who has it?"),
                N("The Slime King, in the dungeon below. He's been sitting on it like a hen on an egg. Beat him, bring it back to me, and the crystals will crumble away."),
                N("And {hero}... the Gloom took my five dragon eggs as well. One for each island. Keep your eyes open for them."),
            },
        },
        new Talk
        {
            Requires = "has:amethyst", NotIf = "thanked:amethyst", Sets = "thanked:amethyst", Gold = 50,
            Lines = new[]
            {
                N("My Amethyst! Oh, {hero}, you brought it home! Hold it up. See how it glows?"),
                H("The crystals are all breaking up!"),
                N("Yes! The crystals are crumbling away across the castle grounds. And look at those boots you're wearing: made from the Slime King's jelly. Bouncy Boots!"),
                N("With them you can hop over gaps. There were places in the grounds and in the dungeon you couldn't cross. Do you remember them? Now you can!"),
                N("Go on, have a look around. I have a feeling there are treasures waiting, and maybe even one of my eggs."),
            },
        },
        new Talk
        {
            Requires = "has:dragon_egg_castle", NotIf = "thanked:egg_castle", Sets = "thanked:egg_castle", Gold = 30,
            Lines = new[]
            {
                N("Is that... it IS! My egg! Oh, it's warm. It's wiggling!"),
                H("It was behind a gap in the dungeon."),
                N("Clever {hero}! Keep it safe with you for now. When we've found all five, we'll hatch them together. Four more to go, in the other islands."),
            },
        },
        new Talk
        {
            Requires = "has:topaz", NotIf = "thanked:topaz", Sets = "thanked:topaz", Gold = 60,
            Lines = new[]
            {
                N("The Topaz! Oh, {hero}, the Glimmer Mines' own gem. I can feel the lamps lighting up under the island from here."),
                H("The Crystal Golem was only grumpy."),
                N("The Gloom got into him, poor lad. And look at those paws: Mole Mitts! They shove blocks and dig up soil. This island is full of heavy stones and loose mounds that you couldn't deal with before."),
                N("Go on, have another look around. Treasure loves to hide where only a mole would dig."),
            },
        },
        new Talk
        {
            Requires = "has:dragon_egg_mines", NotIf = "thanked:egg_mines", Sets = "thanked:egg_mines", Gold = 30,
            Lines = new[]
            {
                N("A golden egg! That's two of my five, {hero}. Oh, it's warm. It hums!"),
                H("It was behind a big stone block in the Crystal Cavern."),
                N("Of course it was. Three more islands to go: the lake, the snowy mountain, and the Spire. Keep them safe."),
            },
        },
        new Talk
        {
            Requires = "has:aquamarine", NotIf = "thanked:aquamarine", Sets = "thanked:aquamarine", Gold = 60,
            Lines = new[]
            {
                N("The Aquamarine! I can hear the lake singing from here. Clear water, at last."),
                H("King Crabbington was only hiding in his shell."),
                N("Crabs do that when the world gets too loud. And the Bubble Charm! Wear it, and you can swim over any deep water, {hero}. The castle pond, the cove, the sea. Islands no boat ever found."),
            },
        },
        new Talk
        {
            Requires = "has:dragon_egg_lake", NotIf = "thanked:egg_lake", Sets = "thanked:egg_lake", Gold = 30,
            Lines = new[]
            {
                N("A sea-green egg! Three of five now, {hero}. It's cool to the touch, and it sloshes."),
                H("It was on an island in the middle of the Murky Reeds. I had to swim."),
                N("You swam! Of course you did. Two more, up the snowy mountain and in the clouds. Take care."),
            },
        },
        new Talk
        {
            Requires = "has:sapphire", NotIf = "thanked:sapphire", Sets = "thanked:sapphire", Gold = 80,
            Lines = new[]
            {
                N("The Sapphire! Snow is falling gently, for the first time in three hundred years. Can you feel it?"),
                H("The Snow Yeti was only lonely."),
                N("Aren't we all, from time to time? And look: the Rainbow Chalk. Draw a bridge between two rainbow posts and you can cross any chasm in Gemhold. There are chasms all over this island, and some in the others."),
                N("Only the Ruby is left now, {hero}, and the Grey Gloom wears it. He lives in the Storm Spire, above the clouds."),
            },
        },
        new Talk
        {
            Requires = "has:dragon_egg_frost", NotIf = "thanked:egg_frost", Sets = "thanked:egg_frost", Gold = 30,
            Lines = new[]
            {
                N("A blue egg, frosty on the outside! That's four of my five, {hero}. Oh, it's warm underneath. It's purring!"),
                H("It was across a rainbow bridge in the Frozen Pass."),
                N("One more, in the clouds. When we have them all, we'll hatch them together. I can hardly wait."),
            },
        },
        // Easter eggs that count visits ("talks:Amethyra" goes up each time you finish a chat with her, so
        // the 10th chat is when it reads 9). The first one that applies wins, so these come before small talk.
        new Talk
        {
            Requires = "talks:Amethyra>=24", NotIf = "snored", Sets = "snored",
            Lines = new[]
            {
                N("Oh, {hero}, you again! You do like chatting. I was just... just... *yawn*..."),
                N("Zzzzz... snrrrk... mmm, pancakes... snrrrk... Zzzzz..."),
                H("(She's fast asleep. Her snoring rumbles like a little thunderstorm.)"),
                N("Zzzz... don't wake the dragon... Zzzz... Hoo... zzzz..."),
            },
        },
        // Forgot to wash up after the toilet? She can tell.
        new Talk
        {
            Requires = HouseFixture.UnwashedFlag,
            Lines = new[]
            {
                N("...{hero}. Did you wash your hands?"),
                H("Um... maybe?"),
                N("A dragon knows these things. Hands wash at the sink at home, dear. Soap first, then rinse. Off you go!"),
            },
        },
        new Talk
        {
            Requires = "talks:Amethyra>=9", NotIf = "joked:ten", Sets = "joked:ten", Gold = 10,
            Lines = new[]
            {
                N("Ten chats! That deserves my VERY best joke. What do you call a dragon who never wakes up?"),
                H("What?"),
                N("A SNORE-asaurus! ...No wait, I'm a dragon, not a dinosaur. A snore-a-DRAGON! Hoo hoo hoo!"),
                N("Here, a tip for listening. Dragons always pay for a good audience."),
            },
        },
        new Talk
        {
            Requires = HouseFixture.HandsWashedCounter + ">=5", NotIf = "praised:hands", Sets = "praised:hands",
            Lines = new[]
            {
                N("Is that lavender I smell? {hero}, your hands are squeaky clean!"),
                H("I washed them five times!"),
                N("Splendid. Cleanest hero in all of Gemhold. I'm so proud I could breathe sparkles."),
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
            c.FindPropertyRelative("giveItem").stringValue = t.GiveItem;
            c.FindPropertyRelative("takeItem").stringValue = t.TakeItem;
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
