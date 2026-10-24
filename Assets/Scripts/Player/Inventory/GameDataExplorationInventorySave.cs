using Astrodiver.Persistence;

namespace Astrodiver.Player.Inventory
{
    internal sealed class GameDataExplorationInventorySave : IExplorationInventorySave
    {
        private readonly GameDataManager _manager;

        public GameDataExplorationInventorySave(GameDataManager manager)
        {
            _manager = manager;
        }

        public bool HasUnsavedChanges => _manager.HasUnsavedChanges;
        public void MarkDirty() => _manager.MarkDirty();
        public void RestoreDirtyState(bool wasDirty) => _manager.RestoreDirtyState(wasDirty);
        public bool SaveNow() => _manager.SaveNow();
        public bool SaveExplorationResult() => _manager.SaveExplorationResult();
        public bool BeginSaveSuspension() => _manager.BeginSaveSuspension();
        public void EndSaveSuspension() => _manager.EndSaveSuspension();
    }
}
