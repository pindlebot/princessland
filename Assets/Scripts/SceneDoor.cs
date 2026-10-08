using UnityEngine;
using UnityEngine.SceneManagement;

// A door you press E on to go to another scene: the castle gate (into the hero's home)
// and the home's front door (back out to the castle grounds). targetSpawn names the
// spawn point to arrive at on the other side.
public class SceneDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Open the door";
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawn;
    [SerializeField] private AudioClip openSound;

    public string TargetScene => targetScene;
    public Vector3 Position => transform.position;
    public string Prompt => prompt;
    public bool CanInteract => !used;

    private bool used;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    public string Interact(GameObject player)
    {
        used = true;
        GameSession.NextSpawn = targetSpawn;
        SaveSystem.Autosave(targetScene, targetSpawn); // every door is a save point
        AudioManager.Play(openSound);
        SceneManager.LoadScene(targetScene);
        return null;
    }
}
