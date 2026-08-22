/// <summary>
/// 게임 중 발생하는 일회성 이벤트를 정의
/// </summary>
public enum GameProgressEventId
{
    None = 0,

    // tutorial guide events: 1000-1099
    ReadLetter = 1000,
    OpenUpgrader = 1001,
    UnlockBattery = 1002,
    ExploreFirstTime = 1003,
    GetFirstResource = 1004,
    ReturnSafely = 1005,
    UnlockNetgun = 1006,
    UnlockWorktable = 1007,
    CaptureFirstCreature = 1008,
    UseWorktable = 1009,
}
