// PlayerMovement.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    public PlayerStats stats;
    public Slider staminaBar;
    public TextMeshProUGUI dashCooldownText;
    private float currentStamina;
    private float lastDashTime = -Mathf.Infinity;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool isInvulnerable = false;
    private Rigidbody2D rb;
    private Vector2 movement = Vector2.zero;
    private float currentSpeed;
    private bool isDashingMovement = false;
    private bool isDead = false;


    private IEnumerator DashRoutine()
    {
        isDashingMovement = true;
        float dashDuration = 0.15f;
        float elapsed = 0f;
        Vector2 dashDir = lastMoveDirection;

        if (stats.hasBlackDash)
            StartCoroutine(InvulnerabilityRoutine(dashDuration));

        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashDir * (stats.dashDistance / dashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashingMovement = false;
    }

    void Start()
    {
        currentStamina = stats.maxStamina;
        staminaBar.maxValue = stats.maxStamina;
        staminaBar.value = currentStamina;
        stats.ResetHealth();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on player!");
    }

    void Update()
    {
        if (isDead) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        movement = new Vector2(moveX, moveY).normalized;

        if (movement != Vector2.zero)
            lastMoveDirection = movement;

        if (stats.hasDash && Input.GetKeyDown(KeyCode.LeftShift)
            && Time.time >= lastDashTime + stats.dashCooldown
            && !isDashingMovement)
        {
            lastDashTime = Time.time;
            StartCoroutine(DashRoutine());
        }

        currentSpeed = stats.normalSpeed;
        if (Input.GetKey(KeyCode.Space) && currentStamina > 0)
        {
            currentSpeed = stats.sprintSpeed;
            currentStamina -= 20f * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
        }
        else
        {
            currentStamina += stats.staminaRecoverRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, stats.maxStamina);
        }

        staminaBar.maxValue = stats.maxStamina;
        staminaBar.value = currentStamina;

        float timeLeft = Mathf.Max(0, (lastDashTime + stats.dashCooldown) - Time.time);
        dashCooldownText.text = stats.hasDash
            ? (timeLeft > 0 ? $"Dash: {timeLeft:F1}s" : "Dash Ready")
            : "Dash Locked";
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (!isDashingMovement)
            rb.linearVelocity = movement * currentSpeed;
    }

    public void UpdateStaminaBarVisual()
    {
        staminaBar.maxValue = stats.maxStamina;
        staminaBar.value = currentStamina;
    }

    public IEnumerator InvulnerabilityRoutine(float duration)
    {
        isInvulnerable = true;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
        yield return new WaitForSeconds(duration);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
        isInvulnerable = false;
    }

    public bool IsInvulnerable() => isInvulnerable;

    public void StartInvulnerability(float duration)
    {
        StartCoroutine(InvulnerabilityRoutine(duration));
    }

    public void RefillStamina()
    {
        currentStamina = stats.maxStamina;
        UpdateStaminaBarVisual();
    }

    public void SetDead()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        movement = Vector2.zero;
    }

}