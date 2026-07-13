using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneGate : MonoBehaviour, IInteractable
{
    [SerializeField] private string _destinationSceneName;
    [SerializeField] private TransitionSequence _transitionSequence;

    private bool _isTransitioning;

    public void Interact()
    {
        if (_isTransitioning || string.IsNullOrWhiteSpace(_destinationSceneName))
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
