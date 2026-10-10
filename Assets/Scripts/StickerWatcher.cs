using UnityEngine;

// Every second or so, checks whether you've earned a new sticker (StickerBook) and says so. Lives on the HUD object.
public class StickerWatcher : MonoBehaviour
{
    [SerializeField] private float checkEvery = 0.75f;
    [SerializeField] private AudioClip earnedSound;

    private float nextAt;

    private void Update()
    {
        if (Time.time < nextAt) return;
        nextAt = Time.time + checkEvery;
        var fresh = StickerBook.Award();
        if (fresh.Count == 0) return;
        AudioManager.Play(earnedSound);
        var hud = GetComponent<HudController>();
        if (hud == null) return;
        foreach (var sticker in fresh)
            hud.ShowToast($"New sticker: {sticker.Name}! ({StickerBook.Earned(sticker.Group)}/{StickerBook.Total(sticker.Group)})");
    }
}
