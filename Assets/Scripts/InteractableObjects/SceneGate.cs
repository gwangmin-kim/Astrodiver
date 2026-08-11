using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneGate : MonoBehaviour, IInteractable
{
    [Header("Hub Stage Selection")]
    [SerializeField] private StageSelectionUI _stageSelectionUI;

    [Header("Direct Scene Transition (Optional)")]
    [SerializeField] private string _destinationSceneName;
    [SerializeField] private TransitionSequence _transitionSequence;

    [Header("Exploration Return")]
    [SerializeField] private bool _finishExploreSession;

    private bool _isTransitioning;

    public void Interact()
    {
        if (_isTransitioning)
        {
            return;
        }

        if (_finishExploreSession)
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.FinishSessionByReturn();
            }
            else
            {
                Debug.LogError("SceneGate: Explore session gate requires a SessionManager in the scene.", this);
            }

            return;
        }

        if (IsHubGateLocked())
        {
            HandleFirstUpgradeLocked();
            return;
        }

        if (_stageSelectionUI != null)
        {
            _stageSelectionUI.Open();
            return;
        }

        if (string.IsNullOrWhiteSpace(_destinationSceneName))
        {
            return;
        }

        _isTransitioning = true;

        if (_transitionSequence != null)
        {
            _transitionSequence.Play(LoadDestinationScene);
        }
        else
        {
            LoadDestinationScene();
        }
    }

    private void LoadDestinationScene()
    {
        SceneTransitionManager.Instance.LoadScene(_destinationSceneName);
    }

    private bool IsHubGateLocked()
    {
        return gameObject.scene.name == "Hub" &&
               (GameDataManager.Instance == null ||
                !GameDataManager.Instance.IsEventCompleted(
                    GameProgressEventId.RootUpgradeUnlocked));
    }

    private void HandleFirstUpgradeLocked()
    {
        Debug.Log("첫 번째 업그레이드 잠금을 해제하세요.", this);
    }

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
        {
            gameObject.layer = interactableLayer;
        }
    }
}
