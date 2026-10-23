using Astrodiver.Equipment.NetGun;
using Astrodiver.Equipment.PlasmaGun;

namespace Astrodiver.Persistence.GameData
{
    using System;

    [Serializable]
    public sealed class EquipmentRuntimeData
    {
        public NetGunData netGun;
        public PlasmaGunData plasmaGun;
    }
}
