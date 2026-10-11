using System.Collections.Generic;
using System.Linq;

// One thing to do inside a quest. It's finished when its condition holds (see Condition.cs), which
// is how quests get progress for free: they only *read* flags, counters and items, so nothing
// about a quest is ever saved.
public class QuestStep
{
    public string Text;           // "Wake the sleepy trees"
    public string Done;           // the condition that finishes this step
    public string Where = "";      // the scene this step is done in (or where to go back to): the tracker and world map point at it
    public string Icon = "";      // its picture: "item:<id>", "npc:<Name>" or "icon:<name>" (see QuestPictures)
    public string Counter = "";   // optional: show progress "(2/4)" from this counter...
    public int Goal;              // ...up to this many
}

public class QuestDefinition
{
    public string Id;
    public string Title;
    public string Giver;          // the NPC's picture key (their name); "" shows the first step's icon instead
    public string Start;          // the condition that puts this quest in the log
    public QuestStep[] Steps;
}

public enum QuestState { Hidden, Active, Done }

// Every quest in the game, as data. The quest log (J) lists the ones that have started, with the
// giver's picture and a picture for the step you're on, so it reads without reading.
//
// Steps are done in order and stay done, so a step's condition has to be something that doesn't
// un-happen: a flag, a counter, or an item you keep (not an ingredient that gets used up).
public static class QuestCatalog
{
    public static readonly QuestDefinition[] All =
    {
        new QuestDefinition
        {
            Id = "frog", Title = "Where's Sir Hopsalot?", Giver = "Coralie", Start = "met:Coralie",
            Steps = new[]
            {
                new QuestStep { Text = "Find the little frog. He loves hiding in bushes.", Where = "Level0", Done = FrogBush.FoundFlag, Icon = "icon:frog" },
                new QuestStep { Text = "Tell Coralie, in the castle pond.", Where = "Level0", Done = "thanked:frog", Icon = "npc:Coralie" },
            },
        },
        new QuestDefinition
        {
            Id = "cove", Title = "The Pirates' Spell", Giver = "Pearl", Start = "met:Pearl",
            Steps = new[]
            {
                new QuestStep { Text = "Beat Captain Grumblebeard and break the sea-spell.", Where = "Cove", Done = "cleared:Cove", Icon = "icon:monster" },
                new QuestStep { Text = "Tell Pearl the good news.", Where = "Cove", Done = "thanked:cove", Icon = "npc:Pearl" },
            },
        },
        new QuestDefinition
        {
            Id = "pancakes", Title = "Pancake Breakfast", Giver = "", Start = "visited:Kitchen",
            Steps = new[]
            {
                new QuestStep
                {
                    Text = "Cook Strawberry Pancakes at the stove: an egg (the hens' coop), flour (the pantry) and a strawberry (the fruit bowl).",
                    Where = "Kitchen", Done = "made:strawberry_pancakes>=1", Icon = "item:pancakes",
                },
            },
        },
        new QuestDefinition
        {
            Id = "maze", Title = "The Glowing Hat", Giver = "Pippin", Start = "met:Pippin",
            Steps = new[]
            {
                new QuestStep { Text = "Find the hat at the middle of Hollow Farm's corn maze.", Where = "Farm", Done = "has:pumpkin_hat", Icon = "item:pumpkin_hat" },
            },
        },
        new QuestDefinition
        {
            Id = "farm", Title = "The Haunted Farm", Giver = "Old Stitches", Start = "met:Old Stitches",
            Steps = new[]
            {
                new QuestStep { Text = "Beat the Pumpkin King in the graveyard.", Where = "Farm", Done = "cleared:Farm", Icon = "icon:monster" },
            },
        },
        new QuestDefinition
        {
            Id = "amethyst", Title = "The Lost Amethyst", Giver = "Amethyra", Start = "told:amethyst",
            Steps = new[]
            {
                new QuestStep { Text = "Beat the Slime King, deep in the dungeon.", Where = "Dungeon", Done = "cleared:Dungeon", Icon = "icon:monster" },
                new QuestStep { Text = "Take the Amethyst he was sitting on.", Where = "Dungeon", Done = "has:amethyst", Icon = "item:amethyst" },
                new QuestStep { Text = "Bring it to Amethyra, in her cave.", Where = "Level0", Done = "thanked:amethyst", Icon = "npc:Amethyra" },
            },
        },
        new QuestDefinition
        {
            Id = "egg", Title = "The Lost Egg", Giver = "Amethyra", Start = "thanked:amethyst",
            Steps = new[]
            {
                new QuestStep { Text = "Hop over the gap you couldn't cross, and find one of Amethyra's eggs.", Where = "Dungeon", Done = "has:dragon_egg_castle", Icon = "item:dragon_egg_castle" },
                new QuestStep { Text = "Show it to Amethyra.", Where = "Level0", Done = "thanked:egg_castle", Icon = "npc:Amethyra" },
            },
        },
        new QuestDefinition
        {
            Id = "trees", Title = "Wake the Trees", Giver = "Old Moss", Start = "met:Old Moss",
            Steps = new[]
            {
                new QuestStep
                {
                    Text = "Wake the sleepy trees with your magic.", Where = "Woods1", Done = "trees_woken>=" + SleepyTreeGoal,
                    Icon = "icon:tree", Counter = "trees_woken", Goal = SleepyTreeGoal,
                },
                new QuestStep { Text = "Tell Old Moss.", Where = "Woods1", Done = "thanked:moss", Icon = "npc:Old Moss" },
            },
        },
        new QuestDefinition
        {
            Id = "mushroom", Title = "Mother Mushroom", Giver = "Old Moss", Start = "thanked:moss",
            Steps = new[]
            {
                new QuestStep { Text = "Beat Mother Mushroom in her grove.", Where = "Woods4", Done = "cleared:Woods4", Icon = "icon:monster" },
                new QuestStep { Text = "Take the Fairy Lantern she was guarding.", Where = "Woods4", Done = "has:fairy_lantern", Icon = "item:fairy_lantern" },
            },
        },
        new QuestDefinition
        {
            Id = "moles", Title = "Lost Moles", Giver = "Digby", Start = "met:Digby",
            Steps = new[]
            {
                new QuestStep
                {
                    Text = "Find Digby's three lost moles in the dark tunnels.", Where = "Mines2", Done = "moles_found>=" + LostMoleGoal,
                    Icon = "icon:mole", Counter = "moles_found", Goal = LostMoleGoal,
                },
                new QuestStep { Text = "Tell Digby.", Where = "Mines1", Done = "thanked:digby", Icon = "npc:Digby" },
            },
        },
        new QuestDefinition
        {
            Id = "golem", Title = "The Crystal Golem", Giver = "Digby", Start = "thanked:digby",
            Steps = new[]
            {
                new QuestStep { Text = "Beat the Crystal Golem when his crystals glow.", Where = "Mines4", Done = "cleared:Mines4", Icon = "icon:monster" },
                new QuestStep { Text = "Take the Mole Mitts he left behind.", Where = "Mines4", Done = "has:mole_mitts", Icon = "item:mole_mitts" },
            },
        },
        new QuestDefinition
        {
            Id = "fishing", Title = "Fishing Lesson", Giver = "Captain Clamshell", Start = "met:Clamshell",
            Steps = new[]
            {
                new QuestStep
                {
                    Text = "Catch fish at a fishing spot: cast, wait for the bobber to dip, then press again.", Where = "Lake1", Done = "fish_caught>=" + FishingGoal,
                    Icon = "icon:fish", Counter = "fish_caught", Goal = FishingGoal,
                },
                new QuestStep { Text = "Tell Captain Clamshell.", Where = "Lake1", Done = "thanked:clamshell", Icon = "npc:Captain Clamshell" },
            },
        },
        new QuestDefinition
        {
            Id = "king", Title = "King Crabbington", Giver = "Captain Clamshell", Start = "thanked:clamshell",
            Steps = new[]
            {
                new QuestStep { Text = "Beat King Crabbington when he peeks out of his shell.", Where = "Lake4", Done = "cleared:Lake4", Icon = "icon:monster" },
                new QuestStep { Text = "Take the Bubble Charm he was guarding.", Where = "Lake4", Done = "has:bubble_charm", Icon = "item:bubble_charm" },
            },
        },
        new QuestDefinition
        {
            Id = "egg_lake", Title = "The Aquamarine Egg", Giver = "Amethyra", Start = "has:bubble_charm",
            Steps = new[]
            {
                new QuestStep { Text = "Swim out to the island in the middle of the Murky Reeds, and find one of Amethyra's eggs.", Where = "Lake2", Done = "has:dragon_egg_lake", Icon = "item:dragon_egg_lake" },
                new QuestStep { Text = "Show it to Amethyra.", Where = "Level0", Done = "thanked:egg_lake", Icon = "npc:Amethyra" },
            },
        },
        new QuestDefinition
        {
            Id = "scarf", Title = "A Scarf for Mr. Frost", Giver = "Mr. Frost", Start = "met:Mr. Frost",
            Steps = new[]
            {
                new QuestStep { Text = "Buy a ball of yarn from Barnaby Badger, in Hollyhock by the castle.", Where = "Level0", Done = "bought:ball_of_yarn>=1", Icon = "item:ball_of_yarn" },
                new QuestStep { Text = "Give the yarn to Granny Purl, by the campfire.", Where = "Frost1", Done = "purl:knit", Icon = "npc:Granny Purl" },
                new QuestStep { Text = "Take the scarf to Mr. Frost.", Where = "Frost1", Done = "thanked:frost", Icon = "npc:Mr. Frost" },
            },
        },
        new QuestDefinition
        {
            Id = "yeti", Title = "The Snow Yeti", Giver = "Granny Purl", Start = "met:Granny Purl",
            Steps = new[]
            {
                new QuestStep { Text = "Beat the Snow Yeti, high on the mountain.", Where = "Frost4", Done = "cleared:Frost4", Icon = "icon:monster" },
                new QuestStep { Text = "Take the Rainbow Chalk he was guarding.", Where = "Frost4", Done = "has:rainbow_chalk", Icon = "item:rainbow_chalk" },
            },
        },
        new QuestDefinition
        {
            Id = "egg_frost", Title = "The Sapphire Egg", Giver = "Amethyra", Start = "has:rainbow_chalk",
            Steps = new[]
            {
                new QuestStep { Text = "Draw a rainbow bridge in the Frozen Pass, and find one of Amethyra's eggs.", Where = "Frost3", Done = "has:dragon_egg_frost", Icon = "item:dragon_egg_frost" },
                new QuestStep { Text = "Show it to Amethyra.", Where = "Level0", Done = "thanked:egg_frost", Icon = "npc:Amethyra" },
            },
        },
        new QuestDefinition
        {
            Id = "egg_mines", Title = "The Topaz Egg", Giver = "Amethyra", Start = "has:mole_mitts",
            Steps = new[]
            {
                new QuestStep { Text = "Find one of Amethyra's eggs in the Glimmer Mines. It's behind something heavy.", Where = "Mines3", Done = "has:dragon_egg_mines", Icon = "item:dragon_egg_mines" },
                new QuestStep { Text = "Show it to Amethyra.", Where = "Level0", Done = "thanked:egg_mines", Icon = "npc:Amethyra" },
            },
        },
    };

