using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // ========== ПЕРЕМЕННЫЕ ==========

    [Header("UI")]
    public OverlayManager overlayManager;
    public TextMeshProUGUI seriesTitleText;
    public Button continueButton;
    public GameObject confirmResetPanel;
    public GameObject quitPanel;
    public GameObject statsPanel;
    public GameObject slotsPanel;

    // ========== ЖИЗНЕННЫЙ ЦИКЛ ==========

    void OnEnable()
    {
        // Подписываемся на событие смены слота сохранения
        SaveSystem.OnSlotChanged += UpdateSeriesTitle;
    }

    void OnDisable()
    {
        // Отписываемся от события
        SaveSystem.OnSlotChanged -= UpdateSeriesTitle;
    }

    void Start()
    {
        // Загружаем прогресс при старте
        SaveSystem.LoadProgress();
        UpdateSeriesTitle();
    }

    void Update()
    {
        // Обработка нажатия Escape
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (quitPanel.activeSelf)
            {
                overlayManager.HideOverlay(quitPanel);
                return;
            }

            if (confirmResetPanel.activeSelf)
            {
                overlayManager.HideOverlay(confirmResetPanel);
                return;
            }

            if (statsPanel.activeSelf)
            {
                statsPanel.SetActive(false);
                return;
            }

            if (slotsPanel.activeSelf)
            {
                slotsPanel.SetActive(false);
                return;
            }

            // Если ничего не открыто - открываем выход
            AskQuit();
        }
    }

    // ========== ПРОДОЛЖИТЬ / НАЧАТЬ ==========

    public void ContinueSeries()
    {
        // Если сохранения нет - начнется первая серия с нуля
        SceneManager.LoadScene("GameScene");
    }

    // ========== ВЫХОД ==========

    public void AskQuit()
    {
        // Показываем подтверждение выхода
        overlayManager.ShowOverlay(quitPanel);
    }

    public void CancelQuit()
    {
        // Отменяем выход
        if (quitPanel.activeSelf)
            overlayManager.HideOverlay(quitPanel);
    }

    public void QuitGame()
    {
        // Закрываем приложение
        Application.Quit();
    }

    // ========== СБРОС ПРОГРЕССА ==========

    public void AskResetProgress()
    {
        // Показываем подтверждение сброса
        overlayManager.ShowOverlay(confirmResetPanel);
    }

    public void CancelReset()
    {
        // Отменяем сброс
        if (confirmResetPanel.activeSelf)
            overlayManager.HideOverlay(confirmResetPanel);
    }

    public void ResetProgress()
    {
        // Полностью очищаем сохранения
        SaveSystem.DeleteAllProgress();
        UpdateSeriesTitle();
        Debug.Log("Progress reset");
        CancelReset();
    }

    // ========== UI ХЕЛПЕРЫ ==========

    private void UpdateSeriesTitle()
    {
        // Обновляет заголовок серии и активность кнопки Continue
        int index = SaveSystem.currentSeriesIndex;
        var list = SeriesManager.Instance.seriesList;

        if (index >= list.Length)
        {
            seriesTitleText.text = "История завершена";
            continueButton.interactable = false;
            return;
        }

        var series = list[index];
        seriesTitleText.text = "Текущая серия: " + series.seriesName;
        continueButton.interactable = true;
    }

    // ========== ОВЕРЛЕЙ ==========

    public void OnDimmerClicked()
    {
        // Закрываем открытые панели по клику на затемнение
        if (confirmResetPanel.activeSelf)
            overlayManager.HideOverlay(confirmResetPanel);
        if (quitPanel.activeSelf)
            overlayManager.HideOverlay(quitPanel);
    }
}