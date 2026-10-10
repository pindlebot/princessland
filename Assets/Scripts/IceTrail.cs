using UnityEngine;

// An ice slime: wherever it slides it leaves a patch of ice (IceZone), so the ground around it turns slippery for the hero.
// A few patches at a time; they melt on their own.
[RequireComponent(typeof(EnemyAI))]
public class IceTrail : MonoBehaviour
{
    [SerializeField] private GameObject patchPrefab;
    [SerializeField] private float every = 0.7f;
    [SerializeField] private float minStep = 0.6f;   // only when it's actually moved this far

    private EnemyAI ai;
    private Vector3 last;
    private float nextAt;

    public int Patches { get; private set; }

    private void Awake() => ai = GetComponent<EnemyAI>();
    private void Start() => last = transform.position;

    private void Update()
    {
        if (!ai.enabled || patchPrefab == null || Time.time < nextAt) return;
        var moved = transform.position - last;
        moved.y = 0f;
        if (moved.magnitude < minStep) return;
        last = transform.position;
        nextAt = Time.time + every;
        var patch = Instantiate(patchPrefab, new Vector3(transform.position.x, 0.03f, transform.position.z), Quaternion.Euler(90f, 0f, 0f));
        patch.SetActive(true);
        Patches++;
    }
}
