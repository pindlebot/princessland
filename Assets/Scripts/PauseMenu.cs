using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// The pause menu (Esc or Start): keep playing, music and sound volume, Gentle/Adventurer
// mode, "save and go to the title screen" and "save and quit". Time stops while it's open.
//
// Every row can be clicked, and the whole menu also works from the keyboard or a gamepad:
// up/down picks a row, left/right changes a setting, Enter/A presses a button, and Esc,
// Start or B close the menu again. The rows live in Hud.uxml under "pause-screen".
[RequireComponent(typeof(UIDocument))]
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private string titleScene = "Title";
    [SerializeField] private AudioClip clickSound;

    private enum Row { Resume, Music, Sounds, Mode, Quit, Exit }
    private static readonly Row[] Rows = { Row.Resume, Row.Music, Row.Sounds, Row.Mode, Row.Quit, Row.Exit };

    public static bool IsOpen { get; private set; }
    public int Highlighted { get; private set; }

    private VisualElement screen;
    private VisualElement[] rows;
    private VisualElement musicValue, soundValue;
    private Label modeValue, modeDescription;
    private int openedFrame = -1;

    private static GameSettings Settings => GameSession.Settings;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        screen = root.Q("pause-screen");
        rows = new VisualElement[Rows.Length];
        for (int i = 0; i < Rows.Length; i++)
        {
            int index = i;
            rows[i] = root.Q($"pause-{Rows[i].ToString().ToLowerInvariant()}");
            rows[i].RegisterCallback<PointerEnterEvent>(_ => Highlight(index));
        }
        musicValue = root.Q("pause-music-value");
        soundValue = root.Q("pause-sounds-value");
        modeValue = root.Q<Label>("pause-mode-value");
        modeDescription = root.Q<Label>("pause-mode-description");

        root.Q<Button>("pause-resume").clicked += Close;
        root.Q<Button>("pause-quit").clicked += QuitToTitle;
        root.Q<Button>("pause-exit").clicked += QuitGame;
        foreach (var row in new[] { Row.Music, Row.Sounds, Row.Mode })
        {
            var r = row;
            var element = rows[(int)row];
            element.Q<Button>(className: "pause-less").clicked += () => Change(r, -1);
            element.Q<Button>(className: "pause-more").clicked += () => Change(r, +1);
        }
        SetOpen(false);
    }

    private void OnDestroy()
    {
        // Leaving the scene with the menu open must not leave the next scene frozen.
        if (IsOpen) Time.timeScale = 1f;
        IsOpen = false;
    }

    private void Update()
    {
        if (!IsOpen)
        {
            if (GameInput.MenuPressed && !DialogueController.BlocksInput && !CookingView.BlocksInput) Open();
            return;
        }
        if (Time.frameCount == openedFrame) return; // the press that opened it
        if (GameInput.MenuPressed || GameInput.BackPressed) { Close(); return; }
        if (GameInput.UpPressed) Highlight(Highlighted - 1, sound: true);
        if (GameInput.DownPressed) Highlight(Highlighted + 1, sound: true);

        var row = Rows[Highlighted];
        if (GameInput.LeftPressed) Change(row, -1);
        if (GameInput.RightPressed) Change(row, +1);
        if (GameInput.ConfirmPressed)
        {
            if (row == Row.Resume) Close();
            else if (row == Row.Quit) QuitToTitle();
            else if (row == Row.Exit) QuitGame();
            else if (row == Row.Mode) Change(row, +1); // A flips the mode too
        }
    }

    public void Open()
    {
        openedFrame = Time.frameCount;
        SetOpen(true);
        Highlight(0);
        AudioManager.Play(clickSound, 0.6f);
    }

    public void Close()
    {
        if (!IsOpen) return;
        SetOpen(false);
        AudioManager.Play(clickSound, 0.6f);
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        Time.timeScale = open ? 0f : 1f;
        screen.EnableInClassList("open", open);
        if (open) Refresh();
    }

    public void Highlight(int index, bool sound = false)
    {
        index = Mathf.Clamp(index, 0, Rows.Length - 1);
        if (sound && index != Highlighted) AudioManager.Play(clickSound, 0.4f);
        Highlighted = index;
        for (int i = 0; i < rows.Length; i++)
            rows[i].EnableInClassList("selected", i == Highlighted);
    }

    // Left/right (or the arrow buttons) on a setting row.
    private void Change(Row row, int step)
    {
        switch (row)
        {
            case Row.Music:
                Settings.musicVolume = Mathf.Clamp(Settings.musicVolume + step, 0, GameSettings.VolumeSteps);
                break;
            case Row.Sounds:
                Settings.soundVolume = Mathf.Clamp(Settings.soundVolume + step, 0, GameSettings.VolumeSteps);
                break;
            case Row.Mode:
                Settings.gentle = !Settings.gentle; // only two modes: either arrow flips it
                break;
            default:
                return;
        }
        AudioManager.Play(clickSound, 0.6f); // played after the change, so you hear the new volume
        Refresh();
    }

    private void Refresh()
    {
        ShowVolume(musicValue, Settings.musicVolume);
        ShowVolume(soundValue, Settings.soundVolume);
        modeValue.text = Settings.ModeName;
        modeDescription.text = Settings.ModeDescription;
    }

    // A volume as a row of dots, filled up to the value (like the objective pips).
    private static void ShowVolume(VisualElement dots, int value)
    {
        while (dots.childCount < GameSettings.VolumeSteps)
        {
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("pause-dot");
            dots.Add(dot);
        }
        for (int i = 0; i < dots.childCount; i++)
            dots[i].EnableInClassList("on", i < value);
    }

    // Save (arriving where you came into this place) and go back to the title screen.
    public void QuitToTitle()
    {
        SaveSystem.Autosave(SceneManager.GetActiveScene().name, GameSession.EnteredBy);
        SetOpen(false);
        SceneManager.LoadScene(titleScene);
    }

    // Save the same way, then close the game.
    public void QuitGame()
    {
        SaveSystem.Autosave(SceneManager.GetActiveScene().name, GameSession.EnteredBy);
        SetOpen(false);
        AppQuit.Quit();
    }
}
