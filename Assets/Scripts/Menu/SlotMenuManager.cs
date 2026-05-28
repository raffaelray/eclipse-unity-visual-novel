using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SlotMenuManager : MonoBehaviour
{
    // ========== ПЕРЕМЕННЫЕ ==========

    public GameObject slotsPanel;           // Панель со списком слотов
    public Transform content;               // Контейнер для кнопок слотов
    public GameObject slotItemPrefab;       // Префаб кнопки слота

    public TextMeshProUGUI currentSlotText; // Текст текущего слота
    public TMP_InputField renameInput;      // Поле для переименования слота
    public GameObject currentSlotTextObject;// Объект с текстом текущего слота

    private int currentSlot;                // ID текущего выбранного слота

    // ========== ЖИЗНЕННЫЙ ЦИКЛ ==========

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Загружаем список слотов, создаём первый если пусто
        var slots = SaveSystem.LoadSlots();

        if (slots.Count == 0)
        {
            SaveSystem.CreateNewSlot();
            slots = SaveSystem.LoadSlots();
        }

        currentSlot = SaveSystem.GetCurrentSlot();
        RefreshSlots();
    }

    // ========== ОТКРЫТИЕ / ЗАКРЫТИЕ ПАНЕЛИ ==========

    public void OpenSlotsPanel()
    {
        // Открываем панель управления слотами
        slotsPanel.SetActive(true);
        RefreshSlots();
    }

    public void CloseSlotsPanel()
    {
        // Закрываем панель
        slotsPanel.SetActive(false);
    }

    // ========== ОБНОВЛЕНИЕ UI ==========

    public void RefreshSlots()
    {
        // Полностью перестраивает список кнопок слотов
        foreach (Transform child in content)
            Destroy(child.gameObject);

        List<int> slots = SaveSystem.LoadSlots();

        foreach (int slot in slots)
        {
            GameObject item = Instantiate(slotItemPrefab, content);
            var ui = item.GetComponent<SlotItemUI>();
            ui.Init(slot, SaveSystem.LoadSlotName(slot), this);
        }

        UpdateCurrentSlotUI();
    }

    private void UpdateCurrentSlotUI()
    {
        // Обновляет отображение имени текущего слота
        currentSlotText.text = SaveSystem.LoadSlotName(currentSlot);
    }

    // ========== ВЫБОР СЛОТА ==========

    public void SelectSlot(int slot)
    {
        // Выбирает слот
        currentSlot = slot;
        SaveSystem.SetSlot(slot);
        UpdateCurrentSlotUI();
    }

    // ========== УДАЛЕНИЕ СЛОТА ==========

    public void DeleteSlot(int slot)
    {
        // Удаляет слот (нельзя удалить последний)
        List<int> slots = SaveSystem.LoadSlots();

        if (slots.Count <= 1)
        {
            Debug.Log("Нельзя удалить единственный слот");
            return;
        }

        slots.Remove(slot);

        // Если удаляем текущий слот — переключаемся на первый
        if (currentSlot == slot)
        {
            currentSlot = slots[0];
            SaveSystem.SetSlot(currentSlot);
        }

        SaveSystem.SaveSlotsExternally(slots);
        SaveSystem.DeleteSlotFiles(slot);

        RefreshSlots();
    }

    // ========== ДОБАВЛЕНИЕ СЛОТА ==========

    public void AddSlot()
    {
        // Создаёт новый слот и переключается на него
        int newSlot = SaveSystem.CreateNewSlot();
        RefreshSlots();
        SelectSlot(newSlot);
    }

    // ========== ПЕРЕИМЕНОВАНИЕ ==========

    public void StartRename()
    {
        // Показывает поле ввода для переименования текущего слота
        renameInput.gameObject.SetActive(true);
        renameInput.text = SaveSystem.LoadSlotName(currentSlot);
        renameInput.ActivateInputField();
    }

    public void ConfirmRename()
    {
        // Сохраняет новое имя слота и закрывает поле ввода
        string newName = renameInput.text.Trim();

        if (string.IsNullOrEmpty(newName))
            newName = "Слот";

        SaveSystem.SaveSlotName(newName);
        renameInput.gameObject.SetActive(false);

        RefreshSlots();
    }
}