using UnityEngine;

// Makes a light flicker like a flame. Perlin noise gives smooth, natural-looking
// wobble (Random.value would jump around harshly), and each light gets its own
// offset into the noise so torches don't flicker in sync.
[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 1.4f;
    [SerializeField] private float flickerAmount = 0.35f;
    [SerializeField] private float speed = 6f;

    private Light flame;
    private float seed;

    private void Awake()
    {
        flame = GetComponent<Light>();
        seed = Random.value * 100f;
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(seed, Time.time * speed); // 0..1
        flame.intensity = baseIntensity + (noise - 0.5f) * 2f * flickerAmount;
    }
}
