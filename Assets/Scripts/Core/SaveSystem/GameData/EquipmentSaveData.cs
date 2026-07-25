using System;

[Serializable]
public sealed class EquipmentSaveData
{
    public bool netGunInitialized;
    public NetGunData netGun;
    public bool plasmaGunInitialized;
    public PlasmaGunData plasmaGun;
}
