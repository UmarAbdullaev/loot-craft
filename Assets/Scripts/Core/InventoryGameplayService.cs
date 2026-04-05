using System;
using System.Collections.Generic;
using LootCraft.Audio;
using LootCraft.Data;
using LootCraft.Persistence;
using UnityEngine;

namespace LootCraft.Core
{
    /// <summary>
    /// Кнопки ТЗ: монеты, предмет, патроны, выстрел, удаление; разблокировка слота; вес; события для UI.
    /// </summary>
    public class InventoryGameplayService : MonoBehaviour
    {
        [SerializeField] private GamePersistenceBootstrap persistence;
        [SerializeField] private ItemRegistry itemRegistry;

        public event Action OnCoinsChanged;
        public event Action OnInventoryChanged;

        /// <summary>Успешные действия для UI (твины, звук). Не срабатывают при ошибках ТЗ.</summary>
        public event Action OnCoinsGranted;
        public event Action<int> OnEquipmentPlaced;
        public event Action<int> OnAmmoPrimarySlot;
        public event Action OnShootPerformed;
        public event Action<int> OnRandomSlotCleared;
        public event Action<int> OnSlotUnlockPurchased;
        public event Action<int, int> OnDragDropApplied;

        private InventoryRuntimeState _state;
        private InventoryConfig _config;
        private ItemRegistry _registry;
        private bool _ready;

        public InventoryRuntimeState State => _state;
        public ItemRegistry Registry => _registry;
        public InventoryConfig Config => _config;

        private void Start()
        {
            if (persistence == null || itemRegistry == null)
            {
                Debug.LogError("InventoryGameplayService: не назначены persistence или itemRegistry.", this);
                enabled = false;
                return;
            }

            _registry = itemRegistry;
            _config = persistence.Config;
            _state = persistence.State;
            if (_state == null || _config == null)
            {
                Debug.LogError("InventoryGameplayService: состояние или конфиг недоступны.", this);
                enabled = false;
                return;
            }

            _ready = true;
        }

        /// <summary>
        /// Суммарный вес содержимого открытых слотов (count * Weight из ItemDefinition).
        /// </summary>
        public float GetTotalWeight()
        {
            if (_state == null || _registry == null)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < _state.TotalSlots; i++)
            {
                if (!_state.IsSlotUnlocked(i))
                    continue;

                var entry = _state.GetSlot(i);
                if (entry.IsEmpty)
                    continue;

                var def = _registry.GetById(entry.ItemId);
                if (def != null)
                    sum += def.Weight * entry.Count;
            }

            return sum;
        }

        /// <summary>
        /// Кнопка «Добавить монеты»: случайно от 9 до 99.
        /// </summary>
        public void AddCoinsRandom()
        {
            if (!EnsureReady())
                return;

            int n = UnityEngine.Random.Range(9, 100);
            _state.AddCoins(n);
            Debug.Log($"Добавлено ({n}) монет");
            AfterEconomyChange();
            OnCoinsGranted?.Invoke();
        }

        /// <summary>
        /// Кнопка «Добавить предмет»: случайное оружие или броня (голова/торс), один экземпляр.
        /// </summary>
        public void AddRandomEquipmentItem()
        {
            if (!EnsureReady())
                return;

            var pool = new List<ItemDefinition>(8);
            foreach (var def in _registry.Items)
            {
                if (def == null)
                    continue;
                if (def.Kind == ItemKind.Weapon || def.Kind == ItemKind.Head || def.Kind == ItemKind.Torso)
                    pool.Add(def);
            }

            if (pool.Count == 0)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError("В реестре нет предметов типа оружие/голова/торс.");
                return;
            }

