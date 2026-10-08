using UnityEngine;

// The bed, toilet, sink and paper towel in the hero's home. One data-driven component
// covers all of them: each instance just sets its prompt, message, sound and effect.
public class HouseFixture : MonoBehaviour, IInteractable
{
    public enum Effect { None, Rest, WashHands, DryHands }

    private const string WetHandsFlag = "wetHands";

    [SerializeField] private string prompt = "Use";
    [TextArea] [SerializeField] private string message;
    [SerializeField] private Effect effect;
    [SerializeField] private AudioClip sound;

    public Effect Kind => effect;
    public Vector3 Position => transform.position;
    public string Prompt => prompt;
    public bool CanInteract => true;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    public string Interact(GameObject player)
    {
        AudioManager.Play(sound);
        switch (effect)
        {
            case Effect.Rest:
                var health = player.GetComponent<Health>();
                health.Heal(health.Max);
                player.GetComponent<Mana>().Refill();
                break;

            case Effect.WashHands:
                GameSession.Flags.Add(WetHandsFlag);
                break;

            case Effect.DryHands:
                // Remove() returns false if the flag wasn't there: nobody washed first.
                if (!GameSession.Flags.Remove(WetHandsFlag))
                    return "Your hands are already dry. Did you wash them?";
                break;
        }
        return message;
    }
}
