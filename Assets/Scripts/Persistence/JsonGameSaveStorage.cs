using System;
using System.IO;
using UnityEngine;

namespace LootCraft.Persistence
{
    /// <summary>
    /// Запись и чтение сохранения в JSON под persistentDataPath (без PlayerPrefs).
    /// </summary>
    public static class JsonGameSaveStorage
    {
        private const string FileName = "game_save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(GameSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("JsonGameSaveStorage: попытка сохранить null.");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"JsonGameSaveStorage: не удалось сохранить файл: {e.Message}");
            }
        }

        /// <summary>
        /// Возвращает true, если файл прочитан и распарсен (даже при пустом содержимом после FromJson).
        /// </summary>
        public static bool TryLoad(out GameSaveData data)
        {
            data = null;
            if (!File.Exists(FilePath))
                return false;

            try
            {
                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                data = JsonUtility.FromJson<GameSaveData>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"JsonGameSaveStorage: не удалось загрузить файл: {e.Message}");
                return false;
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"JsonGameSaveStorage: не удалось удалить файл: {e.Message}");
            }
        }
    }
}
