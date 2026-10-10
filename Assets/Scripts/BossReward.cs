using UnityEngine;
using UnityEngine.SceneManagement;

// A prize that stays out of sight until the level's boss is beaten (Mother Mushroom's Fairy Lantern, the
// Slime King's Amethyst and Bouncy Boots), or until every brazier is lit, then appears with a twinkle. A
// map marks one with "item <id> hidden" (boss) or "item <id> hidden braziers". Come back later and
// it's there already if the level was cleared (the pickup itself remembers if you took it).
public class BossReward : MonoBehaviour
{
    [SerializeField] private GameObject reward;
    [SerializeField] private GameObject sparkle;
    [SerializeField] private AudioClip appearSound;
    [Tooltip("Wait for every brazier on the level to be lit, instead of the boss.")]
    [SerializeField] private bool byBraziers;

    public bool IsShown => reward.activeSelf;

    private void Start()
    {
        bool unlocked = byBraziers ? GameSession.Flags.Contains(Brazier.DoneFlag(SceneManager.GetActiveScene().name))
                                   : GameSession.Flags.Contains(LevelBootstrap.ClearedFlag);
        var boss = byBraziers ? null : FindAnyObjectByType<BossAbilities>();
        reward.SetActive(unlocked || (!byBraziers && boss == null));
        if (unlocked) return;
        if (byBraziers) Brazier.AllLit += Appear;
        else if (boss != null) boss.Health.Died += _ => Appear();
    }

    private void OnDestroy() => Brazier.AllLit -= Appear;

    private void Appear()
    {
        if (reward == null || reward.activeSelf) return;
        reward.SetActive(true);
        AudioManager.Play(appearSound);
        if (sparkle != null) Instantiate(sparkle, reward.transform.position + Vector3.up, Quaternion.identity);
    }
}
