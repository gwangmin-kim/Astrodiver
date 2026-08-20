using UnityEngine;

/// <summary>
/// Mirrors PlayerInteractionController's selected target as an outline effect.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInteractionController))]
public sealed class PlayerInteractionOutlineHighlighter : MonoBehaviour
{
    [SerializeField] private PlayerInteractionController _interactionController;

    [Header("Outline Appearance")]
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Min(0f)] private float _outlineWidth = 2f;

    private InteractableOutline _highlightedOutline;

    private void Awake()
    {
        if (_interactionController == null)
        {
            _interactionController = GetComponent<PlayerInteractionController>();
        }
    }

    private void OnEnable()
    {
        if (_interactionController == null)
        {
            return;
        }

        _interactionController.CurrentTargetChanged += HandleCurrentTargetChanged;
        HandleCurrentTargetChanged(_interactionController.CurrentTarget);
    }

    private void OnDisable()
    {
        if (_interactionController != null)
        {
            _interactionController.CurrentTargetChanged -= HandleCurrentTargetChanged;
        }

        SetHighlightedOutline(null);
    }

    private void HandleCurrentTargetChanged(IInteractable target)
    {
        InteractableOutline nextOutline = null;
        if (target is Component component)
        {
            nextOutline = component.GetComponent<InteractableOutline>();
            if (nextOutline == null)
            {
                nextOutline = component.GetComponentInParent<InteractableOutline>();
            }
        }

        SetHighlightedOutline(nextOutline);
    }

    private void SetHighlightedOutline(InteractableOutline nextOutline)
    {
        if (_highlightedOutline == nextOutline)
        {
            return;
        }

        if (_highlightedOutline != null)
        {
            _highlightedOutline.SetHighlighted(false, Color.clear, 0f);
        }

        _highlightedOutline = nextOutline;

        if (_highlightedOutline != null)
        {
            _highlightedOutline.SetHighlighted(true, _outlineColor, _outlineWidth);
        }
    }
}
