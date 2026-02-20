using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUIManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform container;          // Kalplerin ebeveyni (Canvas altýnda bir boþ GO)
    public GameObject heartPrefab;       // Ýçinde Image olan küçük bir UI prefab
    public Sprite fullHeart;
    public Sprite emptyHeart;

    [Header("Options")]
    public bool rebuildOnEveryUpdate = false; // Max aynýysa yeniden inþa etme

    private readonly List<Image> _hearts = new List<Image>();
    private int _cachedMax = -1;

    /// <summary>Max deðiþtiðinde kalpleri yeniden oluþtur.</summary>
    void Rebuild(int max)
    {
        if (!container || !heartPrefab) return;

        // Fazla çocuklarý sil
        for (int i = _hearts.Count - 1; i >= max; i--)
        {
            if (_hearts[i]) Destroy(_hearts[i].gameObject);
            _hearts.RemoveAt(i);
        }

        // Eksikse ekle
        while (_hearts.Count < max)
        {
            var go = Instantiate(heartPrefab, container);
            var img = go.GetComponent<Image>();
            if (!img) img = go.AddComponent<Image>();
            _hearts.Add(img);
        }

        _cachedMax = max;
    }

    /// <summary>UI’yi güncelle (gerekirse rebuild eder).</summary>
    public void UpdateHealthDisplay(int current, int max)
    {
        if (rebuildOnEveryUpdate || _cachedMax != max || _hearts.Count != max)
            Rebuild(max);

        current = Mathf.Clamp(current, 0, max);

        for (int i = 0; i < _hearts.Count; i++)
        {
            var img = _hearts[i];
            if (!img) continue;

            img.sprite = (i < current) ? fullHeart : emptyHeart;
            img.enabled = true; // güvenlik
        }
    }
}
