using UnityEngine;
using UnityEngine.SceneManagement;

// The Castle Grounds start grey: the Grey Gloom drained the colour from the island when it stole the
// Amethyst. This is a full-screen effect on the camera that fades the world's saturation, and it
// floods back (with a fanfare) the first time you arrive after the condition is met ("has:amethyst").
// The map file says so with a "grey_until:" header; levels without one are in full colour.
// The HUD is drawn on top of this effect, so it stays bright.
[RequireComponent(typeof(Camera))]
public class ColorDrain : MonoBehaviour
{
    [SerializeField] private Shader shader;
    [Tooltip("A Condition (Condition.cs): once it holds, the colour is back.")]
    [SerializeField] private string returnsWhen = "has:amethyst";
    [SerializeField, Range(0f, 1f)] private float drained = 0.12f;
    [SerializeField] private float waitSeconds = 1.2f;   // how long you see the grey before the colour comes back
    [SerializeField] private float floodSeconds = 3.5f;
    [SerializeField] private AudioClip returnSound;

    private Material material;
    private float arrivedAt;

    public float Saturation { get; private set; } = 1f;
    public bool IsBack => Condition.Met(returnsWhen);
    public static string SeenFlag(string scene) => "color_seen:" + scene;

    private void Start()
    {
        arrivedAt = Time.time;
        string seen = SeenFlag(SceneManager.GetActiveScene().name);
        // Grey on arrival; if the colour is already earned and this is the first visit since, it floods back in a moment.
        Saturation = IsBack && GameSession.Flags.Contains(seen) ? 1f : drained;
        if (shader != null) material = new Material(shader);
    }

    private void Update()
    {
        if (!IsBack) { Saturation = drained; return; }
        if (Saturation >= 1f || Time.time - arrivedAt < waitSeconds) return;

        if (GameSession.Flags.Add(SeenFlag(SceneManager.GetActiveScene().name)))
        {
            AudioManager.Play(returnSound);
            var hud = FindAnyObjectByType<HudController>();
            if (hud != null) hud.ShowToast("The colours are coming back!");
        }
        Saturation = Mathf.MoveTowards(Saturation, 1f, Time.deltaTime * (1f - drained) / floodSeconds);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null || Saturation >= 0.999f)
        {
            Graphics.Blit(source, destination);
            return;
        }
        material.SetFloat("_Saturation", Saturation);
        Graphics.Blit(source, destination, material);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
