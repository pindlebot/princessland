using System.Linq;

// The first-session coach: one small action at a time (walk, talk, cast, open a chest or eat something,
// clear a thorny bramble), reusing the real things in the castle grounds instead of a tutorial level.
//
// It's all flags in GameSession ("tip:walk", ...), so it is saved with the slot, forgotten by
// GameSession.NewGame (a second save starts fresh, never inheriting the first one's progress), and never
// blocks anything: every step also finishes on its own when the player does the thing, in any order.
// "tips:off" silences the coach (G); pressing G again brings it back and starts the steps over.
public enum FirstStep { Walk, Talk, Spell, Treasure, Obstacle, Done }

public static class FirstSteps
{
    public const string OffFlag = "tips:off";
    private static readonly string[] DoneFlags = { "tip:walk", "tip:talk", "tip:spell", "tip:treasure", "tip:obstacle" };

    public static bool Enabled => !GameSession.Flags.Contains(OffFlag);

    // The step the player is on: the first one not done yet.
    public static FirstStep Current
    {
        get
        {
            for (int i = 0; i < DoneFlags.Length; i++)
                if (!GameSession.Flags.Contains(DoneFlags[i])) return (FirstStep)i;
            return FirstStep.Done;
        }
    }

    public static bool IsDone(FirstStep step) => step == FirstStep.Done || GameSession.Flags.Contains(DoneFlags[(int)step]);

    public static void Complete(FirstStep step)
    {
        if (step != FirstStep.Done) GameSession.Flags.Add(DoneFlags[(int)step]);
    }

    // A save made before the coach existed (or any adventure that's clearly under way) has nothing to
    // learn: mark every step done so it never shows tips for things the player already knows.
    // Returns true when it did that.
    public static bool SkipIfExperienced()
    {
        if (DoneFlags.Any(GameSession.Flags.Contains) || GameSession.Flags.Contains(OffFlag)) return false;
        bool experienced = GameSession.Progress.Xp > 0 || GameSession.Progress.Level > 1
                           || GameSession.Flags.Any(f => f.StartsWith("met:") || f.StartsWith("used:") || f.StartsWith("cleared:"));
        if (experienced) foreach (var step in System.Enum.GetValues(typeof(FirstStep)).Cast<FirstStep>()) Complete(step);
        return experienced;
    }

    // Turn the coach off (and keep it off in the save), or on again from the first step.
    public static void SetEnabled(bool on)
    {
        if (on)
        {
            GameSession.Flags.Remove(OffFlag);
            foreach (var f in DoneFlags) GameSession.Flags.Remove(f);
        }
        else GameSession.Flags.Add(OffFlag);
    }

    // The goal for the HUD's objective card: a short picture-first sentence for the step you're on.
    // friend: who to say hello to (the nearest one you haven't met), or "" if nobody's around.
    public static string Goal(FirstStep step, string friend) => step switch
    {
        FirstStep.Walk => "Look around the castle grounds",
        FirstStep.Talk => string.IsNullOrEmpty(friend) ? "Say hello to someone friendly" : $"Say hello to {friend}",
        FirstStep.Spell => "Zap a monster with your magic",
        FirstStep.Treasure => "Open a chest or find something yummy",
        FirstStep.Obstacle => "Zap the thorny bramble to clear the way",
        _ => "",
    };
}
