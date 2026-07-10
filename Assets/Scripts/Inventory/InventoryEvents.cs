using System;

public static class InventoryEvents
{
    public static event Action<int, CreatureInventorySlot> CreatureSlotChanged;
    public static event Action<ResourceDefinition, int> ResourceAmountChanged;

    public static void RaiseCreatureSlotChanged(int slotIndex, CreatureInventorySlot slot)
    {
        CreatureSlotChanged?.Invoke(slotIndex, slot);
    }

    public static void RaiseResourceAmountChanged(ResourceDefinition definition, int amount)
    {
        ResourceAmountChanged?.Invoke(definition, amount);
    }
}
