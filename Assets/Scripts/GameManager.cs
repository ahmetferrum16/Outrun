using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public GameObject buffSelectionUI;
    public PlayerStats playerStats;
    public EnemyManager enemyManager;

    public List<Buff> allBuffs;
    public BuffButton[] buffButtons;

    public EnemySpawner enemySpawner;

    [Header("References")]
    public DayNightCycle dayNightCycle;
    public PlayerMovement playerMovement;

    private float elapsedTime = 0f;
    private int lastProcessedMinute = 0;

    void Awake()
    {
        if (!playerMovement)
            playerMovement = FindObjectOfType<PlayerMovement>();
    }

    void OnEnable()
    {
        DayNightCycle.OnDayNightChanged += HandleDayNightChange;
    }

    void OnDisable()
    {
        DayNightCycle.OnDayNightChanged -= HandleDayNightChange;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        if (timerText) timerText.text = $"Time: {minutes:00}:{seconds:00}";

        if (minutes > lastProcessedMinute)
        {
            lastProcessedMinute = minutes;
            OnNewMinute(minutes);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            float skip = dayNightCycle ? Mathf.Max(0f, dayNightCycle.debugSkipSeconds) : 20f;
            SkipTime(skip);
            if (dayNightCycle) dayNightCycle.AddTime(skip);
        }
    }

    public void SkipTime(float seconds)
    {
        elapsedTime += seconds;
    }

    private void HandleDayNightChange(bool isNight)
    {
        if (!isNight)
            ShowRandomBuffs();
    }

    void OnNewMinute(int minute)
    {
        if (minute == 2) playerStats?.UnlockDash();
        if (minute == 5) playerStats?.UnlockBlackDash();

        playerStats?.ScaleStats(minute);
        enemyManager?.ScaleEnemyStats(minute);
        enemySpawner?.ScaleSpawnRate(minute);
    }

    void ShowRandomBuffs()
    {
        if (allBuffs == null || allBuffs.Count < 3)
        {
            Debug.LogError("Buff list must contain at least 3 unique buffs!");
            return;
        }

        List<Buff> selected = new List<Buff>();
        HashSet<Buff.BuffType> selectedTypes = new HashSet<Buff.BuffType>();

        int attempts = 0;
        while (selected.Count < 3 && attempts < 100)
        {
            Buff random = allBuffs[Random.Range(0, allBuffs.Count)];
            if (!selectedTypes.Contains(random.type))
            {
                selected.Add(random);
                selectedTypes.Add(random.type);
            }
            attempts++;
        }

        if (selected.Count < 3)
        {
            Debug.LogWarning("Could not select 3 unique buff types.");
            return;
        }

        for (int i = 0; i < 3; i++)
            buffButtons[i].Setup(selected[i], this);

        buffSelectionUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ApplyBuff(Buff buff)
    {
        if (playerStats == null)
        {
            Debug.LogWarning("ApplyBuff called but playerStats is null.");
            return;
        }

        switch (buff.type)
        {
            case Buff.BuffType.Speed:
                playerStats.normalSpeed += buff.amount;
                break;
            case Buff.BuffType.SprintSpeed:
                playerStats.sprintSpeed += buff.amount;
                break;
            case Buff.BuffType.Stamina:
                playerStats.maxStamina += buff.amount;
                if (playerMovement)
                    playerMovement.UpdateStaminaBarVisual();
                break;
            case Buff.BuffType.StaminaRegen:
                playerStats.staminaRecoverRate += buff.amount;
                break;
            case Buff.BuffType.Cooldown:
                playerStats.dashCooldown = Mathf.Max(1f, playerStats.dashCooldown - buff.amount);
                break;
            case Buff.BuffType.DashDistance:
                playerStats.dashDistance += buff.amount;
                break;
            case Buff.BuffType.CameraZoom:
                playerStats.IncreaseCameraView();
                break;
        }

        buffSelectionUI.SetActive(false);
        Time.timeScale = 1f;
    }
}