using UnityEngine;

/// <summary>Connects a map's destruction events to the existing resource collection flow.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(StageMap))]
public sealed class MiningFragmentDrops : MonoBehaviour
{
    private const float DropRadius = 0.15f;
    private StageMap _map;

    private void OnEnable()
    {
        _map = GetComponent<StageMap>();
        _map.MiningCellDestroyed += HandleCellDestroyed;
    }

    private void OnDisable()
    {
        if (_map != null) _map.MiningCellDestroyed -= HandleCellDestroyed;
        _map = null;
    }

    private void HandleCellDestroyed(StageMapMiningCellDestroyed destroyed)
    {
        if (!Application.isPlaying) return;
        FragmentParticleManager fragments = FragmentParticleManager.Instance;
        if (fragments == null)
        {
            Debug.LogError("Mining drops require a FragmentParticleManager in the session.", this);
            return;
        }

        fragments.DropFragment(destroyed.WorldPosition, destroyed.Definition.DropResource,
            DropRadius, destroyed.Definition.BaseDropAmount);
    }
}