            var pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            int slot = FindFirstEmptyUnlockedSlot();
            if (slot < 0)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError("Инвентарь полон");
                return;
            }

            _state.TrySetSlotContent(slot, pick.ItemId, 1);
            Debug.Log($"Добавлено {pick.ItemId} в слот: {slot}");
            AfterInventoryChange();
            OnEquipmentPlaced?.Invoke(slot);
        }

        /// <summary>
        /// Кнопка «Добавить патроны»: 10–30 случайного типа; сначала добиваем стаки, затем пустые слоты.
        /// </summary>
        public void AddRandomAmmo()
        {
            if (!EnsureReady())
                return;

            string ammoId = UnityEngine.Random.value < 0.5f ? "PistolAmmo" : "AssaultRifleAmmo";
            var ammoDef = _registry.GetById(ammoId);
            if (ammoDef == null || ammoDef.Kind != ItemKind.Ammo)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError($"В реестре нет патронов «{ammoId}».");
                return;
            }

            int amount = UnityEngine.Random.Range(10, 31);
            int maxStack = ammoDef.MaxStack;
            int remaining = amount;
            int firstTouchedSlot = -1;

            while (remaining > 0)
            {
                int atStart = remaining;

                for (int i = 0; i < _state.TotalSlots && remaining > 0; i++)
                {
                    if (!_state.IsSlotUnlocked(i))
                        continue;

                    var e = _state.GetSlot(i);
                    if (e.IsEmpty || e.ItemId != ammoId)
                        continue;
                    if (e.Count >= maxStack)
                        continue;

                    int space = maxStack - e.Count;
                    int add = Mathf.Min(space, remaining);
                    if (add <= 0)
                        continue;

                    if (firstTouchedSlot < 0)
                        firstTouchedSlot = i;

                    _state.TrySetSlotContent(i, ammoId, e.Count + add);
                    remaining -= add;
                }

                for (int i = 0; i < _state.TotalSlots && remaining > 0; i++)
                {
                    if (!_state.IsSlotUnlocked(i))
                        continue;
                    if (!_state.GetSlot(i).IsEmpty)
                        continue;

                    int add = Mathf.Min(maxStack, remaining);
                    if (firstTouchedSlot < 0)
                        firstTouchedSlot = i;

                    _state.TrySetSlotContent(i, ammoId, add);
                    remaining -= add;
                }

                if (remaining == atStart)
                    break;
            }

            int placed = amount - remaining;
            if (placed == 0)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError("Инвентарь полон");
                return;
            }

            if (remaining > 0)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError("Инвентарь полон");
            }

            int logSlot = firstTouchedSlot >= 0 ? firstTouchedSlot : FindFirstSlotWithItem(ammoId);
            Debug.Log($"Добавлено ({placed}) {ammoId} в слот: {logSlot}");
            AfterInventoryChange();
            OnAmmoPrimarySlot?.Invoke(logSlot);
        }

        /// <summary>
        /// Кнопка «Выстрел»: случайное оружие из инвентаря, расход одного патрона.
        /// </summary>
        public void ShootRandomWeapon()
        {
            if (!EnsureReady())
                return;

            var weaponSlots = new List<int>(4);
            for (int i = 0; i < _state.TotalSlots; i++)
            {
                if (!_state.IsSlotUnlocked(i))
                    continue;
                var e = _state.GetSlot(i);
                if (e.IsEmpty || e.Count < 1)
                    continue;
                var def = _registry.GetById(e.ItemId);
                if (def != null && def.Kind == ItemKind.Weapon)
                    weaponSlots.Add(i);
            }

            if (weaponSlots.Count == 0)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError("Нет оружия");
                return;
            }

            int wSlot = weaponSlots[UnityEngine.Random.Range(0, weaponSlots.Count)];
            var wEntry = _state.GetSlot(wSlot);
            var weaponDef = _registry.GetById(wEntry.ItemId);
            string ammoId = weaponDef.AmmoItemId;

            var ammoIndices = new List<int>(8);
            for (int i = 0; i < _state.TotalSlots; i++)
            {
                if (!_state.IsSlotUnlocked(i))
                    continue;
                var e = _state.GetSlot(i);
                if (!e.IsEmpty && e.ItemId == ammoId && e.Count > 0)
                    ammoIndices.Add(i);
            }

            if (ammoIndices.Count == 0)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError($"Нет патронов для {weaponDef.ItemId}");
                return;
            }

            int aSlot = ammoIndices[UnityEngine.Random.Range(0, ammoIndices.Count)];
            var aEntry = _state.GetSlot(aSlot);
            int newCount = aEntry.Count - 1;
            if (newCount <= 0)
                _state.TrySetSlotContent(aSlot, "", 0);
            else
                _state.TrySetSlotContent(aSlot, ammoId, newCount);

            Debug.Log($"Выстрел из {weaponDef.ItemId}, патроны: {ammoId}, урон: {weaponDef.Damage}");
            OnShootPerformed?.Invoke();
            AfterInventoryChange();
        }

        /// <summary>
        /// Кнопка «Удалить предмет»: случайный непустой открытый слот.
        /// </summary>
        public void DeleteRandomOccupiedSlot()
        {
            if (!EnsureReady())
                return;

            var occupied = new List<int>(16);
            for (int i = 0; i < _state.TotalSlots; i++)
            {
                if (!_state.IsSlotUnlocked(i))
                    continue;
                if (!_state.GetSlot(i).IsEmpty)
                    occupied.Add(i);
            }

            if (occupied.Count == 0)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError("Инвентарь пуст");
                return;
            }

            int idx = occupied[UnityEngine.Random.Range(0, occupied.Count)];
            var e = _state.GetSlot(idx);
            int count = e.Count;
            string id = e.ItemId;
            _state.TrySetSlotContent(idx, "", 0);
            Debug.Log($"Удалено ({count}) {id} из слота: {idx}");
            AfterInventoryChange();
            OnRandomSlotCleared?.Invoke(idx);
        }

        /// <summary>
        /// Перетаскивание между открытыми слотами: пустой — перенос; тот же предмет — слияние до MaxStack; иначе — обмен.
        /// </summary>
        public bool TryApplyDragBetweenSlots(int fromIndex, int toIndex)
        {
            if (!EnsureReady())
                return false;
            if (fromIndex == toIndex)
                return false;
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= _state.TotalSlots || toIndex >= _state.TotalSlots)
                return false;
            if (!_state.IsSlotUnlocked(fromIndex) || !_state.IsSlotUnlocked(toIndex))
                return false;

            var fromEntry = _state.GetSlot(fromIndex);
            if (fromEntry.IsEmpty)
                return false;

            var def = _registry.GetById(fromEntry.ItemId);
            if (def == null)
                return false;

            var toEntry = _state.GetSlot(toIndex);

            if (toEntry.IsEmpty)
            {
                _state.TrySetSlotContent(toIndex, fromEntry.ItemId, fromEntry.Count);
                _state.TrySetSlotContent(fromIndex, "", 0);
                OnDragDropApplied?.Invoke(fromIndex, toIndex);
                AfterInventoryChange();
                return true;
            }

            if (toEntry.ItemId == fromEntry.ItemId)
            {
                int maxStack = def.MaxStack;
                int space = maxStack - toEntry.Count;
                if (space <= 0)
                    return false;

                int move = Mathf.Min(space, fromEntry.Count);
                _state.TrySetSlotContent(toIndex, toEntry.ItemId, toEntry.Count + move);
                int remain = fromEntry.Count - move;
                if (remain <= 0)
                    _state.TrySetSlotContent(fromIndex, "", 0);
                else
                    _state.TrySetSlotContent(fromIndex, fromEntry.ItemId, remain);
                OnDragDropApplied?.Invoke(fromIndex, toIndex);
                AfterInventoryChange();
                return true;
            }

            string toId = toEntry.ItemId;
            int toCount = toEntry.Count;
            _state.TrySetSlotContent(toIndex, fromEntry.ItemId, fromEntry.Count);
            _state.TrySetSlotContent(fromIndex, toId, toCount);
            OnDragDropApplied?.Invoke(fromIndex, toIndex);
            AfterInventoryChange();
            return true;
        }

        /// <summary>
        /// Тап по следующему заблокированному слоту: оплата и открытие по очереди.
        /// </summary>
        public void TryUnlockSlotAtIndex(int slotIndex)
        {
            if (!EnsureReady())
                return;

            if (slotIndex != _state.UnlockedSlotCount)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogWarning($"Слот {slotIndex} сейчас нельзя разблокировать (следующий индекс: {_state.UnlockedSlotCount}).");
                return;
            }

            int cost = _config.GetUnlockCoinCostForSlot(slotIndex);
            if (cost == int.MaxValue)
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogWarning($"Для слота {slotIndex} не задана стоимость.");
                return;
            }

            if (!_state.TrySpendCoins(cost))
            {
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogWarning("Недостаточно монет для разблокировки слота.");
                return;
            }

            if (!_state.TryUnlockOneMoreSlot())
            {
                _state.AddCoins(cost);
                SoundManager.PlaySound(SoundManager.SoundType.Error);
                Debug.LogError("Не удалось разблокировать слот (внутренняя ошибка).");
                return;
            }

            AfterEconomyChange();
            OnSlotUnlockPurchased?.Invoke(slotIndex);
        }

        private bool EnsureReady()
        {
            return _ready && _state != null && _config != null && _registry != null;
        }

        private void AfterEconomyChange()
        {
            persistence.Persist();
            OnCoinsChanged?.Invoke();
            OnInventoryChanged?.Invoke();
        }

        private void AfterInventoryChange()
        {
            persistence.Persist();
            OnInventoryChanged?.Invoke();
        }

        private int FindFirstEmptyUnlockedSlot()
        {
            for (int i = 0; i < _state.TotalSlots; i++)
            {
                if (!_state.IsSlotUnlocked(i))
                    continue;
                if (_state.GetSlot(i).IsEmpty)
                    return i;
            }

            return -1;
        }

        private int FindFirstSlotWithItem(string itemId)
        {
            for (int i = 0; i < _state.TotalSlots; i++)
            {
                if (!_state.IsSlotUnlocked(i))
                    continue;
                var e = _state.GetSlot(i);
                if (!e.IsEmpty && e.ItemId == itemId)
                    return i;
            }

            return -1;
        }
    }
}
