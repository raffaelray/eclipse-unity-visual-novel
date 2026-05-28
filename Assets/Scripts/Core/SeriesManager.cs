using UnityEngine;

public class SeriesManager : Singleton<SeriesManager>
{
    // ========== ВНУТРЕННИЕ КЛАССЫ ==========

    [System.Serializable]
    public class SeriesData
    {
        public string seriesName;   // Название серии
        public TextAsset inkJSON;   // Ink-файл с диалогами серии
    }

    // ========== ПЕРЕМЕННЫЕ ==========

    public SeriesData[] seriesList;        // Массив всех серий

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    public SeriesData GetCurrentSeries()
    {
        // Возвращает текущую серию по индексу из SaveSystem
        int index = SaveSystem.currentSeriesIndex;
        return seriesList[Mathf.Clamp(index, 0, seriesList.Length - 1)];
    }
}