using System.Collections.Generic;

// Things that need to survive loading a new scene. Every object in a scene is destroyed
// when the next scene loads, but static fields live on, so this is the simplest way to
// carry state between scenes: which hero was picked, where to arrive, what's been done.
public static class GameSession
{
    // Null until the player picks someone; levels then fall back to their default (the wizard),
    // so pressing Play directly in a level scene still works.
    public static CharacterDefinition SelectedCharacter;

    // Which named spawn point to arrive at in the next scene (e.g. "FromHouse" puts you
    // outside the castle gate instead of at the level start). Used once, then cleared.
    public static string NextSpawn;

    // Simple facts about this playthrough, e.g. "cleared:Level0" or "met:Amethyra",
    // so leaving and re-entering a scene doesn't undo them.
    public static readonly HashSet<string> Flags = new HashSet<string>();

    // Level, experience, gold and skills.
    public static Progression Progress = new Progression();

    // Called when a hero is picked: forget the previous playthrough.
    public static void NewGame(CharacterDefinition hero)
    {
        SelectedCharacter = hero;
        NextSpawn = null;
        Flags.Clear();
        Progress = new Progression();
    }
}