    public const int SleepyTreeGoal = 4;
    public const int LostMoleGoal = 3;
    public const int FishingGoal = 3;

    public static QuestDefinition Find(string id) => All.FirstOrDefault(q => q.Id == id);

    // Where the quest stands: not yet heard of, in progress, or finished (which wins over "started",
    // so a quest you finished before anyone gave it to you still shows up, as done).
    public static QuestState StateOf(QuestDefinition quest)
    {
        if (quest.Steps.All(s => Condition.Met(s.Done))) return QuestState.Done;
        return Condition.Met(quest.Start) ? QuestState.Active : QuestState.Hidden;
    }

    // The step to do next (null once the quest is finished): the first one not done yet.
    public static QuestStep CurrentStep(QuestDefinition quest) =>
        quest.Steps.FirstOrDefault(s => !Condition.Met(s.Done));

    public static int StepsDone(QuestDefinition quest) => quest.Steps.Count(s => Condition.Met(s.Done));

    // "Wake the sleepy trees with your magic. (2/4)"
    public static string StepText(QuestStep step)
    {
        if (string.IsNullOrEmpty(step.Counter)) return step.Text;
        return $"{step.Text} ({System.Math.Min(GameSession.GetCounter(step.Counter), step.Goal)}/{step.Goal})";
    }

    // The quest the HUD tracker follows: the one the player picked in the quest log (saved as the flag "track:<id>"),
    // else the one that started most recently (the last in the log's order), else none.
    public const string TrackFlagPrefix = "track:";

    public static QuestDefinition Tracked()
    {
        var active = All.Where(q => StateOf(q) == QuestState.Active).ToList();
        foreach (var quest in active)
            if (GameSession.Flags.Contains(TrackFlagPrefix + quest.Id)) return quest;
        return active.LastOrDefault();
    }

    // Follow this quest (and no other).
    public static void Track(QuestDefinition quest)
    {
        foreach (var q in All) GameSession.Flags.Remove(TrackFlagPrefix + q.Id);
        GameSession.Flags.Add(TrackFlagPrefix + quest.Id);
    }

    // The scene the next step of a quest points to, or "" if there isn't one.
    public static string WhereNext(QuestDefinition quest) => CurrentStep(quest)?.Where ?? "";

    // The quests to show in the log: the ones in progress first, then the finished ones.
    public static List<QuestDefinition> Listed() =>
        All.Where(q => StateOf(q) == QuestState.Active).Concat(All.Where(q => StateOf(q) == QuestState.Done)).ToList();
}
