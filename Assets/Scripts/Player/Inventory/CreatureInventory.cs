using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrodiver.Player.Inventory
{
    /// <summary>
    /// 인벤토리 보관 규칙
    /// </summary>
    public sealed class CreatureInventory
    {
        private CreatureInventorySlot[] _creatureSlots = Array.Empty<CreatureInventorySlot>();
        public IReadOnlyList<CreatureInventorySlot> Slots { get; private set; } = Array.Empty<CreatureInventorySlot>();
        public int Capacity => _creatureSlots.Length;

        public void Reset(int capacity)
        {
            _creatureSlots = CreateEmptySlots(capacity);
            Slots = Array.AsReadOnly(_creatureSlots);
        }

        public void Clear()
        {
            _creatureSlots = Array.Empty<CreatureInventorySlot>();
            Slots = Array.AsReadOnly(_creatureSlots);
        }

        public bool CanAdd(string id, int maxStackCount) => FindCreatureSlot(_creatureSlots, id, maxStackCount) >= 0;

        public bool TryAdd(string id, int maxStackCount)
        {
            int index = FindCreatureSlot(_creatureSlots, id, maxStackCount);
            if (index < 0)
            {
                return false;
            }
            CreatureInventorySlot slot = _creatureSlots[index];
            slot.Set(id, slot.IsEmpty ? 1 : slot.Count + 1);
            return true;
        }

        /// <summary>
        /// 비어있지 않은 첫 번째 슬롯 탐색
        /// </summary>
        /// <param name="id">탐색한 대상의 id</param>
        /// <returns>탐색이 성공한 경우 true</returns>
        public bool TryPeekFirst(out string id)
        {
            foreach (CreatureInventorySlot slot in _creatureSlots)
            {
                if (slot.IsEmpty)
                {
                    continue;
                }
                id = slot.DefinitionId;
                return true;
            }
            id = null;
            return false;
        }

        /// <summary>
        /// 비어있지 않은 첫 번째 슬롯에서 하나를 제거
        /// </summary>
        /// <param name="expectedId">제거될 대상의 id</param>
        /// <returns>실제로 제거된 경우 true</returns>
        public bool TryRemoveFirst(string expectedId)
        {
            if (!TryPeekFirst(out string id) || !string.Equals(id, expectedId, StringComparison.Ordinal))
            {
                return false;
            }
            for (int i = 0; i < Capacity; i++)
            {
                CreatureInventorySlot slot = _creatureSlots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }
                slot.Set(slot.DefinitionId, slot.Count - 1);
                CompactCreatureSlots(i);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 저장된 생물 수량을 빈 슬롯에 복원<br/>
        /// 앞에서부터 빈 슬롯만 찾아 최대 스택 수만큼 채움
        /// </summary>
        /// <param name="id">채울 대상</param>
        /// <param name="count">채울 수량</param>
        /// <param name="maxStackCount">대상의 최대 스택 수량</param>
        /// <returns>공간이 부족해 넣지 못한 수량</returns>
        public int RestoreEntry(string id, int count, int maxStackCount)
        {
            int remaining = count;
            foreach (CreatureInventorySlot slot in _creatureSlots)
            {
                if (remaining <= 0)
                {
                    break;
                }
                if (!slot.IsEmpty)
                {
                    continue;
                }
                int amount = Mathf.Min(remaining, maxStackCount);
                slot.Set(id, amount);
                remaining -= amount;
            }
            return remaining;
        }

        /// <summary>
        /// 인벤토리의 슬롯 개수 재설정<br/>
        /// 새 슬롯을 만들기 때문에 기존 슬롯 정보가 모두 초기화됨
        /// </summary>
        /// <param name="capacity"></param>
        public void Resize(int capacity)
        {
            Array.Resize(ref _creatureSlots, Mathf.Max(1, capacity));
            for (int i = 0; i < Capacity; i++)
            {
                _creatureSlots[i] ??= new CreatureInventorySlot();
            }
            Slots = Array.AsReadOnly(_creatureSlots);
        }

        private static int FindCreatureSlot(
            IReadOnlyList<CreatureInventorySlot> slots,
            string creatureId,
            int maxStackCount)
        {
            // 일치하면서 자리가 남는 슬롯이 있는지 검사
            for (int i = 0; i < slots.Count; i++)
            {
                CreatureInventorySlot slot = slots[i];
                if (slot != null && slot.Matches(creatureId) &&
                    slot.Count < maxStackCount)
                {
                    return i;
                }
            }

            // 빈 슬롯이 있는지 검사
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null || slots[i].IsEmpty)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 지정한 위치부터 비어있지 않은 슬롯을 순서대로 앞으로 옮김<br/>
        /// 슬롯 순서 및 수량은 유지되며, 스택끼리 합치지는 않음
        /// </summary>
        /// <param name="startIndex">지정할 위치(이 뒤로만 빈 슬롯을 당겨옴)</param>
        private void CompactCreatureSlots(int startIndex)
        {
            int writeIndex = Mathf.Clamp(startIndex, 0, _creatureSlots.Length);
            for (int readIndex = writeIndex; readIndex < _creatureSlots.Length; readIndex++)
            {
                CreatureInventorySlot source = _creatureSlots[readIndex];
                if (source == null || source.IsEmpty)
                {
                    continue;
                }

                if (writeIndex != readIndex)
                {
                    _creatureSlots[writeIndex].Set(source.DefinitionId, source.Count);
                    source.Clear();
                }

                writeIndex++;
            }

            for (int i = writeIndex; i < _creatureSlots.Length; i++)
            {
                _creatureSlots[i]?.Clear();
            }
        }

        private static CreatureInventorySlot[] CreateEmptySlots(int capacity)
        {
            CreatureInventorySlot[] slots =
                new CreatureInventorySlot[Mathf.Max(1, capacity)];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new CreatureInventorySlot();
            }

            return slots;
        }

        /// <summary>
        /// 특정 대상을 특정 개수만큼 꺼냄<br/>
        /// 부족한 경우에도 제거되며, 실제 꺼낸 개수를 반환
        /// </summary>
        /// <param name="creatureId">꺼낼 대상의 id</param>
        /// <param name="requestedAmount">꺼낼 개수</param>
        /// <returns>실제로 꺼내진 개수</returns>
        public int Take(string creatureId, int requestedAmount)
        {
            CreatureInventorySlot[] slots = _creatureSlots;
            int remainingAmount = requestedAmount;

            for (int i = slots.Length - 1; i >= 0 && remainingAmount > 0; i--)
            {
                CreatureInventorySlot slot = slots[i];
                if (slot == null || !slot.Matches(creatureId))
                {
                    continue;
                }

                int takenFromSlot = Mathf.Min(slot.Count, remainingAmount);
                slot.Set(creatureId, slot.Count - takenFromSlot);
                remainingAmount -= takenFromSlot;
            }

            int takenAmount = requestedAmount - remainingAmount;

            return takenAmount;
        }

        public int GetCount(string creatureId)
        {
            int total = 0;
            IReadOnlyList<CreatureInventorySlot> slots = _creatureSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                CreatureInventorySlot slot = slots[i];
                if (slot == null || !slot.Matches(creatureId))
                {
                    continue;
                }

                total = total > int.MaxValue - slot.Count
                    ? int.MaxValue
                    : total + slot.Count;
            }

            return total;
        }

        /// <summary>
        /// 타임아웃 등으로 인한 일정 비율 유실<br/>
        /// 슬롯별로 반올림하여 적용됨
        /// </summary>
        /// <param name="lossRatio"></param>
        public void ApplyLoss(float lossRatio)
        {
            float clampedLossRatio = Mathf.Clamp01(lossRatio);
            if (clampedLossRatio <= 0f)
            {
                return;
            }

            CreatureInventorySlot[] slots = _creatureSlots;
            for (int i = 0; i < slots.Length; i++)
            {
                CreatureInventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty)
                {
                    continue;
                }

                int lostAmount = Mathf.CeilToInt(slot.Count * clampedLossRatio);
                slot.Set(slot.DefinitionId, slot.Count - lostAmount);
            }
        }
    }
}
