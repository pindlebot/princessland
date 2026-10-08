using UnityEngine;
using UnityEngine.SceneManagement;

// Tracks win/lose state and restarts the level (R). (Esc opens the PauseMenu.)
// Drawing all of this is the HUD's job.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;

    private Health playerHealth; // set by LevelBootstrap when it spawns the player

    public bool IsGameOver { get; private set; }
    public bool PlayerWon { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SetPlayer(Health health) => playerHealth = health;

    private void Start()
    {
        playerHealth.Died += _ => EndGame(won: false);
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
        if (GameInput.GameplayBlocked) return;
        if (GameInput.RestartPressed)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
