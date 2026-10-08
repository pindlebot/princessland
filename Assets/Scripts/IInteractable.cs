using System.Collections.Generic;
using UnityEngine;

// Anything the player can walk up to and press E on (chests now; doors, levers, shrines later).
// An interface lets PlayerInteractor work with all of them without knowing their types.
public interface IInteractable
{
    Vector3 Position { get; }
    string Prompt { get; }      // shown in the HUD, e.g. "Open chest"
    bool CanInteract { get; }   // false once a chest is open, a lever is pulled, ...

    // Does the thing; returns a message for the HUD (or null for none).
    string Interact(GameObject player);
}

// Unity can't search the scene for an interface type, so interactables
// register themselves here (in OnEnable/OnDisable) instead.
public static class Interactables
{
    private static readonly List<IInteractable> all = new List<IInteractable>();
    public static IReadOnlyList<IInteractable> All => all;

    public static void Register(IInteractable i) => all.Add(i);
    public static void Unregister(IInteractable i) => all.Remove(i);
}
