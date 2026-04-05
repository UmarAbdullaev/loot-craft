using System;
using LootCraft.Data;
using UnityEngine;

namespace LootCraft.Persistence
{
    /// <summary>
    /// Одна ячейка инвентаря в файле сохранения (JsonUtility).
    /// </summary>
    [Serializable]
    public class SlotSaveData
    {
        public string itemId = "";
        public int count;
    }

    /// <summary>
    /// Корневой объект JSON: монеты, прогресс разблокировки, все слоты.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public int saveVersion = 1;
        public int coins;
        public int extraUnlockedBeyondDefault;
        public SlotSaveData[] slots = Array.Empty<SlotSaveData>();

        /// <summary>
        /// Приводит данные к корректному размеру и диапазонам; очищает предметы в ещё заблокированных слотах.
        /// </summary>
        public void Normalize(InventoryConfig config)
        {
            int totalSlots = config != null ? config.TotalSlots : 50;
            int initialUnlocked = config != null ? config.InitialUnlockedSlots : 15;
            int maxExtra = Mathf.Max(0, totalSlots - initialUnlocked);

            coins = Mathf.Max(0, coins);
            extraUnlockedBeyondDefault = Mathf.Clamp(extraUnlockedBeyondDefault, 0, maxExtra);

            if (slots == null || slots.Length != totalSlots)
            {
                var next = new SlotSaveData[totalSlots];
                if (slots != null)
                {
                    for (int i = 0; i < Mathf.Min(slots.Length, totalSlots); i++)
                        next[i] = CloneSlot(slots[i]);
                }

                for (int i = 0; i < totalSlots; i++)
                {
                    if (next[i] == null)
                        next[i] = new SlotSaveData();
                }

                slots = next;
            }

            int unlockedCount = initialUnlocked + extraUnlockedBeyondDefault;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    slots[i] = new SlotSaveData();

                if (slots[i].count < 0)
                    slots[i].count = 0;

                if (string.IsNullOrEmpty(slots[i].itemId))
                    slots[i].itemId = "";

                // Заблокированные ячейки не должны хранить лут
                if (i >= unlockedCount)
                {
                    slots[i].itemId = "";
                    slots[i].count = 0;
                }
            }
        }

        private static SlotSaveData CloneSlot(SlotSaveData s)
        {
            if (s == null)
                return new SlotSaveData();
            return new SlotSaveData { itemId = s.itemId ?? "", count = s.count };
        }
    }
}
