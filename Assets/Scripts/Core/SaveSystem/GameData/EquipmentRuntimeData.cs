using System;

[Serializable]
public sealed class EquipmentRuntimeData
{
    public bool netGunInitialized;
    public NetGunData netGun;
    public bool plasmaGunInitialized;
    public PlasmaGunData plasmaGun;
}
