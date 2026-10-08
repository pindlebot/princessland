using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tracks win/lose state and restarts the level (R). (Esc opens the PauseMenu.)
// Drawing all of this is the HUD's job.
//
// Gentle Mode (GameSession.Settings.gentle, the default) changes losing: monsters hit for
// half damage, and at 0 hearts there's no Game Over. The hero gets sleepy, the screen fades,
// and she wakes at LevelBootstrap's wake point (where she came in, or the last fountain she
// passed) with full hearts. Everything else stays as it was: coins, items, beaten monsters.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip sleepSound;   // a lullaby as she nods off
    [SerializeField] private AudioClip wakeSound;
    [SerializeField] private float sleepSeconds = 1.2f;  // lying there before the fade
    [SerializeField] private float fadeSeconds = 0.8f;
    [SerializeField] private float safeSeconds = 2.5f;   // can't be hurt right after waking

    private Health playerHealth; // set by LevelBootstrap when it spawns the player

    public bool IsGameOver { get; private set; }
    public bool PlayerWon { get; private set; }
    public bool IsAsleep { get; private set; }
    public float SleepFade { get; private set; } // 0 = clear, 1 = fully faded out (the HUD draws it)
    public int Naps { get; private set; }

    // Can the hero walk, cast and use things right now?
    public bool PlayerCanAct => !IsGameOver && !IsAsleep;

    private float halfHitCarry;

    // Gentle Mode: a half hit is waiting to cost a heart (the HUD shows a half heart).
    public bool HalfHeartLost => GameSession.Settings.gentle && halfHitCarry > 0f;

    private void Awake()
    {
        Instance = this;
    }

    public void SetPlayer(Health health)
    {
        playerHealth = health;
        playerHealth.AdjustDamage = SoftenHit;
        playerHealth.Healed += _ => halfHitCarry = 0f; // healing mends a half-broken heart too
    }

    private void Start()
    {
        playerHealth.Died += _ =>
        {
            if (GameSession.Settings.gentle) StartCoroutine(Nap());
            else EndGame(won: false);
        };
    }

    // Gentle Mode: half damage. Hearts are whole, so every other 1-damage hit costs a heart.
    private int SoftenHit(int amount)
    {
        if (!GameSession.Settings.gentle) return amount;
        halfHitCarry += amount * 0.5f;
        int whole = Mathf.FloorToInt(halfHitCarry);
        halfHitCarry -= whole;
        return whole;
    }

    private IEnumerator Nap()
    {
        IsAsleep = true;
        Naps++;
        AudioManager.Play(sleepSound);
        yield return new WaitForSeconds(sleepSeconds);
        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            SleepFade = t / fadeSeconds;
            yield return null;
        }
        SleepFade = 1f;

        // Behind the fade: back to the wake point, fully rested.
        var player = playerHealth.gameObject;
        var wake = LevelBootstrap.Current.WakePoint;
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // a CharacterController ignores direct moves while enabled
        player.transform.SetPositionAndRotation(wake.position, wake.rotation);
        if (cc != null) cc.enabled = true;
        playerHealth.Revive();
        if (player.TryGetComponent(out Mana mana)) mana.Refill();
        playerHealth.InvulnerableUntil = Time.time + safeSeconds + fadeSeconds;
        halfHitCarry = 0f;
        yield return new WaitForSeconds(0.4f);

        AudioManager.Play(wakeSound);
        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            SleepFade = 1f - t / fadeSeconds;
            yield return null;
        }
        SleepFade = 0f;
        IsAsleep = false;
    }

    public void Win() => EndGame(won: true);

    private void EndGame(bool won)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        PlayerWon = won;
        AudioManager.Play(won ? winSound : loseSound);
    }

    private void Update()
    {
        if (playerHealth != null) playerHealth.PlayDeathSound = !GameSession.Settings.gentle; // no sad tune for a nap
        if (GameInput.GameplayBlocked) return;
        // R any time; on a controller, A once the "try again?" banner is up.
        if (GameInput.RestartPressed || (IsGameOver && GameInput.ConfirmPressed))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
