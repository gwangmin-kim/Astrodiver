using Astrodiver.Equipment.NetGun;
using Astrodiver.Equipment.PlasmaGun;
using System;

namespace Astrodiver.Persistence.GameData
{
    [Serializable]
    public sealed class EquipmentRuntimeData
    {
        public NetGunData netGun;
        public PlasmaGunData plasmaGun;
    }
}
