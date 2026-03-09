using System.Collections.Generic;
using System.Linq;

namespace CursedBlood.Equipment
{
    public sealed class Inventory
    {
        public const int Capacity = 20;

        public EquipmentData[] EquippedSlots { get; } = new EquipmentData[4];

        public List<EquipmentData> Bag { get; } = new();

        public EquipmentData GetEquipped(EquipmentCategory category)
        {
            return EquippedSlots[(int)category];
        }

        public bool TryAddToBag(EquipmentData item)
        {
            if (item == null || Bag.Count >= Capacity)
            {
                return false;
            }

            Bag.Add(item.Clone());
            return true;
        }

        public EquipmentData ReplaceLowest(EquipmentData item)
        {
            if (Bag.Count < Capacity)
            {
                Bag.Add(item.Clone());
                return null;
            }

            var lowestIndex = 0;
            var lowestScore = Bag[0].PowerScore;
            for (var index = 1; index < Bag.Count; index++)
            {
                if (!(Bag[index].PowerScore < lowestScore))
                {
                    continue;
                }

                lowestScore = Bag[index].PowerScore;
                lowestIndex = index;
            }

            var removed = Bag[lowestIndex];
            Bag[lowestIndex] = item.Clone();
            return removed;
        }

        public EquipmentData RemoveFromBag(int index)
        {
            if (index < 0 || index >= Bag.Count)
            {
                return null;
            }

            var item = Bag[index];
            Bag.RemoveAt(index);
            return item;
        }

        public void Equip(EquipmentData item)
        {
            if (item == null)
            {
                return;
            }

            var slotIndex = (int)item.Category;
            Bag.Remove(item);

            var previous = EquippedSlots[slotIndex];
            EquippedSlots[slotIndex] = item.Clone();

            if (previous != null)
            {
                if (Bag.Count < Capacity)
                {
                    Bag.Add(previous);
                }
                else
                {
                    ReplaceLowest(previous);
                }
            }
        }

        public void EquipFromBag(int index)
        {
            var item = RemoveFromBag(index);
            if (item != null)
            {
                Equip(item);
            }
        }

        public void Unequip(EquipmentCategory category)
        {
            var slotIndex = (int)category;
            var item = EquippedSlots[slotIndex];
            if (item == null)
            {
                return;
            }

            if (Bag.Count < Capacity)
            {
                Bag.Add(item);
                EquippedSlots[slotIndex] = null;
            }
        }

        public IEnumerable<EquipmentData> EnumerateAllItems()
        {
            foreach (var equipped in EquippedSlots.Where(item => item != null))
            {
                yield return equipped;
            }

            foreach (var item in Bag)
            {
                yield return item;
            }
        }

        public EquipmentStats CalculateTotalStats()
        {
            return EquipmentEffectResolver.Calculate(EquippedSlots.Where(item => item != null));
        }

        public bool HasRarity(Rarity rarity)
        {
            return EnumerateAllItems().Any(item => item.Rarity == rarity);
        }

        public void ResetForNextGeneration(EquipmentData heirloom)
        {
            for (var index = 0; index < EquippedSlots.Length; index++)
            {
                EquippedSlots[index] = null;
            }

            Bag.Clear();

            if (heirloom != null)
            {
                TryAddToBag(heirloom.Clone());
            }
        }
    }
}