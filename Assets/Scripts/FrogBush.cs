using UnityEngine;

// Easter egg: one bush on the castle grounds rustles when you press E, and out hops
// Sir Hopsalot, the frog Coralie the mermaid has been looking for. Sets "found:frog",
// which changes what Coralie says. Once found, he stays out (even after a save and load).
public class FrogBush : MonoBehaviour, IInteractable
{
    public const string FoundFlag = "found:frog";

    [SerializeField] private GameObject frogPrefab;
    [SerializeField] private AudioClip rustleSound;

    public Vector3 Position => transform.position;
    public string Prompt => "Rustle the bush";
    public bool CanInteract => !GameSession.Flags.Contains(FoundFlag);

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start()
    {
        if (GameSession.Flags.Contains(FoundFlag)) Release(hopOut: false);
    }

    public string Interact(GameObject player)
    {
        GameSession.Flags.Add(FoundFlag);
        AudioManager.Play(rustleSound);
        Release(hopOut: true);
        return "Ribbit! A little frog with a tiny crown hops out of the bush!";
    }

    private void Release(bool hopOut)
    {
        // He comes out on the side facing the camera, so you can see him.
        var frog = Instantiate(frogPrefab, transform.position + new Vector3(-1f, 0f, -1.4f), Quaternion.identity);
        if (hopOut) frog.GetComponent<Frog>().HopNow();
    }
}
