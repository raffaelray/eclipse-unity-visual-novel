using UnityEngine;

// ========== БАЗОВЫЙ СИНГЛТОН ==========

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    // ========== ПЕРЕМЕННЫЕ ==========

    private static T _instance;
    private static bool _isQuitting = false;

    // ========== ПУБЛИЧНОЕ СВОЙСТВО ==========

    public static T Instance
    {
        // Возвращает экземпляр, создаёт его при первом обращении
        get
        {
            if (_isQuitting)
            {
                Debug.LogWarning($"[Singleton] {typeof(T)} уже уничтожен при выходе из игры.");
                return null;
            }

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    _instance = obj.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    // ========== ЖИЗНЕННЫЙ ЦИКЛ ==========

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this as T)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}