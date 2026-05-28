using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class SaveSystem
{
    // ========== ПУБЛИЧНЫЕ ПЕРЕМЕННЫЕ ==========

    public static int currentSeriesIndex = 0;
    private static int currentSlot = 0;
    public static System.Action OnSlotChanged;

    // ========== ВНУТРЕННИЕ КЛАССЫ ДЛЯ JSON ==========

    [System.Serializable]
    class SlotMeta
    {
        public string slotName;
    }

    [System.Serializable]
    class SlotList
    {
        public List<int> slots = new List<int>();
    }

    [System.Serializable]
    class SaveData
    {
        public string inkState;
        public int seriesIndex;
    }

    [System.Serializable]
    class GlobalProgress
    {
        public int currentSeriesIndex;
    }

    [System.Serializable]
    public class SerializationWrapper
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        public SerializationWrapper(Dictionary<string, object> dict)
        {
            foreach (var kv in dict)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value.ToString());
            }
        }
    }

    // ========== ПУТИ К ФАЙЛАМ ==========

    private static string SlotsPath =>
        Path.Combine(Application.persistentDataPath, "slots.json");

    private static string GetProgressPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"progress_{slot}.json");

    private static string GetSavePath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    private static string GetMetaPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_{slot}_meta.json");

    // ========== УПРАВЛЕНИЕ СЛОТАМИ ==========

    public static int GetCurrentSlot()
    {
        // Возвращает текущий активный слот сохранения
        return currentSlot;
    }

    public static void SetSlot(int slot)
    {
        // Переключается на указанный слот и загружает его прогресс
        currentSlot = slot;
        LoadProgress();
        Debug.Log("Switched to slot: " + slot);
        OnSlotChanged?.Invoke();
    }

    public static int CreateNewSlot()
    {
        // Создаёт новый слот сохранения с автоматическим именем
        var slots = LoadSlots();

        int newSlot = slots.Count > 0 ? slots.Max() + 1 : 0;
        slots.Add(newSlot);

        SaveSlots(new SlotList { slots = slots });

        SetSlot(newSlot);
        SaveSlotName("Слот " + (slots.Count));

        return newSlot;
    }

    public static bool HasSave(int slot)
    {
        // Проверяет, существует ли сохранение в указанном слоте
        string path = GetSavePath(slot);

        if (!File.Exists(path))
            return false;

        string json = File.ReadAllText(path);
        return !string.IsNullOrEmpty(json) && json.Length > 10;
    }

    public static void DeleteSlotFiles(int slot)
    {
        // Удаляет все файлы сохранения указанного слота
        string savePath = GetSavePath(slot);
        string metaPath = GetMetaPath(slot);

        if (File.Exists(savePath))
            File.Delete(savePath);

        if (File.Exists(metaPath))
            File.Delete(metaPath);
    }

    // ========== СОХРАНЕНИЕ / ЗАГРУЗКА ИНК-СОСТОЯНИЯ ==========

    public static void SaveInkState(Ink.Runtime.Story story)
    {
        // Сохраняет состояние Ink-стори в файл
        SaveData data = new SaveData();
        data.inkState = story.state.ToJson();
        data.seriesIndex = currentSeriesIndex;

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(GetSavePath(currentSlot), json);
    }

    public static bool LoadInkState(Ink.Runtime.Story story)
    {
        // Загружает Ink-стори из файла (только если серия совпадает)
        string path = GetSavePath(currentSlot);

        if (!File.Exists(path))
            return false;

        string json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data.seriesIndex != currentSeriesIndex)
            return false;

        story.state.LoadJson(data.inkState);
        return true;
    }

    public static void DeleteSave()
    {
        // Удаляет файл сохранения текущего слота
        string path = GetSavePath(currentSlot);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    // ========== ГЛОБАЛЬНЫЕ ПЕРЕМЕННЫЕ ==========

    public static void SaveGlobalVariables(Ink.Runtime.Story story)
    {
        // Сохраняет все глобальные переменные Ink в отдельный файл
        var vars = story.variablesState;
        Dictionary<string, object> dict = new Dictionary<string, object>();

        foreach (string name in vars)
        {
            dict[name] = vars[name];
        }

        string json = JsonUtility.ToJson(new SerializationWrapper(dict));
        File.WriteAllText(GetSavePath(currentSlot) + "_vars", json);
    }

    public static void LoadGlobalVariables(Ink.Runtime.Story story)
    {
        // Загружает глобальные переменные Ink из файла
        string path = GetSavePath(currentSlot) + "_vars";

        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);

        for (int i = 0; i < wrapper.keys.Count; i++)
        {
            string key = wrapper.keys[i];
            string value = wrapper.values[i];

            if (story.variablesState.Contains(key))
            {
                if (int.TryParse(value, out int intVal))
                    story.variablesState[key] = intVal;
                else if (bool.TryParse(value, out bool boolVal))
                    story.variablesState[key] = boolVal;
                else
                    story.variablesState[key] = value;
            }
        }
    }

    public static Dictionary<string, object> LoadGlobalVariablesRaw()
    {
        // Загружает глобальные переменные без Ink-стори (для UI/статов)
        string path = GetSavePath(currentSlot) + "_vars";

        if (!File.Exists(path))
            return new Dictionary<string, object>();

        string json = File.ReadAllText(path);
        var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);

        Dictionary<string, object> dict = new Dictionary<string, object>();

        for (int i = 0; i < wrapper.keys.Count; i++)
        {
            string key = wrapper.keys[i];
            string value = wrapper.values[i];

            if (int.TryParse(value, out int intVal))
                dict[key] = intVal;
            else if (bool.TryParse(value, out bool boolVal))
                dict[key] = boolVal;
            else
                dict[key] = value;
        }

        return dict;
    }

    // ========== ПРОГРЕСС СЕРИЙ ==========

    public static void SaveProgress()
    {
        // Сохраняет индекс текущей серии
        GlobalProgress data = new GlobalProgress();
        data.currentSeriesIndex = currentSeriesIndex;

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(GetProgressPath(currentSlot), json);
    }

    public static void LoadProgress()
    {
        // Загружает индекс текущей серии
        string path = GetProgressPath(currentSlot);

        if (!File.Exists(path))
        {
            currentSeriesIndex = 0;
            return;
        }

        string json = File.ReadAllText(path);
        GlobalProgress data = JsonUtility.FromJson<GlobalProgress>(json);

        currentSeriesIndex = data.currentSeriesIndex;
    }

    // ========== УПРАВЛЕНИЕ ИМЕНАМИ СЛОТОВ ==========

    public static void SaveSlotName(string name)
    {
        // Сохраняет имя для текущего слота
        SlotMeta meta = new SlotMeta();
        meta.slotName = name;

        string json = JsonUtility.ToJson(meta);
        File.WriteAllText(GetMetaPath(currentSlot), json);
    }

    public static string LoadSlotName(int slot)
    {
        // Загружает имя слота (или "Пустой слот" если нет)
        string path = GetMetaPath(slot);

        if (!File.Exists(path))
            return "Пустой слот";

        string json = File.ReadAllText(path);
        SlotMeta meta = JsonUtility.FromJson<SlotMeta>(json);

        return meta.slotName;
    }

    // ========== ВНУТРЕННИЕ МЕТОДЫ РАБОТЫ СО СПИСКАМИ СЛОТОВ ==========

    private static void SaveSlots(SlotList list)
    {
        // Сохраняет список всех слотов
        string json = JsonUtility.ToJson(list);
        File.WriteAllText(SlotsPath, json);
    }

    public static List<int> LoadSlots()
    {
        // Загружает список всех слотов (создаёт дефолтный если нет)
        if (!File.Exists(SlotsPath))
        {
            var list = new SlotList();
            list.slots.Add(0);
            SaveSlots(list);

            currentSlot = 0;
            SaveSlotName("Слот 1");

            return list.slots;
        }

        string json = File.ReadAllText(SlotsPath);
        return JsonUtility.FromJson<SlotList>(json).slots;
    }

    public static void SaveSlotsExternally(List<int> slots)
    {
        // Внешний метод для сохранения списка слотов
        SlotList list = new SlotList();
        list.slots = slots;
        SaveSlots(list);
    }

    // ========== ОЧИСТКА ПРОГРЕССА ==========

    public static void DeleteAllProgress()
    {
        // Полностью очищает все файлы сохранения текущего слота
        string savePath = GetSavePath(currentSlot);
        string varsPath = savePath + "_vars";
        string progressPath = Path.Combine(Application.persistentDataPath, $"progress_{currentSlot}.json");

        if (File.Exists(savePath))
            File.Delete(savePath);

        if (File.Exists(varsPath))
            File.Delete(varsPath);

        if (File.Exists(progressPath))
            File.Delete(progressPath);

        currentSeriesIndex = 0;
    }
}