using LootCraft.Core;
using LootCraft.Data;
using UnityEngine;

namespace LootCraft.Persistence
{
    /// <summary>
    /// Загружает сохранение при старте и пишет JSON при выходе / паузе (важно для Android).
    /// Повесьте на пустой объект в первой сцене и назначьте InventoryConfig.
    /// </summary>
    public class GamePersistenceBootstrap : MonoBehaviour
    {
        [SerializeField] private InventoryConfig inventoryConfig;

        private InventoryRuntimeState _state;

        public InventoryRuntimeState State => _state;
        public InventoryConfig Config => inventoryConfig;

        private void Awake()
        {
            if (JsonGameSaveStorage.TryLoad(out var saved))
            {
                saved.Normalize(inventoryConfig);
                _state = InventoryRuntimeState.FromSaveData(saved, inventoryConfig);
            }
            else
            {
                _state = InventoryRuntimeState.CreateNew(inventoryConfig);
            }

            // Пересохраняем нормализованное состояние, чтобы файл появился и формат был консистентен
            Persist();
        }

        private void OnApplicationQuit()
        {
            Persist();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                Persist();
        }

        /// <summary>
        /// Сохранить текущее состояние на диск (вызывать после любых игровых изменений).
        /// </summary>
        public void Persist()
        {
            if (_state == null)
                return;

            var data = _state.ToSaveData();
            data.Normalize(inventoryConfig);
            JsonGameSaveStorage.Save(data);
        }
    }
}
