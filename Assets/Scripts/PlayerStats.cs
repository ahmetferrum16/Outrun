using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float normalSpeed = 5f;
    public float sprintSpeed = 10f;
    public float maxStamina = 100f;
    public float staminaRecoverRate = 10f;


    public bool hasDash = false;
    public bool hasBlackDash = false;

    public float dashCooldown = 10f;
    public float dashDistance = 2f;

    public float cameraZoomStep = 0.5f;

    public int maxHealth = 3;
    public int currentHealth = 3;

    public HealthUIManager healthUI;


    public void UnlockDash()
    {
        hasDash = true;
    }

    public void UnlockBlackDash()
    {
        hasBlackDash = true;
    }

    public void ScaleStats(int minute)
    {
        staminaRecoverRate += 0.5f;
        //şuanlık pek bi ekleme yapmak istemiyorum ilerde güncelle.
    }


    public void IncreaseCameraView()
    {
        Camera.main.orthographicSize += cameraZoomStep;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;

        if (healthUI != null)
            healthUI.UpdateHealthDisplay(currentHealth, maxHealth);
    }


    public void TakeDamage()
    {
        currentHealth--;

        if (healthUI != null)
            healthUI.UpdateHealthDisplay(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            FindObjectOfType<GameOverManager>()?.ShowGameOver();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthUI != null)
            healthUI.UpdateHealthDisplay(currentHealth, maxHealth);
    }

    public void FullHeal()
    {
        currentHealth = maxHealth;
        if (healthUI != null)
            healthUI.UpdateHealthDisplay(currentHealth, maxHealth);
    }






}
