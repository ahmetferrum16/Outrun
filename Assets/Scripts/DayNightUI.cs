using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayNightUI : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle dayNight;          // DayNightCycle referansý (Inspector’dan ata)
    public TextMeshProUGUI label;           // "Day" / "Night" yazýsý
    public Image fillImage;                 // Dolum barý veya halka (Image Type = Filled)

    [Header("Style")]
    public string dayText = "DAY";
    public string nightText = "NIGHT";
    public Color dayColor = new Color(1f, 0.95f, 0.6f, 1f);    // sýcak sarý
    public Color nightColor = new Color(0.5f, 0.7f, 1f, 1f);   // soðuk mavi

    void OnEnable()
    {
        DayNightCycle.OnDayNightChanged += HandlePhaseChanged;
        // Ýlk frame’de doðru görünsün
        RefreshStatic();
    }

    void OnDisable()
    {
        DayNightCycle.OnDayNightChanged -= HandlePhaseChanged;
    }

    void Update()
    {
        if (!dayNight) return;

        // Ýlerleme (0..1). Image Type = Filled olduðunda fillAmount ile güncellenir.
        if (fillImage)
            fillImage.fillAmount = dayNight.PhaseProgress01;
    }

    void HandlePhaseChanged(bool isNight)
    {
        RefreshStatic();
    }

    void RefreshStatic()
    {
        if (!dayNight) return;

        bool isNight = dayNight.IsNight;

        if (label)
        {
            label.text = isNight ? nightText : dayText;
            label.color = isNight ? nightColor : dayColor;
        }

        if (fillImage)
        {
            // Gece/gündüz renk geçiþi
            fillImage.color = isNight ? nightColor : dayColor;
        }
    }
}
