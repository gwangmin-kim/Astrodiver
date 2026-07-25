using System;

[Serializable]
public sealed class PlayerStatsSaveData
{
    public bool movementInitialized;
    public PlayerMovementData movement;
    public bool batteryInitialized;
    public BatteryData battery;
    public bool magnetInitialized;
    public MagnetData magnet;
}
