using System.Collections.Generic;
using System.Linq;

// One thing to do inside a quest. It's finished when its condition holds (see Condition.cs), which
// is how quests get progress for free: they only *read* flags, counters and items, so nothing
// about a quest is ever saved.
public class QuestStep
{
    public string Text;           // "Wake the sleepy trees"
    public string Done;           // the condition that finishes this step
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
                new QuestStep { Text = "Find the little frog. He loves hiding in bushes.", Done = FrogBush.FoundFlag, Icon = "icon:frog" },
                new QuestStep { Text = "Tell Coralie, in the castle pond.", Done = "thanked:frog", Icon = "npc:Coralie" },
            },
        },
        new QuestDefinition
        {
            Id = "cove", Title = "The Pirates' Spell", Giver = "Pearl", Start = "met:Pearl",
            Steps = new[]
            {
                new QuestStep { Text = "Beat Captain Grumblebeard and break the sea-spell.", Done = "cleared:Cove", Icon = "icon:monster" },
                new QuestStep { Text = "Tell Pearl the good news.", Done = "thanked:cove", Icon = "npc:Pearl" },
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
                    Done = "made:strawberry_pancakes>=1", Icon = "item:pancakes",
                },
            },
        },
        new QuestDefinition
        {
            Id = "maze", Title = "The Glowing Hat", Giver = "Pippin", Start = "met:Pippin",
            Steps = new[]
            {
                new QuestStep { Text = "Find the hat at the middle of Hollow Farm's corn maze.", Done = "has:pumpkin_hat", Icon = "item:pumpkin_hat" },
            },
        },
        new QuestDefinition
        {
            Id = "farm", Title = "The Haunted Farm", Giver = "Old Stitches", Start = "met:Old Stitches",
            Steps = new[]
            {
                new QuestStep { Text = "Beat the Pumpkin King in the graveyard.", Done = "cleared:Farm", Icon = "icon:monster" },
            },
        },
        new QuestDefinition
        {
            Id = "amethyst", Title = "The Lost Amethyst", Giver = "Amethyra", Start = "told:amethyst",
            Steps = new[]
            {
                new QuestStep { Text = "Beat the Slime King, deep in the dungeon.", Done = "cleared:Dungeon", Icon = "icon:monster" },
                new QuestStep { Text = "Take the Amethyst he was sitting on.", Done = "has:amethyst", Icon = "item:amethyst" },
                new QuestStep { Text = "Bring it to Amethyra, in her cave.", Done = "thanked:amethyst", Icon = "npc:Amethyra" },
            },
        },
        new QuestDefinition
        {
            Id = "egg", Title = "The Lost Egg", Giver = "Amethyra", Start = "thanked:amethyst",
            Steps = new[]
            {
                new QuestStep { Text = "Hop over the gap you couldn't cross, and find one of Amethyra's eggs.", Done = "has:dragon_egg_castle", Icon = "item:dragon_egg_castle" },
                new QuestStep { Text = "Show it to Amethyra.", Done = "thanked:egg_castle", Icon = "npc:Amethyra" },
            },
        },
        new QuestDefinition
        {
            Id = "trees", Title = "Wake the Trees", Giver = "Old Moss", Start = "met:Old Moss",
            Steps = new[]
            {
                new QuestStep
                {
                    Text = "Wake the sleepy trees with your magic.", Done = "trees_woken>=" + SleepyTreeGoal,
                    Icon = "icon:tree", Counter = "trees_woken", Goal = SleepyTreeGoal,
                },
                new QuestStep { Text = "Tell Old Moss.", Done = "thanked:moss", Icon = "npc:Old Moss" },
            },
        },
        new QuestDefinition
        {
            Id = "mushroom", Title = "Mother Mushroom", Giver = "Old Moss", Start = "thanked:moss",
            Steps = new[]
            {
                new QuestStep { Text = "Beat Mother Mushroom in her grove.", Done = "cleared:Woods4", Icon = "icon:monster" },
                new QuestStep { Text = "Take the Fairy Lantern she was guarding.", Done = "has:fairy_lantern", Icon = "item:fairy_lantern" },
            },
        },
    };

    public const int SleepyTreeGoal = 4;

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

    // The quests to show in the log: the ones in progress first, then the finished ones.
    public static List<QuestDefinition> Listed() =>
        All.Where(q => StateOf(q) == QuestState.Active).Concat(All.Where(q => StateOf(q) == QuestState.Done)).ToList();
}
