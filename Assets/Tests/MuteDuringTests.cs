using NUnit.Framework;
using UnityEngine;

// Silences the game while the tests run. A [SetUpFixture] outside any namespace wraps every
// test in this assembly. AudioListener.volume is the master volume: the game never changes it,
// and AudioManager's own settings (music mute, volume sliders) still work, so tests that check
// them are unaffected; you just don't hear anything.
[SetUpFixture]
public class MuteDuringTests
{
    private float volumeBefore;

    [OneTimeSetUp]
    public void Mute()
    {
        volumeBefore = AudioListener.volume;
        AudioListener.volume = 0f;
    }

    [OneTimeTearDown]
    public void Unmute() => AudioListener.volume = volumeBefore;
}
