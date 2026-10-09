using UnityEngine;

// A sound that loops for as long as its object is active: the bath's running faucet. Turning
// the object on starts it and turning it off stops it, so the faucet only has to show or hide
// its stream. It follows the pause menu's sound volume like every other effect.
public class AmbientLoop : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

    private AudioSource source;

    private void OnEnable()
    {
        if (clip == null) return;
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
        }
        source.volume = AudioManager.EffectsVolume * volume;
        source.Play();
    }

    private void OnDisable()
    {
        if (source != null) source.Stop();
    }

    private void Update()
    {
        if (source != null) source.volume = AudioManager.EffectsVolume * volume;
    }
}
