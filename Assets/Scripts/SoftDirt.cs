using System.Collections;
using UnityEngine;

// A mound of loose soil. With the Mole Mitts, press E to dig it away; some hide something (a heart piece, a star
// shard, a handful of coins), which is waiting underneath as an inactive child until the dirt is gone. A dug mound
// stays dug: it remembers by where it stands, like a chest.
public class SoftDirt : MonoBehaviour, IInteractable
{
    public const string DugCounter = "dirt_dug";

    [SerializeField] private string persistentId;
    [Tooltip("What's buried here (an inactive child), if anything.")]
    [SerializeField] private GameObject reward;
    [SerializeField] private Collider solid;
    [SerializeField] private GameObject dust;
    [SerializeField] private AudioClip digSound;

    public bool IsDug { get; private set; }
    public Vector3 Position => transform.position;
    public string Prompt => "Dig";
    public bool CanInteract => !IsDug && Abilities.Has(Abilities.MoleMitts);

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start()
    {
        if (!string.IsNullOrEmpty(persistentId) && GameSession.IsUsed(persistentId))
        {
            IsDug = true;
            if (reward != null) reward.SetActive(true); // whatever was buried is still there if it wasn't taken
            Destroy(gameObject);
        }
    }

    public string Interact(GameObject player)
    {
        Dig();
        return null;
    }

    public void Dig()
    {
        if (IsDug) return;
        IsDug = true;
        if (!string.IsNullOrEmpty(persistentId)) GameSession.MarkUsed(persistentId);
        GameSession.AddToCounter(DugCounter);
        if (solid != null) solid.enabled = false;
        NavGrid.MarkDirty();
        AudioManager.Play(digSound);
        if (dust != null) Instantiate(dust, transform.position + Vector3.up * 0.6f, Quaternion.identity);
        if (reward != null)
        {
            reward.transform.SetParent(transform.parent, true); // so it outlives the mound
            reward.SetActive(true);
        }
        StartCoroutine(Crumble());
    }

    private IEnumerator Crumble()
    {
        var start = transform.localScale;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            transform.localScale = start * (1f - t / 0.4f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
