using UnityEngine;

// ========== ОПРЕДЕЛЕНИЕ СТАТА ==========

[System.Serializable]
public class StatDefinition
{
    public string inkVariable;      // Имя переменной в Ink-стори
    public string displayName;      // Отображаемое имя в UI
    public Sprite icon;             // Иконка стата
}

// ========== ОПРЕДЕЛЕНИЕ ОТНОШЕНИЯ ==========

[System.Serializable]
public class RelationDefinition
{
    public string inkVariable;      // Имя переменной в Ink-стори
    public string characterName;    // Имя персонажа
    public Sprite icon;             // Иконка отношения
}

// ========== ОПРЕДЕЛЕНИЕ ФЛАГА ==========

[System.Serializable]
public class FlagDefinition
{
    public string inkVariable;      // Имя переменной в Ink-стори
    public string description;      // Описание события/флага
}