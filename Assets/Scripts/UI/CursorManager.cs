using UnityEngine;

public class CursorManager : MonoBehaviour
{
    // ========== ПЕРЕМЕННЫЕ ==========

    public Texture2D cursorTexture;   // Текстура кастомного курсора
    public Vector2 hotspot = Vector2.zero;   // Точка клика (обычно 0,0 - левый верхний угол)

    // ========== ЖИЗНЕННЫЙ ЦИКЛ ==========

    void Start()
    {
        // Устанавливаем кастомный курсор
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}