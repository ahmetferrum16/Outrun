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
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        movement = new Vector2(moveX, moveY).normalized;

        if (movement != Vector2.zero)
            lastMoveDirection = movement;

        if (stats.hasDash && Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + stats.dashCooldown)
        {
            Vector2 dashTarget = rb.position + lastMoveDirection * stats.dashDistance;
            rb.MovePosition(dashTarget);
            lastDashTime = Time.time;

            if (stats.hasBlackDash)
                StartCoroutine(InvulnerabilityRoutine(1f));
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
        rb.velocity = movement * currentSpeed;
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
}