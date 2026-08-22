using UnityEngine;

/// <summary>
/// Opens the tutorial document when the player interacts with the hub whiteboard.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class WhiteboardTutorialInteractable : InteractableObject
{
    [SerializeField] private TutorialDocumentView _tutorialDocument;

    private void Awake()
    {
        _isRepeatable = false;
    }

    public override void Interact()
    {
        _tutorialDocument?.Open();

        GameDataManager gameData = GameDataManager.Instance;
        if (gameData != null && gameData.CompleteEvent(GameProgressEventId.ReadLetter))
        {
            gameData.SaveNow();
        }
    }

    private void Reset()
    {
        _isRepeatable = false;
        Collider2D interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider != null)
        {
            interactionCollider.isTrigger = true;
        }

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
        {
            gameObject.layer = interactableLayer;
        }
    }
}
