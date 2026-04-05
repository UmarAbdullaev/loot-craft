using LootCraft.Data;
using LootCraft.Persistence;
using UnityEngine;

namespace LootCraft.Core
{
    /// <summary>
    /// Текущее состояние инвентаря и кошелька в памяти; сериализуется через GameSaveData.
    /// </summary>
    public class InventoryRuntimeState
    {
        private readonly int _totalSlots;
        private readonly int _initialUnlockedSlots;
        private readonly int _maxExtraUnlocked;
        private readonly SlotEntry[] _slots;

        private int _coins;
        private int _extraUnlockedBeyondDefault;

        public int Coins => _coins;
        public int ExtraUnlockedBeyondDefault => _extraUnlockedBeyondDefault;
        public int TotalSlots => _totalSlots;
        public int InitialUnlockedSlots => _initialUnlockedSlots;
        public int UnlockedSlotCount => _initialUnlockedSlots + _extraUnlockedBeyondDefault;

        private InventoryRuntimeState(int totalSlots, int initialUnlockedSlots)
        {
            _totalSlots = totalSlots;
            _initialUnlockedSlots = initialUnlockedSlots;
            _maxExtraUnlocked = Mathf.Max(0, totalSlots - initialUnlockedSlots);
            _slots = new SlotEntry[totalSlots];
            for (int i = 0; i < totalSlots; i++)
                _slots[i] = new SlotEntry();
        }

        public static InventoryRuntimeState CreateNew(InventoryConfig config)
        {
            int total = config != null ? config.TotalSlots : 50;
            int initial = config != null ? config.InitialUnlockedSlots : 15;
            return new InventoryRuntimeState(total, initial);
        }

        public static InventoryRuntimeState FromSaveData(GameSaveData data, InventoryConfig config)
        {
            var state = CreateNew(config);
            if (data == null)
                return state;

            state._coins = Mathf.Max(0, data.coins);
            state._extraUnlockedBeyondDefault = Mathf.Clamp(data.extraUnlockedBeyondDefault, 0, state._maxExtraUnlocked);

            int n = Mathf.Min(state._slots.Length, data.slots != null ? data.slots.Length : 0);
            for (int i = 0; i < n; i++)
            {
                var s = data.slots[i];
                if (s == null)
                    continue;
                state._slots[i].Set(s.itemId, s.count);
            }

            state.SanitizeLockedSlots();
            return state;
        }

        public GameSaveData ToSaveData()
        {
            var data = new GameSaveData
            {
                saveVersion = 1,
                coins = _coins,
                extraUnlockedBeyondDefault = _extraUnlockedBeyondDefault,
                slots = new SlotSaveData[_totalSlots]
            };

            for (int i = 0; i < _totalSlots; i++)
            {
                var e = _slots[i];
                data.slots[i] = new SlotSaveData
                {
                    itemId = e.IsEmpty ? "" : e.ItemId,
                    count = e.IsEmpty ? 0 : e.Count
                };
            }

            return data;
        }

        public bool IsSlotUnlocked(int index)
        {
            if (index < 0 || index >= _totalSlots)
                return false;
            return index < UnlockedSlotCount;
        }

        public SlotEntry GetSlot(int index) => _slots[index];

        /// <summary>
        /// Записывает содержимое слота (пусто: itemId пустой или count = 0). Только для разблокированных индексов.
        /// </summary>
        public bool TrySetSlotContent(int index, string itemId, int count)
        {
            if (index < 0 || index >= _totalSlots || !IsSlotUnlocked(index))
                return false;
            if (count < 0)
                return false;

            if (string.IsNullOrEmpty(itemId) || count == 0)
                _slots[index].Clear();
            else
                _slots[index].Set(itemId, count);

            return true;
        }

        public void SetCoins(int value)
        {
            _coins = Mathf.Max(0, value);
        }

        public void AddCoins(int delta)
        {
            _coins = Mathf.Max(0, _coins + delta);
        }

        /// <summary>
        /// Пытается потратить монеты; при нехватке возвращает false, баланс не меняет.
        /// </summary>
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0)
                return true;
            if (_coins < amount)
                return false;
            _coins -= amount;
            return true;
        }

        public bool TryUnlockOneMoreSlot()
        {
            if (_extraUnlockedBeyondDefault >= _maxExtraUnlocked)
                return false;
            _extraUnlockedBeyondDefault++;
            return true;
        }

        /// <summary>
        /// Устанавливает число дополнительно открытых слотов (после стартовых бесплатных).
        /// </summary>
        public void SetExtraUnlockedBeyondDefault(int value)
        {
            _extraUnlockedBeyondDefault = Mathf.Clamp(value, 0, _maxExtraUnlocked);
            SanitizeLockedSlots();
        }

        /// <summary>
        /// Очищает слоты, которые сейчас считаются заблокированными.
        /// </summary>
        public void SanitizeLockedSlots()
        {
            for (int i = UnlockedSlotCount; i < _totalSlots; i++)
                _slots[i].Clear();
        }
    }
}
