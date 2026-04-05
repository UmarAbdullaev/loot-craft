using UnityEngine;

namespace LootCraft.Data
{
    /// <summary>
    /// Параметры сетки инвентаря и стоимости разблокировки слотов (после первых N бесплатных).
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "Inventory/Inventory Config", order = 1)]
    public class InventoryConfig : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int totalSlots = 50;

        [Min(0)]
        [SerializeField] private int initialUnlockedSlots = 15;

        [Tooltip("Стоимость в монетах: индекс 0 = слот с индексом initialUnlockedSlots, далее по порядку.")]
        [SerializeField] private int[] unlockCostsFromFirstPurchasableSlot;

        public int TotalSlots => totalSlots;
        public int InitialUnlockedSlots => initialUnlockedSlots;
        public int[] UnlockCostsFromFirstPurchasableSlot => unlockCostsFromFirstPurchasableSlot;

        /// <summary>
        /// Сколько слотов нужно открыть за монеты (обычно 50 - 15 = 35).
        /// </summary>
        public int PurchasableSlotCount => Mathf.Max(0, totalSlots - initialUnlockedSlots);

        /// <summary>
        /// Монеты за разблокировку слота с индексом slotIndex. Для уже бесплатных слотов — 0.
        /// </summary>
        public int GetUnlockCoinCostForSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= totalSlots)
                return int.MaxValue;

            if (slotIndex < initialUnlockedSlots)
                return 0;

            int rel = slotIndex - initialUnlockedSlots;
            if (unlockCostsFromFirstPurchasableSlot == null || rel < 0 || rel >= unlockCostsFromFirstPurchasableSlot.Length)
                return int.MaxValue;

            return Mathf.Max(0, unlockCostsFromFirstPurchasableSlot[rel]);
        }

        private void OnValidate()
        {
            if (totalSlots < initialUnlockedSlots)
                initialUnlockedSlots = totalSlots;

            int need = PurchasableSlotCount;
            if (need == 0)
                return;

            if (unlockCostsFromFirstPurchasableSlot == null || unlockCostsFromFirstPurchasableSlot.Length != need)
            {
                // Заполняем прогрессией из референса ТЗ: 20, 25, 30, … (+5 за слот)
                unlockCostsFromFirstPurchasableSlot = new int[need];
                for (int i = 0; i < need; i++)
                    unlockCostsFromFirstPurchasableSlot[i] = 20 + i * 5;
            }

            for (int i = 0; i < unlockCostsFromFirstPurchasableSlot.Length; i++)
            {
                if (unlockCostsFromFirstPurchasableSlot[i] < 0)
                    unlockCostsFromFirstPurchasableSlot[i] = 0;
            }
        }
    }
}
