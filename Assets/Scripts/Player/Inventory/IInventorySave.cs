namespace Astrodiver.Player.Inventory
{
    /// <summary>
    /// 인벤토리 변경의 저장과 실패 복원에 필요한 기능
    /// </summary>
    public interface IInventorySave
    {
        public bool HasUnsavedChanges { get; }
        public void MarkDirty();
        public void RestoreDirtyState(bool wasDirty);
        public bool SaveNow();
    }
}
