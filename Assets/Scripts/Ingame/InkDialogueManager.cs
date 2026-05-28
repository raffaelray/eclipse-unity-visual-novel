using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InkDialogueManager : MonoBehaviour
{
    // ========== ПЕРЕМЕННЫЕ ==========

    [Header("Ink")]
    public TextAsset inkJSON;
    private Story story;
    public int slidesCount = 0;

    [Header("UI")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI bodyText;
    public OverlayManager overlayManager;

    [Header("Skip")]
    public Button skipButton;
    private bool skipHeld = false;
    public float skipDelay = 0.03f;
    private float skipTimer = 0f;

    [Header("Exit Confirm")]
    public GameObject exitConfirmPanel;

    [Header("Choices")]
    public GameObject choicesPanel;
    public Button choiceButtonPrefab;

    [Header("Character")]
    public Image characterImage;
    public RectTransform textBox;
    public float textOffset = 720f;
    public float characterPadding = 120f;
    public float characterTopPadding = 20f;
    public float characterBottomPadding = 0f;

    [Header("Text Notifications")]
    public GameObject notifyIndicator;
    public GameObject notifyPopup;
    public TextMeshProUGUI notifyText;
    private string pendingNotification;
    public CanvasGroup notifyIndicatorCanvasGroup;
    private int notificationSlideCounter = 0;
    public int notificationMaxSlides = 3;

    [Header("Stat Notifications")]
    public Transform statNotificationContainer;
    public GameObject statNotificationPrefab;
    private Dictionary<string, int> previousStats = new Dictionary<string, int>();

    [Header("Stats Screen")]
    public GameObject statsPanel;
    public Transform statsContainer;
    public GameObject statItemPrefab;

    [Header("Series End")]
    public GameObject seriesEndPanel;
    public TextMeshProUGUI slidesCountText;
    private bool isSeriesEnded = false;

    [Header("Navigation")]
    private Stack<DialogueSnapshot> history = new Stack<DialogueSnapshot>();
    [System.Serializable]
    private class DialogueSnapshot
    {
        public string inkStateJson;
        public int slidesCount;
    }

    // ========== ЖИЗНЕННЫЙ ЦИКЛ ==========

    void Start()
    {
        var series = SeriesManager.Instance.GetCurrentSeries();
        story = new Story(series.inkJSON.text);

        bool loaded = SaveSystem.LoadInkState(story);

        if (loaded)
        {
            RefreshViewFromState();
        }
        else
        {
            SaveSystem.LoadGlobalVariables(story);
            slidesCount = 0;
            ShowNextLine();
            CacheInitialStats();
        }

        if (skipButton != null)
        {
            var trigger = skipButton.gameObject.AddComponent<EventTrigger>();

            var pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { skipHeld = true; });
            trigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { skipHeld = false; });
            trigger.triggers.Add(pointerUp);

            var exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((data) => { skipHeld = false; });
            trigger.triggers.Add(exit);
        }
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (seriesEndPanel.activeSelf)
            {
                ExitToMenu();
                return;
            }

            if (notifyPopup.activeSelf)
            {
                overlayManager.HideOverlay(notifyPopup);
                return;
            }

            if (statsPanel.activeSelf)
            {
                overlayManager.HideOverlay(statsPanel);
                return;
            }

            if (exitConfirmPanel.activeSelf)
            {
                overlayManager.HideOverlay(exitConfirmPanel);
                return;
            }

            ExitToMenuWithConfirm();
        }

        if (seriesEndPanel.activeSelf)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ExitToMenu();
            }
            return;
        }

        if (statsPanel.activeSelf || notifyPopup.activeSelf || exitConfirmPanel.activeSelf)
        {
            return;
        }

        if (story == null)
            return;

        bool skip = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed || skipHeld;

        if (skip && story.currentChoices.Count == 0)
        {
            skipTimer += Time.deltaTime;
            if (skipTimer >= skipDelay)
            {
                skipTimer = 0f;
                ShowNextLine();
            }
            return;
        }
        else
        {
            skipTimer = 0f;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame && story.currentChoices.Count == 0)
        {
            OnPrevPressed();
            return;
        }

        if ((Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame) && story.currentChoices.Count == 0)
        {
            ShowNextLine();
        }
    }

    void OnApplicationQuit()
    {
        // Сохраняем прогресс при закрытии игры
        if (!isSeriesEnded)
        {
            SaveSystem.SaveInkState(story);
            SaveSystem.SaveGlobalVariables(story);
        }
    }

    // ========== ОСНОВНОЙ ДИАЛОГ ==========

    private string ProcessFormatting(string text)
    {
        // Преобразует *текст* в <i>текст</i> (курсив)
        text = Regex.Replace(text, "\\*(.*?)\\*", "<i>$1</i>");
        return text;
    }

    private void RefreshViewFromState()
    {
        // Восстанавливает UI из сохраненного состояния стори
        if (string.IsNullOrEmpty(story.currentText))
        {
            if (story.canContinue)
                story.Continue();
        }

        string text = story.currentText.Trim();
        text = ProcessFormatting(text);
        bodyText.text = text;

        HandleTags(story.currentTags);
        RefreshUI();
        CacheInitialStats();
    }

    private void SaveStepSnapshot()
    {
        // Сохраняет состояние на истории текущий слайд
        history.Push(new DialogueSnapshot
        {
            inkStateJson = story.state.ToJson(),
            slidesCount = slidesCount
        });
    }

    public void ShowNextLine()
    {
        // Показывает следующую строку диалога
        if (story.currentChoices.Count > 0)
            return;

        if (story.canContinue)
        {
            SaveStepSnapshot();

            string text = story.Continue().Trim();
            slidesCount++;

            text = ProcessFormatting(text);
            bodyText.text = text;

            HandleTags(story.currentTags);
            CheckStatChanges();
            RefreshUI();
        }
        else
        {
            bodyText.text = "Конец серии. Тут ничего нет.";
            speakerText.text = "";
        }

        UpdateNotificationLifetime();
    }

    public void OnNextPressed()
    {
        // Вызывается с кнопки "Далее"
        if (story == null)
            return;

        if (story.currentChoices.Count > 0)
            return;

        ShowNextLine();
    }

    public void OnPrevPressed()
    {
        if (story == null)
            return;

        if (history.Count == 0)
            return;

        var snapshot = history.Pop();
        story.state.LoadJson(snapshot.inkStateJson);
        slidesCount = snapshot.slidesCount;

        RefreshViewFromState();
        CacheInitialStats();
    }

    // ========== ТЕГИ И ПЕРСОНАЖИ ==========

    private void HandleTags(List<string> tags)
    {
        // Обрабатывает инк-теги: speaker, notify, end_series, sprite, side
        speakerText.text = "";
        bool hasSprite = false;
        bool sideSet = false;

        foreach (string tag in tags)
        {
            if (tag.StartsWith("speaker:"))
            {
                speakerText.text = tag.Substring("speaker:".Length);
            }
            else if (tag.StartsWith("notify:"))
            {
                string message = tag.Substring("notify:".Length);
                ShowCustomNotification(message);
            }
            else if (tag == "end_series")
            {
                EndSeries();
            }
            else if (tag.StartsWith("sprite:"))
            {
                string spriteName = tag.Substring("sprite:".Length);
                ShowCharacter(spriteName);
                hasSprite = true;
            }
            else if (tag.StartsWith("side:"))
            {
                string side = tag.Substring("side:".Length);
                SetCharacterSide(side);
                sideSet = true;
            }
        }

        if (hasSprite && !sideSet)
        {
            SetCharacterSide("right");
        }

        if (!hasSprite)
        {
            HideCharacter();
        }
    }

    private void ShowCharacter(string spriteName)
    {
        // Показывает спрайт персонажа из Resources/Sprites/
        characterImage.preserveAspect = true;
        Sprite sprite = Resources.Load<Sprite>("Sprites/new/" + spriteName);

        if (sprite != null)
        {
            characterImage.sprite = sprite;
            characterImage.gameObject.SetActive(true);
        }
    }

    private void HideCharacter()
    {
        // Прячет спрайт персонажа и возвращает текстовое поле на место
        characterImage.gameObject.SetActive(false);
        textBox.offsetMin = new Vector2(0, textBox.offsetMin.y);
        textBox.offsetMax = new Vector2(0, textBox.offsetMax.y);
    }

    private void SetCharacterSide(string side)
    {
        // Ставит спрайт персонажа слева или справа, сдвигает текстовое поле
        RectTransform rt = characterImage.rectTransform;

        if (side == "right")
        {
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-characterPadding, 0);
            textBox.offsetMax = new Vector2(-textOffset, textBox.offsetMax.y);
            textBox.offsetMin = new Vector2(0, textBox.offsetMin.y);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(characterPadding, 0);
            textBox.offsetMin = new Vector2(textOffset, textBox.offsetMin.y);
            textBox.offsetMax = new Vector2(0, textBox.offsetMax.y);
        }

        rt.offsetMin = new Vector2(rt.offsetMin.x, characterBottomPadding);
        rt.offsetMax = new Vector2(rt.offsetMax.x, -characterTopPadding);
    }

    // ========== ВЫБОРЫ ==========

    private void RefreshUI()
    {
        // Обновляет UI: очищает и показывает варианты выбора при наличии
        ClearChoices();

        if (story.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
    }

    private void DisplayChoices()
    {
        // Создает кнопки для всех вариантов выбора
        foreach (Choice choice in story.currentChoices)
        {
            Button button = Instantiate(choiceButtonPrefab, choicesPanel.transform);
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            text.text = choice.text;
            button.onClick.AddListener(() => OnChoiceSelected(choice));
        }
    }

    private void OnChoiceSelected(Choice choice)
    {
        // Обрабатывает выбор игрока
        story.ChooseChoiceIndex(choice.index);
        history.Clear(); // выбор = точка невозврата назад

        ClearChoices();
        ShowNextLine();

        CacheInitialStats();
    }

    private void ClearChoices()
    {
        // Удаляет все кнопки выбора
        choicesPanel.transform.ClearChildren();
    }

    // ========== СТАТЫ ==========

    private void CacheInitialStats()
    {
        // Сохраняет начальные значения статов для отслеживания изменений
        previousStats.Clear();

        foreach (var stat in StatsDatabase.Instance.stats)
        {
            string varName = stat.inkVariable;
            if (story.variablesState.Contains(varName))
            {
                int value = (int)story.variablesState[varName];
                previousStats[varName] = value;
            }
        }
    }

    private void CheckStatChanges()
    {
        // Проверяет, изменились ли статы после последнего шага
        foreach (var stat in StatsDatabase.Instance.stats)
        {
            string varName = stat.inkVariable;
            if (!story.variablesState.Contains(varName))
                continue;

            int currentValue = (int)story.variablesState[varName];
            if (!previousStats.ContainsKey(varName))
            {
                previousStats[varName] = currentValue;
                continue;
            }

            int previousValue = previousStats[varName];
            int diff = currentValue - previousValue;

            if (diff != 0)
            {
                ShowStatNotification(stat, diff);
                previousStats[varName] = currentValue;
            }
        }
    }

    private void ShowStatNotification(StatDefinition stat, int diff)
    {
        // Показывает всплывающее уведомление об изменении стата
        GameObject notif = Instantiate(statNotificationPrefab, statNotificationContainer);

        Image iconImage = notif.transform.Find("IconImage").GetComponent<Image>();
        TextMeshProUGUI text = notif.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();

        iconImage.sprite = stat.icon;
        iconImage.enabled = stat.icon != null;

        string sign = diff > 0 ? "+" : "";
        text.text = $"{sign}{diff} {stat.displayName}";

        StartCoroutine(StatNotificationLife(notif));
    }

    private IEnumerator StatNotificationLife(GameObject notif)
    {
        // Анимация жизни уведомления: появление -> ожидание -> исчезновение
        CanvasGroup cg = notif.GetComponent<CanvasGroup>();
        float fadeInTime = 0.25f;
        float visibleTime = 1.5f;
        float fadeOutTime = 0.25f;

        float t = 0;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, t / fadeInTime);
            yield return null;
        }
        cg.alpha = 1;

        yield return new WaitForSeconds(visibleTime);

        t = 0;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, t / fadeOutTime);
            yield return null;
        }

        Destroy(notif);
    }

    public void OpenStatsScreen()
    {
        // Открывает панель со всеми статами
        BuildStatsUI();
        overlayManager.ShowOverlay(statsPanel);
    }

    private void BuildStatsUI()
    {
        // Строит UI-список всех статов
        foreach (Transform child in statsContainer)
            Destroy(child.gameObject);

        foreach (var stat in StatsDatabase.Instance.stats)
        {
            if (!story.variablesState.Contains(stat.inkVariable))
                continue;

            int value = (int)story.variablesState[stat.inkVariable];
            GameObject item = Instantiate(statItemPrefab, statsContainer);

            Image icon = item.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = stat.icon;
            icon.enabled = stat.icon != null;

            TextMeshProUGUI valueText = item.transform.Find("Value").GetComponent<TextMeshProUGUI>();
            valueText.text = value.ToString();
        }
    }

    // ========== УВЕДОМЛЕНИЯ ==========

    private void ShowCustomNotification(string message)
    {
        // Показывает иконку-индикатор уведомления
        pendingNotification = message;
        notifyIndicator.SetActive(true);
        notifyIndicatorCanvasGroup.alpha = 1;
        notifyIndicatorCanvasGroup.blocksRaycasts = true;
        notificationSlideCounter = 0;
    }

    private void UpdateNotificationLifetime()
    {
        // Увеличивает счётчик слайдов и гасит индикатор при достижении лимита
        if (!notifyIndicator.activeSelf)
            return;

        notificationSlideCounter++;

        if (notificationSlideCounter >= notificationMaxSlides)
        {
            StartCoroutine(FadeOutNotificationIndicator());
        }
    }

    private IEnumerator FadeOutNotificationIndicator()
    {
        // Плавно прячет индикатор уведомления
        CanvasGroup cg = notifyIndicatorCanvasGroup;
        float t = 0;
        float duration = 0.5f;

        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, t / duration);
            yield return null;
        }

        cg.alpha = 0;
        cg.blocksRaycasts = false;
        notifyIndicator.SetActive(false);
    }

    public void OpenNotification()
    {
        // Открывает попап с кастомным текстом уведомления
        overlayManager.ShowOverlay(notifyPopup);
        notifyText.text = pendingNotification;
        notifyIndicator.SetActive(false);
    }

    // ========== КОНЕЦ СЕРИИ ==========

    public void EndSeries()
    {
        // Завершает серию, сохраняет прогресс и показывает финальную панель
        if (isSeriesEnded) return;
        isSeriesEnded = true;

        Debug.Log("SERIES END");

        SaveSystem.SaveGlobalVariables(story);
        SaveSystem.DeleteSave();
        SaveSystem.currentSeriesIndex++;
        SaveSystem.SaveProgress();

        slidesCountText.text = "Слайдов прочитано: " + slidesCount;
        overlayManager.ShowOverlay(seriesEndPanel);
    }

    // ========== ВЫХОД ==========

    public void ExitToMenuWithConfirm()
    {
        // Показывает подтверждение выхода
        overlayManager.ShowOverlay(exitConfirmPanel);
    }

    public void CancelExit()
    {
        // Отменяет выход
        overlayManager.HideOverlay(exitConfirmPanel);
    }

    public void ExitToMenu()
    {
        // Выходит в главное меню с сохранением
        if (!isSeriesEnded)
        {
            SaveSystem.SaveInkState(story);
            SaveSystem.SaveGlobalVariables(story);
        }
        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        // Ждет отпускания кнопки мыши/пальца перед загрузкой сцены
        while ((Mouse.current != null && Mouse.current.leftButton.isPressed) ||
               (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed))
        {
            yield return null;
        }

        SceneManager.LoadScene("MenuScene");
    }

    // ========== ОВЕРЛЕЙ ==========

    public void OnDimmerClicked()
    {
        // Закрывает открытые панели по клику на затемнение
        if (statsPanel.activeSelf)
            overlayManager.HideOverlay(statsPanel);

        if (notifyPopup.activeSelf)
            overlayManager.HideOverlay(notifyPopup);
    }
}