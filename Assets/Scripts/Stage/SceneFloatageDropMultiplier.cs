using UnityEngine;

/// <summary>
/// Applies a scene-specific multiplier to every catalogued floatage drop.
/// Attach this only to scenes that intentionally override normal progression.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-900)]
public sealed class SceneFloatageDropMultiplier : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _multiplier = 50f;
    [SerializeField] private FloatageDefinition[] _floatages;

    private GameDataManager _gameDataManager;
    private GameRuntimeData _appliedRuntimeData;

    private void Awake()
    {
        BindGameDataManager();
    }

    private void OnDestroy()
    {
        if (_gameDataManager != null)
        {
            _gameDataManager.RuntimeDataChanged -= HandleRuntimeDataChanged;
        }
    }

    private void BindGameDataManager()
    {
        _gameDataManager = GameDataManager.Instance;
        if (_gameDataManager == null)
        {
            Debug.LogError(
                "SceneFloatageDropMultiplier: GameDataManager is not available.",
                this);
            enabled = false;
            return;
        }

        _gameDataManager.RuntimeDataChanged += HandleRuntimeDataChanged;
        Apply(_gameDataManager.RuntimeData);
    }

    private void HandleRuntimeDataChanged(GameRuntimeData runtimeData)
    {
        Apply(runtimeData);
    }

    private void Apply(GameRuntimeData runtimeData)
    {
        if (runtimeData == null || ReferenceEquals(_appliedRuntimeData, runtimeData))
        {
            return;
        }

        _appliedRuntimeData = runtimeData;
        float multiplier = Mathf.Max(1f, _multiplier);
        if (_floatages == null)
        {
            return;
        }

        foreach (FloatageDefinition definition in _floatages)
        {
            if (definition != null)
            {
                runtimeData.FloatageDropMultipliers.Multiply(definition, multiplier);
            }
        }
    }
}
