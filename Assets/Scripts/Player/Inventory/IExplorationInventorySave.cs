namespace Astrodiver.Player.Inventory
{
    /// <summary>
    /// 탐사 결산 시 인벤토리 저장을 위한 인터페이스
    /// </summary>
    public interface IExplorationInventorySave : IInventorySave
    {
        public bool SaveExplorationResult();
        public bool BeginSaveSuspension();
        public void EndSaveSuspension();
    }
}
