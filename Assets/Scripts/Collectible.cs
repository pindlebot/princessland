using UnityEngine;

// A small treasure you collect by walking into it, found behind gaps and brambles: a Heart Piece
// (four make an extra heart) or a Star Shard (collected for the wardrobe at home, later). It counts
// itself in GameSession's counters, which are saved, and remembers being taken by where it stands.
public class Collectible : MonoBehaviour
{
    public enum Kind { HeartPiece, StarShard }

    public const string HeartPieceCounter = "heart_pieces";
    public const string StarShardCounter = "star_shards";
    public const int PiecesPerHeart = 4;

    [SerializeField] private Kind kind;
    [SerializeField] private string persistentId;
    [SerializeField] private Transform visual;      // spins and bobs
    [SerializeField] private GameObject sparkle;
    [SerializeField] private AudioClip collectSound;

    private Vector3 visualStart;
    private bool collected;

    public Kind Type => kind;
    public static int HeartPieces => GameSession.GetCounter(HeartPieceCounter);
    public static int StarShards => GameSession.GetCounter(StarShardCounter);
    // Every four pieces is a whole extra heart (PlayerProgression adds this to her maximum).
    public static int BonusHearts => HeartPieces / PiecesPerHeart;

    private void Start()
    {
        visualStart = visual.localPosition;
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId))
        {
            collected = true;
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (collected) return;
        visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.1f);

        var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
        if (player == null) return;
        var to = player.transform.position - transform.position;
        to.y = 0f;
        if (to.magnitude < 1f) Collect();
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        AudioManager.Play(collectSound);
        if (sparkle != null) Instantiate(sparkle, transform.position + Vector3.up * 0.8f, Quaternion.identity);

        string message;
        if (kind == Kind.HeartPiece)
        {
            int before = BonusHearts;
            int pieces = GameSession.AddToCounter(HeartPieceCounter);
            message = BonusHearts > before ? "Heart Piece! That's four: you have a whole new heart!"
                                           : $"Heart Piece! ({pieces % PiecesPerHeart}/{PiecesPerHeart})";
            var player = LevelBootstrap.Current != null ? LevelBootstrap.Current.Player : null;
            var progression = player != null ? player.GetComponent<PlayerProgression>() : null;
            if (progression != null) progression.Refresh(heal: BonusHearts > before); // a whole new heart shows (and heals) straight away
        }
        else
        {
            message = $"Star Shard! You have {GameSession.AddToCounter(StarShardCounter)}.";
        }
        var hud = FindAnyObjectByType<HudController>();
        if (hud != null) hud.ShowToast(message);
        Destroy(gameObject);
    }
}
