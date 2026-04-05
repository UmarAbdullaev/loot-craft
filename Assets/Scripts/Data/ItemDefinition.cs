using UnityEngine;

namespace LootCraft.Data
{
    /// <summary>
    /// Описание одного типа предмета (строка таблицы ТЗ). Настраивается в инспекторе или через меню генерации.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Inventory/Item Definition", order = 0)]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private ItemKind kind;
        [SerializeField] private Sprite icon;
        [SerializeField] private float weight;
        [SerializeField] private int maxStack = 1;
        [SerializeField] private int protection;
        [SerializeField] private int damage;
        [Tooltip("Для оружия: ID патронов (ItemDefinition), например PistolAmmo.")]
        [SerializeField] private string ammoItemId;

        public string ItemId => itemId;
        public ItemKind Kind => kind;
        public Sprite Icon => icon;
        public float Weight => weight;
        public int MaxStack => maxStack;
        public int Protection => protection;
        public int Damage => damage;
        public string AmmoItemId => ammoItemId;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"ItemDefinition '{name}': пустой itemId.", this);
                return;
            }

            if (maxStack < 1)
                maxStack = 1;

            if (weight < 0f)
                weight = 0f;

            switch (kind)
            {
                case ItemKind.Weapon:
                    if (string.IsNullOrWhiteSpace(ammoItemId))
                        Debug.LogWarning($"ItemDefinition '{itemId}': у оружия должен быть ammoItemId.", this);
                    break;
                case ItemKind.Ammo:
                    if (protection != 0 || damage != 0)
                        Debug.LogWarning($"ItemDefinition '{itemId}': патроны не должны иметь protection/damage.", this);
                    if (!string.IsNullOrEmpty(ammoItemId))
                        Debug.LogWarning($"ItemDefinition '{itemId}': у патронов ammoItemId должен быть пустым.", this);
                    break;
                case ItemKind.Head:
                case ItemKind.Torso:
                    if (damage != 0)
                        Debug.LogWarning($"ItemDefinition '{itemId}': броня не должна иметь damage.", this);
                    if (!string.IsNullOrEmpty(ammoItemId))
                        Debug.LogWarning($"ItemDefinition '{itemId}': у брони ammoItemId должен быть пустым.", this);
                    break;
            }
        }
    }
}
