using UnityEngine;

public class OverlayManager : MonoBehaviour
{
    // ========== ПЕРЕМЕННЫЕ ==========

    private GameObject _currentPanel;
    public GameObject dimmer;

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    public void ShowOverlay(GameObject panel)
    {
        // Показывает затемнение и указанную панель, закрывает предыдущую
        if (_currentPanel != null && _currentPanel != panel)
        {
            _currentPanel.SetActive(false);
        }

        dimmer.SetActive(true);
        panel.SetActive(true);
        _currentPanel = panel;
    }

    public void HideOverlay(GameObject panel = null)
    {
        // Прячет указанную панель (или текущую) и затемнение
        GameObject targetPanel = panel ?? _currentPanel;

        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }

        dimmer.SetActive(false);
        _currentPanel = null;
    }

    public void HideCurrentOverlay()
    {
        // Прячет текущую открытую панель
        HideOverlay();
    }

    public bool IsOverlayActive()
    {
        // Проверяет, открыта ли какая-либо панель
        return _currentPanel != null && _currentPanel.activeSelf;
    }
}