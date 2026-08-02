using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageSpawnedObject : MonoBehaviour
{
    private StagePopulationManager _owner;

    public string StageId { get; private set; }
    public string EntryId { get; private set; }
    public StageSpawnCategory Category { get; private set; }
    public int AreaIndex { get; private set; }
    public bool IsRemovedFromStage { get; private set; }

    public void Initialize(
        StagePopulationManager owner,
        string stageId,
        string entryId,
        StageSpawnCategory category,
        int areaIndex)
    {
        if (_owner != null && !IsRemovedFromStage)
        {
            _owner.Unregister(this);
        }

        _owner = owner;
        StageId = stageId;
        EntryId = entryId;
        Category = category;
        AreaIndex = areaIndex;
        IsRemovedFromStage = false;
        _owner?.Register(this);
    }

    public void NotifyRemovedFromStage()
    {
        if (IsRemovedFromStage)
        {
            return;
        }

        IsRemovedFromStage = true;
        _owner?.Unregister(this);
    }

    private void OnDestroy()
    {
        NotifyRemovedFromStage();
    }
}
