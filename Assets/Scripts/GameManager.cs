using UnityEngine;
using UnityEngine.SceneManagement;

// Tracks win/lose state, restarts the level (R) or returns to the title screen (Esc).
// Drawing all of this is the HUD's job.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string titleScene = "Title";
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
        if (DialogueController.BlocksInput) return;
        if (GameInput.RestartPressed)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (GameInput.MenuPressed)
            SceneManager.LoadScene(titleScene);
    }
}
