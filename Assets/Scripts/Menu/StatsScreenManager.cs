using Ink.Runtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsScreenManager : MonoBehaviour
{
    // ========== ПЕРЕМЕННЫЕ ==========

    public GameObject statsPanel;           // Панель со статистикой
    public Transform contentParent;         // Контейнер для строк статов
    public GameObject statRowPrefab;        // Префаб строки стата

    private Dictionary<string, object> vars;   // Загруженные переменные из сохранения

    // ========== ОТКРЫТИЕ / ЗАКРЫТИЕ ПАНЕЛИ ==========

    public void OpenStats()
    {
        // Открывает панель и загружает свежие данные статов
        vars = SaveSystem.LoadGlobalVariablesRaw();
        statsPanel.SetActive(true);
        ShowStats();
    }

    public void CloseStats()
    {
        // Закрывает панель статистики
        statsPanel.SetActive(false);
    }

    // ========== ПОЛУЧЕНИЕ ЗНАЧЕНИЙ ПЕРЕМЕННЫХ ==========

    private bool TryGetVar(string name, out object value)
    {
        // Пытается получить значение переменной из загруженного словаря
        if (vars != null && vars.ContainsKey(name))
        {
            value = vars[name];
            return true;
        }

        value = null;
        return false;
    }

    // ========== ОТОБРАЖЕНИЕ СТАТОВ ==========

    public void ShowStats()
    {
        // Показывает все статы из базы данных
        Clear();

        foreach (var stat in StatsDatabase.Instance.stats)
        {
            if (TryGetVar(stat.inkVariable, out object value))
            {
                CreateRow(stat.icon, stat.displayName, value.ToString());
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
    }

    // ========== ОТОБРАЖЕНИЕ ОТНОШЕНИЙ ==========

    public void ShowRelations()
    {
        // Показывает все отношения из базы данных
        Clear();

        foreach (var rel in StatsDatabase.Instance.relations)
        {
            if (TryGetVar(rel.inkVariable, out object value))
            {
                CreateRow(rel.icon, rel.characterName, value.ToString());
            }
        }
    }

    // ========== ОТОБРАЖЕНИЕ ФЛАГОВ ==========

    public void ShowFlags()
    {
        // Показывает только активные флаги (где значение true)
        Clear();

        foreach (var flag in StatsDatabase.Instance.flags)
        {
            if (TryGetVar(flag.inkVariable, out object value))
            {
                if ((bool)value)
                    CreateRow(null, flag.description, "");
            }
        }
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ UI ==========

    private void CreateRow(Sprite icon, string name, string value)
    {
        // Создаёт одну строку в таблице статов
        var row = Instantiate(statRowPrefab, contentParent);
        row.transform.Find("NameText").GetComponent<TMP_Text>().text = name;
        row.transform.Find("ValueText").GetComponent<TMP_Text>().text = value;

        var iconImg = row.transform.Find("Icon").GetComponent<Image>();
        if (icon != null)
            iconImg.sprite = icon;
        else
            iconImg.gameObject.SetActive(false);
    }

    private void Clear()
    {
        // Удаляет все дочерние элементы из контейнера
        contentParent.ClearChildren();
    }
}