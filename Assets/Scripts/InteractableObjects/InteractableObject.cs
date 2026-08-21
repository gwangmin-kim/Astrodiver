using UnityEngine;

/// <summary>
/// Base component for world objects the player can interact with.
/// </summary>
public abstract class InteractableObject : MonoBehaviour
{
    public abstract void Interact();
}
