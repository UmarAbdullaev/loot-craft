#if UNITY_EDITOR
using System.IO;
using LootCraft.Persistence;
using UnityEditor;
using UnityEngine;

namespace LootCraft.Editor
{
    /// <summary>
    /// Удаляет JSON сохранение из persistentDataPath (как в рантайме). Удобно сбросить прогресс при тестах в Editor.
    /// </summary>
    public static class InventorySaveEditorMenu
    {
        private const string MenuPath = "Tools/Inventory/Clear Save File (Editor)";

        [MenuItem(MenuPath)]
        public static void ClearSaveFile()
        {
            string path = JsonGameSaveStorage.FilePath;
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog(
                    "Inventory Save",
                    $"Файл не найден (уже чисто):\n{path}",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Inventory Save",
                    $"Удалить сохранение?\n\n{path}",
                    "Удалить",
                    "Отмена"))
                return;

            JsonGameSaveStorage.DeleteSave();
            Debug.Log($"Inventory: удалено сохранение — {path}");
            EditorUtility.DisplayDialog("Inventory Save", "Файл сохранения удалён.", "OK");
        }
    }
}
#endif
