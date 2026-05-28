using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotItemUI : MonoBehaviour
{
    // ========== ПЕРЕМЕННЫЕ ==========

    public TextMeshProUGUI slotText;      // Текст с именем слота
    public Button selectButton;           // Кнопка выбора слота
    public Button deleteButton;           // Кнопка удаления слота

    private int slotIndex;                // ID текущего слота
    private SlotMenuManager manager;      // Ссылка на менеджера слотов

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    public void Init(int slot, string name, SlotMenuManager m)
    {
        // Инициализирует UI-элемент слота: устанавливает имя и вешает обработчики
        slotIndex = slot;
        manager = m;

        slotText.text = name;

        // Настраиваем кнопку выбора
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            manager.SelectSlot(slotIndex);
        });

        // Настраиваем кнопку удаления
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() =>
        {
            manager.DeleteSlot(slotIndex);
        });
    }
}