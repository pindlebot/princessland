using UnityEngine;

// Plays the scene's music loop and every sound effect. One per scene (the builder adds
// it), each with its own music, so the castle grounds and the dungeon sound different.
//
// Gameplay scripts own *which* sound to play (e.g. SpellAbility has a castSound field you
// can swap in the Inspector) and just call AudioManager.Play(clip). Sounds are "2D": in a
// small top-down game, everything on screen should be equally loud.
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float effectsVolume = 0.8f;
    [Tooltip("Each effect plays at a slightly random pitch, so repeated sounds don't feel robotic.")]
    [SerializeField] [Range(0f, 0.2f)] private float pitchVariation = 0.06f;
    [SerializeField] private int voices = 8; // how many effects can overlap

    public static AudioManager Instance { get; private set; }

    // Static so the mute choice (M) survives loading the next scene.
    private static bool musicMuted;

    private AudioSource musicSource;
    private AudioSource[] effectSources;
    private int nextVoice;

    // The most recent effect, handy for debugging and tests.
    public AudioClip LastPlayed { get; private set; }
    public bool MusicPlaying => musicSource.isPlaying && !musicSource.mute;
    public AudioClip Music => music;

    private void Awake()
    {
        Instance = this;

        // An AudioSource plays one clip at a time, so effects get a small pool of them
        // and take turns (round robin), letting several sounds overlap.
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.volume = musicVolume * GameSession.Settings.Music01;
        musicSource.mute = musicMuted;
        musicSource.playOnAwake = false;

        effectSources = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            effectSources[i] = gameObject.AddComponent<AudioSource>();
            effectSources[i].playOnAwake = false;
        }
    }

    private void Start()
    {
        if (music != null) musicSource.Play();
    }

    private void Update()
    {
        musicSource.volume = musicVolume * GameSession.Settings.Music01; // the pause menu's music setting
        if (GameInput.MutePressed)
        {
            musicMuted = !musicMuted;
            musicSource.mute = musicMuted;
        }
    }

    // How loud effects are right now (the pause menu's sound setting), for sounds that play
    // from their own AudioSource, like a looping faucet (AmbientLoop).
    public static float EffectsVolume => Instance != null ? Instance.effectsVolume * GameSession.Settings.Sound01 : 0f;

    // Safe to call from anywhere: does nothing if the clip isn't set or there's no manager.
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || Instance == null) return;
        Instance.PlayEffect(clip, volume);
    }

    private void PlayEffect(AudioClip clip, float volume)
    {
        var source = effectSources[nextVoice];
        nextVoice = (nextVoice + 1) % effectSources.Length;
        source.clip = clip;
        source.volume = effectsVolume * GameSession.Settings.Sound01 * volume;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.Play();
        LastPlayed = clip;
    }
}
