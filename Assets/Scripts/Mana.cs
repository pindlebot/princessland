using UnityEngine;

// A regenerating resource that abilities spend. Float, so regen is smooth;
// the HUD rounds it for display.
public class Mana : MonoBehaviour
{
    [SerializeField] private float max = 50f;
    [SerializeField] private float regenPerSecond = 8f;

    public float Current { get; private set; }
    public float Max => max;

    private void Awake()
    {
        Current = max;
    }

    private void Update()
    {
        Current = Mathf.Min(max, Current + regenPerSecond * Time.deltaTime);
    }

    public void Refill() => Current = max;

    public void SetMax(float newMax)
    {
        max = Mathf.Max(1f, newMax);
        Current = Mathf.Min(Current, max);
    }

    public bool CanAfford(float amount) => Current >= amount;

    // Spends only if there's enough; returns whether it did.
    public bool TrySpend(float amount)
    {
        if (!CanAfford(amount)) return false;
        Current -= amount;
        return true;
    }
}
