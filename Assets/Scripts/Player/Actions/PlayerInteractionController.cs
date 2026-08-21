using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private LayerMask _interactableLayer;
    [SerializeField] private InteractionPromptSprite _interactionPrompt;

    private readonly List<InteractableObject> _overlappingInteractables = new();

    public InteractableObject CurrentTarget { get; private set; }
    public event Action<InteractableObject> CurrentTargetChanged;

    private void Awake()
    {
        if (_inputHandler == null)
        {
            _inputHandler = GetComponent<PlayerInputHandler>();
        }

        if (_interactableLayer.value == 0)
        {
            _interactableLayer = LayerMask.GetMask("Interactable");
        }

        if (_interactionPrompt == null)
        {
            _interactionPrompt = GetComponentInChildren<InteractionPromptSprite>(true);
        }

        if (_interactionPrompt != null) _interactionPrompt.Hide();
    }

    private void Update()
    {
        UpdateCurrentTarget();

        if (_inputHandler.ConsumeInteractInput())
        {
            CurrentTarget?.Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((_interactableLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (!other.TryGetComponent<InteractableObject>(out var interactable))
        {
            return;
        }

        _overlappingInteractables.Add(interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<InteractableObject>(out var interactable))
        {
            return;
        }

        _overlappingInteractables.Remove(interactable);
    }

    private void UpdateCurrentTarget()
    {
        InteractableObject nextTarget = FindClosestInteractable();
        if (CurrentTarget == nextTarget)
        {
            return;
        }

        Debug.Log($"InteractionController: interact target changed by {nextTarget}");

        CurrentTarget = nextTarget;

        if (_interactionPrompt != null)
        {
            if (CurrentTarget == null)
            {
                _interactionPrompt.Hide();
            }
            else
            {
                _interactionPrompt.Show();
            }
        }

        CurrentTargetChanged?.Invoke(CurrentTarget);
    }

    private InteractableObject FindClosestInteractable()
    {
        if (_overlappingInteractables.Count == 0)
        {
            return null;
        }

        if (_overlappingInteractables.Count == 1)
        {
            InteractableObject onlyTarget = _overlappingInteractables[0];
            if (IsAvailable(onlyTarget))
            {
                return onlyTarget;
            }

            _overlappingInteractables.Clear();
            return null;
        }

        InteractableObject closest = null;
        float closestSqrDistance = float.PositiveInfinity;

        for (int i = _overlappingInteractables.Count - 1; i >= 0; i--)
        {
            InteractableObject interactable = _overlappingInteractables[i];
            if (!IsAvailable(interactable))
            {
                _overlappingInteractables.RemoveAt(i);
                continue;
            }

            float sqrDistance = ((Vector2)interactable.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closest = interactable;
                closestSqrDistance = sqrDistance;
            }
        }

        return closest;
    }

    private static bool IsAvailable(InteractableObject interactable)
    {
        return interactable != null
               && interactable.isActiveAndEnabled
               && interactable.gameObject.activeInHierarchy;
    }
}
