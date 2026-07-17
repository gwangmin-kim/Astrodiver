using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneGate : MonoBehaviour, IInteractable
{
    [SerializeField] private string _destinationSceneName;
    [SerializeField] private TransitionSequence _transitionSequence;
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
