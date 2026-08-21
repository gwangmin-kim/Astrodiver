using UnityEngine;

/// <summary>
/// Base component for world objects the player can interact with.
/// </summary>
public abstract class InteractableObject : MonoBehaviour
{
    [Header("Repeat Interaction")]
    [SerializeField] protected bool _isRepeatable;
    [SerializeField, Min(0.01f)] protected float _repeatInterval = 0.5f;

    public bool IsRepeatable => _isRepeatable;
    public virtual float RepeatInterval => Mathf.Max(0.01f, _repeatInterval);
    public virtual bool CanRepeatInteract => IsRepeatable;

    public abstract void Interact();
}
