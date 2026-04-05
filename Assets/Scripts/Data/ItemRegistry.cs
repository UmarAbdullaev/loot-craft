using System.Collections.Generic;
using UnityEngine;

namespace LootCraft.Data
{
    /// <summary>
    /// Список всех ItemDefinition для поиска по строковому ID (и для удобства в инспекторе сервиса).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemRegistry", menuName = "Inventory/Item Registry", order = 2)]
    public class ItemRegistry : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> Items => items;

        public ItemDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < items.Count; i++)
            {
                var def = items[i];
                if (def != null && def.ItemId == id)
                    return def;
            }

            return null;
        }

        private void OnValidate()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < items.Count; i++)
            {
                var def = items[i];
                if (def == null)
                    continue;

                if (string.IsNullOrEmpty(def.ItemId))
                    continue;

                if (!seen.Add(def.ItemId))
                    Debug.LogWarning($"ItemRegistry '{name}': дубликат itemId '{def.ItemId}'.", this);
            }
        }
    }
}
