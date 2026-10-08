using System;
using UnityEngine;

// Per-save choices made in the pause menu. Each save slot has its own, so a child's slot
// can be in Gentle Mode while a grown-up's is in Adventurer Mode.
[Serializable]
public class GameSettings
{
    public const int VolumeSteps = 5;

    public bool gentle = true;                 // Gentle Mode (the default) or Adventurer Mode
    public int musicVolume = VolumeSteps;      // 0..VolumeSteps
    public int soundVolume = VolumeSteps;

    public float Music01 => Mathf.Clamp(musicVolume, 0, VolumeSteps) / (float)VolumeSteps;
    public float Sound01 => Mathf.Clamp(soundVolume, 0, VolumeSteps) / (float)VolumeSteps;

    public string ModeName => gentle ? "Gentle" : "Adventurer";
    public string ModeDescription => gentle
        ? "No Game Over. Monsters bump softly."
        : "The classic rules. Watch your hearts!";
}
