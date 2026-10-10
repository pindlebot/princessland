using UnityEngine;

// Carry the Fairy Lantern and a warm little light follows you, bright enough to see by in the dark
// hollows. LevelBootstrap adds this to every hero; it makes its own light and keeps it switched on
// only while the lantern is in the treasures tab, so picking it up lights you up straight away.
public class LanternLight : MonoBehaviour
{
    public const string ItemId = "fairy_lantern";

    private Light glow;
    private Inventory inventory;

    public bool IsLit => glow != null && glow.enabled;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        glow = new GameObject("FairyLight").AddComponent<Light>();
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0.6f, 2.2f, -0.3f);
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.92f, 0.62f);
        glow.range = 11f;
        glow.intensity = 2.4f;
        glow.shadows = LightShadows.None;
        glow.enabled = false;
    }

    private void Update()
    {
        bool has = inventory != null && inventory.HasKeyItem(ItemId);
        if (glow.enabled != has) glow.enabled = has;
        if (has) glow.intensity = 2.4f + Mathf.Sin(Time.time * 3f) * 0.25f; // a gentle twinkle
    }
}
