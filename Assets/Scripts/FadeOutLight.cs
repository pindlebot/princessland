using UnityEngine;

// Fades a light to zero over a short time: a quick flash for explosions.
[RequireComponent(typeof(Light))]
public class FadeOutLight : MonoBehaviour
{
    [SerializeField] private float duration = 0.35f;

    private Light flash;
    private float startIntensity;
    private float startTime;

    private void Awake()
    {
        flash = GetComponent<Light>();
        startIntensity = flash.intensity;
        startTime = Time.time;
    }

    private void Update()
    {
        float t = (Time.time - startTime) / duration;
        flash.intensity = Mathf.Lerp(startIntensity, 0f, t);
    }
}
