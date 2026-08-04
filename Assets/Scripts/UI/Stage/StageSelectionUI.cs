using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageSelectionUI : MonoBehaviour
{
    [Header("Destinations")]
    [SerializeField] private StageDestination[] _destinations =
        Array.Empty<StageDestination>();

    [Header("Input")]
    [SerializeField] private PlayerInputHandler _playerInput;
    [SerializeField] private UIInputHandler _uiInput;

    [Header("Transition (Optional)")]
    [SerializeField] private TransitionSequence _transitionSequence;

    private bool _inputStateCaptured;
    private bool _playerInputWasEnabled;
    private bool _uiInputWasEnabled;
    private bool _isTransitioning;

    public bool IsOpen => gameObject.activeSelf;

    private void OnEnable()
    {
        ResolveReferences();
        BindDestinations();

        if (_uiInput != null)
        {
            _uiInput.CancelPressed += Close;
        }

        CaptureAndApplyInputState();
        FocusFirstAvailableDestination();
    }

    private void Update()
    {
        if (_isTransitioning)
        {
            return;
        }

        // UIInputHandler.Start may run after this component's OnEnable.
        if (_playerInput != null && _playerInput.InputEnabled)
        {
            _playerInput.SetInputEnabled(false);
        }

        if (_uiInput != null && !_uiInput.InputEnabled)
        {
            _uiInput.SetInputEnabled(true);
        }
    }

    private void OnDisable()
    {
        if (_uiInput != null)
        {
            _uiInput.CancelPressed -= Close;
        }

        UnbindDestinations();
        RestoreInputState();
        _isTransitioning = false;
    }

    public void Open()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        if (_isTransitioning || !gameObject.activeSelf)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    private void SelectStage(BuildSceneReference scene)
    {
        if (_isTransitioning || scene == null)
        {
            return;
        }

        if (!scene.CanLoad())
        {
            Debug.LogError(
                $"StageSelectionUI: Build scene index {scene.BuildIndex} is not available.",
                this);
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("StageSelectionUI: SceneTransitionManager is not available.", this);
            return;
        }

        _isTransitioning = true;
        SetDestinationButtonsInteractable(false);

        if (_transitionSequence != null)
        {
            _transitionSequence.Play(() => LoadStage(scene.BuildIndex));
        }
        else
        {
            LoadStage(scene.BuildIndex);
        }
    }

    private static void LoadStage(int buildIndex)
    {
        SceneTransitionManager.Instance.LoadScene(buildIndex);
    }

    private void ResolveReferences()
    {
        if (_playerInput == null)
        {
            _playerInput = FindAnyObjectByType<PlayerInputHandler>();
        }

        if (_uiInput == null)
        {
            _uiInput = GetComponentInParent<UIInputHandler>();
        }
    }

    private void BindDestinations()
    {
        for (int i = 0; i < _destinations.Length; i++)
        {
            StageDestination destination = _destinations[i];
            if (destination == null)
            {
                continue;
            }

            destination.Bind(SelectStage);
            destination.SetInteractable(destination.Scene?.CanLoad() == true);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < _destinations.Length; i++)
        {
            _destinations[i]?.Scene?.RefreshBuildIndex();
        }
    }
#endif

    private void UnbindDestinations()
    {
        for (int i = 0; i < _destinations.Length; i++)
        {
            _destinations[i]?.Unbind();
        }
    }

    private void FocusFirstAvailableDestination()
    {
        for (int i = 0; i < _destinations.Length; i++)
        {
            Button button = _destinations[i]?.Button;
            if (button == null || !button.interactable)
            {
                continue;
            }

            EventSystem.current?.SetSelectedGameObject(button.gameObject);
            return;
        }
    }

    private void SetDestinationButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < _destinations.Length; i++)
        {
            _destinations[i]?.SetInteractable(interactable);
        }
    }

    private void CaptureAndApplyInputState()
    {
        if (!_inputStateCaptured)
        {
            _playerInputWasEnabled = _playerInput != null && _playerInput.InputEnabled;
            _uiInputWasEnabled = _uiInput != null && _uiInput.InputEnabled;
            _inputStateCaptured = true;
        }

        _playerInput?.SetInputEnabled(false);
        _uiInput?.SetInputEnabled(true);
    }

    private void RestoreInputState()
    {
        if (!_inputStateCaptured)
        {
            return;
        }

        _uiInput?.SetInputEnabled(_uiInputWasEnabled);
        _playerInput?.SetInputEnabled(_playerInputWasEnabled);
        _inputStateCaptured = false;
    }
}

[Serializable]
public sealed class StageDestination
{
    [SerializeField] private Button _button;
    [SerializeField] private BuildSceneReference _scene = new();

    [NonSerialized] private UnityAction _clickListener;

    public Button Button => _button;
    public BuildSceneReference Scene => _scene;

    public void Bind(Action<BuildSceneReference> selectStage)
    {
        Unbind();
        if (_button == null || selectStage == null)
        {
            return;
        }

        _clickListener = () => selectStage(_scene);
        _button.onClick.AddListener(_clickListener);
    }

    public void Unbind()
    {
        if (_button != null && _clickListener != null)
        {
            _button.onClick.RemoveListener(_clickListener);
        }

        _clickListener = null;
    }

    public void SetInteractable(bool interactable)
    {
        if (_button != null)
        {
            _button.interactable = interactable;
        }
    }
}
