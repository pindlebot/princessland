using UnityEngine;

// What an enemy is worth when it dies: experience for the hero, and a burst of gold coins.
// Separate from EnemyAI so anything with Health could drop loot (a breakable pot, say).
[RequireComponent(typeof(Health))]
public class Loot : MonoBehaviour
{
    [SerializeField] private int experience = 15;
    [SerializeField] private CoinPickup coinPrefab;
    [SerializeField] private int minCoins = 2;
    [SerializeField] private int maxCoins = 4;
    [SerializeField] private int minCoinValue = 1;
    [SerializeField] private int maxCoinValue = 3;

    public int Experience => experience;

    private void Awake()
    {
        GetComponent<Health>().Died += _ => Drop();
    }

    private void Drop()
    {
        GameSession.Progress.AddXp(experience);
        if (coinPrefab == null) return;

        int coins = Random.Range(minCoins, maxCoins + 1); // Range's max is exclusive for ints
        Vector3 ground = new Vector3(transform.position.x, 0.3f, transform.position.z);
        for (int i = 0; i < coins; i++)
        {
            var coin = Instantiate(coinPrefab, ground, Quaternion.identity);
            coin.Value = Random.Range(minCoinValue, maxCoinValue + 1);
        }
    }
}
