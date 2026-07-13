using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private LayerMask _interactableLayer;

    private readonly List<IInteractable> _overlappingInteractables = new();

    public IInteractable CurrentTarget { get; private set; }
    public event Action<IInteractable> CurrentTargetChanged;

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

        if (!other.TryGetComponent<IInteractable>(out var interactable))
        {
            return;
        }

        _overlappingInteractables.Add(interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<IInteractable>(out var interactable))
        {
            return;
        }

        _overlappingInteractables.Remove(interactable);
    }

    private void UpdateCurrentTarget()
    {
        IInteractable nextTarget = FindClosestInteractable();
        if (ReferenceEquals(CurrentTarget, nextTarget))
        {
            return;
        }

        Debug.Log($"InteractionController: interact target changed by {nextTarget}");

        CurrentTarget = nextTarget;
        CurrentTargetChanged?.Invoke(CurrentTarget);
    }

    private IInteractable FindClosestInteractable()
    {
        if (_overlappingInteractables.Count == 0)
        {
            return null;
        }

        if (_overlappingInteractables.Count == 1)
        {
            IInteractable onlyTarget = _overlappingInteractables[0];
            if (IsAvailable(onlyTarget))
            {
                return onlyTarget;
            }

            _overlappingInteractables.Clear();
            return null;
        }

        IInteractable closest = null;
        float closestSqrDistance = float.PositiveInfinity;

        for (int i = _overlappingInteractables.Count - 1; i >= 0; i--)
        {
            IInteractable interactable = _overlappingInteractables[i];
            if (!IsAvailable(interactable))
            {
                _overlappingInteractables.RemoveAt(i);
                continue;
            }

            Component component = (Component)interactable;
            float sqrDistance = ((Vector2)component.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closest = interactable;
                closestSqrDistance = sqrDistance;
            }
        }

        return closest;
    }

    private static bool IsAvailable(IInteractable interactable)
    {
        return interactable is Component component
               && component != null
               && component.gameObject.activeInHierarchy;
    }
}
