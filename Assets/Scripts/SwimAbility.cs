using UnityEngine;

// The Bubble Charm: with it in the treasures tab the hero can swim, walking out across the water. It works by
// the hero's own physics layer (LevelMap.SwimmerLayer) no longer colliding with the invisible banks round every
// water tile (the Water layer), so the water just carries them. Water on the island's rim stays a wall (the
// WaterRim layer, made by DungeonBuilder): there's nothing out there but a long drop.
// While swimming the hero sits low in the water in a ring of ripples, and can't cast (PlayerController.IsSwimming).
// Like HopAbility, the component sits on every hero from the start and does nothing until the Charm is found.
[RequireComponent(typeof(PlayerController))]
public class SwimAbility : MonoBehaviour
{
    [SerializeField] private GameObject ripplePrefab;
    [SerializeField] private AudioClip splashSound;
    [SerializeField] private float sink = 0.28f;   // how far the hero sits down in the water

    private PlayerController player;
    private LevelMap map;
    private Transform sprite;
    private GameObject ripple;
    private bool searchedForMap;

    public bool IsSwimming => player.IsSwimming;
    public int Swims { get; private set; }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        gameObject.layer = LevelMap.SwimmerLayer;
        if (TryGetComponent(out CharacterAnimator visuals) && visuals.SpriteRenderer != null) sprite = visuals.SpriteRenderer.transform;
    }

    private void Update()
    {
        bool has = Abilities.Has(Abilities.BubbleCharm);
        Physics.IgnoreLayerCollision(LevelMap.SwimmerLayer, LevelMap.WaterLayer, has);
        if (!searchedForMap) { map = FindAnyObjectByType<LevelMap>(); searchedForMap = true; }
        bool inWater = has && map != null && !player.IsHopping && LevelMap.IsWater(map.TileAt(transform.position));
        if (inWater != player.IsSwimming) SetSwimming(inWater);
    }

    private void SetSwimming(bool swimming)
    {
        player.IsSwimming = swimming;
        if (sprite != null) sprite.localPosition += Vector3.down * (swimming ? sink : -sink);
        AudioManager.Play(splashSound, 0.7f);
        if (swimming)
        {
            Swims++;
            GameSession.AddToCounter("swims");
            if (ripplePrefab != null)
            {
                ripple = Instantiate(ripplePrefab, transform);
                ripple.transform.localPosition = new Vector3(0f, -0.9f, 0f);
                ripple.transform.localScale = Vector3.one * 1.6f;
            }
        }
        else if (ripple != null) Destroy(ripple);
    }

    private void OnDisable()
    {
        if (player != null && player.IsSwimming) SetSwimming(false);
    }
}
