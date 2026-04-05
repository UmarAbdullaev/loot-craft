#if UNITY_EDITOR
using LootCraft.Data;
using UnityEditor;
using UnityEngine;

namespace LootCraft.Editor
{
    /// <summary>
    /// Создаёт или обновляет ScriptableObject-данные инвентаря по таблицам тестового задания.
    /// </summary>
    public static class InventoryDefaultAssetsMenu
    {
        private const string DataRoot = "Assets/Data";
        private const string ItemsFolder = "Assets/Data/Items";

        [MenuItem("Tools/Inventory/Generate Default Item Data")]
        public static void GenerateAll()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(ItemsFolder);

            var cap = CreateOrReplaceItem($"{ItemsFolder}/Item_Cap.asset", "Cap", ItemKind.Head, 0.2f, 1, protection: 1);
            var helmet = CreateOrReplaceItem($"{ItemsFolder}/Item_Helmet.asset", "Helmet", ItemKind.Head, 1f, 1, protection: 8);
            var jacket = CreateOrReplaceItem($"{ItemsFolder}/Item_Jacket.asset", "Jacket", ItemKind.Torso, 0.8f, 1, protection: 3);
            var bodyArmor = CreateOrReplaceItem($"{ItemsFolder}/Item_BodyArmor.asset", "BodyArmor", ItemKind.Torso, 10f, 1, protection: 10);
            var pistolAmmo = CreateOrReplaceItem($"{ItemsFolder}/Item_PistolAmmo.asset", "PistolAmmo", ItemKind.Ammo, 0.01f, maxStack: 50);
            var rifleAmmo = CreateOrReplaceItem($"{ItemsFolder}/Item_AssaultRifleAmmo.asset", "AssaultRifleAmmo", ItemKind.Ammo, 0.015f, maxStack: 40);
            var pistol = CreateOrReplaceItem($"{ItemsFolder}/Item_Pistol.asset", "Pistol", ItemKind.Weapon, 1f, 1, damage: 10, ammoItemId: "PistolAmmo");
            var rifle = CreateOrReplaceItem($"{ItemsFolder}/Item_AssaultRifle.asset", "AssaultRifle", ItemKind.Weapon, 5f, 1, damage: 20, ammoItemId: "AssaultRifleAmmo");

            var registry = CreateOrReplaceRegistry($"{DataRoot}/ItemRegistry.asset", new[]
            {
                cap, helmet, jacket, bodyArmor, pistolAmmo, rifleAmmo, pistol, rifle
            });

            var invConfig = CreateOrReplaceInventoryConfig($"{DataRoot}/InventoryConfig.asset");

            EditorUtility.FocusProjectWindow();
            Selection.objects = new Object[] { registry, invConfig };
            Debug.Log("Созданы ItemDefinition (8), ItemRegistry, InventoryConfig. При необходимости назначьте Icon вручную.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = "Assets";
            var parts = path.Replace("Assets/", "").Split('/');
            foreach (var part in parts)
            {
                var next = $"{parent}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(parent, part);
                parent = next;
            }
        }

        private static ItemDefinition CreateOrReplaceItem(
            string assetPath,
            string id,
            ItemKind kind,
            float weight,
            int maxStack,
            int protection = 0,
            int damage = 0,
            string ammoItemId = "")
        {
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            ApplyItem(assetPath, asset, id, kind, weight, maxStack, protection, damage, ammoItemId);
            return asset;
        }

        private static void ApplyItem(
            string assetPath,
            ItemDefinition asset,
            string id,
            ItemKind kind,
            float weight,
            int maxStack,
            int protection,
            int damage,
            string ammoItemId)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("itemId").stringValue = id;
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("weight").floatValue = weight;
            so.FindProperty("maxStack").intValue = maxStack;
            so.FindProperty("protection").intValue = protection;
            so.FindProperty("damage").intValue = damage;
            so.FindProperty("ammoItemId").stringValue = ammoItemId ?? string.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static ItemRegistry CreateOrReplaceRegistry(string assetPath, ItemDefinition[] ordered)
        {
            var reg = AssetDatabase.LoadAssetAtPath<ItemRegistry>(assetPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<ItemRegistry>();
                AssetDatabase.CreateAsset(reg, assetPath);
            }

            var so = new SerializedObject(reg);
            var list = so.FindProperty("items");
            list.arraySize = ordered.Length;
            for (int i = 0; i < ordered.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssetIfDirty(reg);
            return reg;
        }

        private static InventoryConfig CreateOrReplaceInventoryConfig(string assetPath)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<InventoryConfig>(assetPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<InventoryConfig>();
                AssetDatabase.CreateAsset(cfg, assetPath);
            }

            var so = new SerializedObject(cfg);
            so.FindProperty("totalSlots").intValue = 50;
            so.FindProperty("initialUnlockedSlots").intValue = 15;
            var costs = so.FindProperty("unlockCostsFromFirstPurchasableSlot");
            const int need = 35;
            costs.arraySize = need;
            for (int i = 0; i < need; i++)
                costs.GetArrayElementAtIndex(i).intValue = 20 + i * 5;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssetIfDirty(cfg);
            return cfg;
        }
    }
}
#endif
