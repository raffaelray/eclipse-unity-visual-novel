using UnityEngine;

public static class TransformExtensions
{
    // ========== ÌÅÒÎÄ-ÐÀÑØÈÐÅÍÈÅ ==========

    public static void ClearChildren(this Transform transform)
    {
        // Óäàëÿåò âñå äî÷åðíèå îáúåêòû ó Transform
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(transform.GetChild(i).gameObject);
        }
    }
}