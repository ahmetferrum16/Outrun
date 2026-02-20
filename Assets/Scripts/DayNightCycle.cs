using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle")]
    [Min(0.01f)] public float cycleDuration = 60f; // Tek faz (gündüz veya gece) süresi
    public GameObject playerFOVObject;

    [Header("State (read-only at runtime)")]
    [SerializeField] private float timer = 0f;
    [SerializeField] private bool isNight = false;
    public bool IsNight => isNight;

    [Header("Debug")]
    public bool debugHotkeys = false;              // ⬅️ DEFAULT: kapalı
    [Min(0f)] public float debugSkipSeconds = 20f; // T ile ileri sarma miktarı (Inspector'dan değiştir)

    public float CycleDuration => cycleDuration;                   // faz süresi (sn)
    public float PhaseProgress01 => Mathf.Clamp01(timer / cycleDuration); // 0..1 ilerleme

    // Event: faz değiştiğinde (true = night, false = day) haber verir
    public delegate void DayNightChange(bool isNight);
    public static event DayNightChange OnDayNightChanged;

    void Start()
    {
        UpdateFOVVisibility(isNight);
    }

    void Update()
    {
        timer += Time.deltaTime;

        while (timer >= cycleDuration)
        {
            timer -= cycleDuration;
            TogglePhase();
        }

        // Debug kısayolu: ihtiyaç olursa sadece bunu aç
        if (debugHotkeys && Input.GetKeyDown(KeyCode.T))
        {
            AddTime(debugSkipSeconds);
        }
    }

    public void AddTime(float seconds)
    {
        if (seconds <= 0f) return;

        timer += seconds;

        while (timer >= cycleDuration)
        {
            timer -= cycleDuration;
            TogglePhase();
        }
    }

    private void TogglePhase()
    {
        isNight = !isNight;
        OnDayNightChanged?.Invoke(isNight);
        UpdateFOVVisibility(isNight);
    }

    private void UpdateFOVVisibility(bool night)
    {
        if (playerFOVObject != null)
            playerFOVObject.SetActive(night);
    }
}
