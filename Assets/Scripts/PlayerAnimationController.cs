// PlayerAnimationController.cs
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private string lastDirection = "Down";
    private bool ready = false;
    private bool isDashing = false;
    private bool isDead = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (!ready)
        {
            if (animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                ready = true;
            return;
        }

        if (isDead) return;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isDashing = true;
            Debug.Log("DASH: Player_Dash_" + lastDirection);
            SafePlay("Player_Dash_" + lastDirection);
            return;
        }

        if (isDashing)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1f && stateInfo.IsName("Player_Dash_" + lastDirection))
                isDashing = false;
            return;
        }

        Vector2 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        if (speed > 0.1f)
            UpdateDirectionWalk(vel);
        else
            SafePlay("Player_Idle_" + lastDirection);
    }

    void UpdateDirectionWalk(Vector2 vel)
    {
        float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;

        if (angle > -120f && angle <= -60f)
            lastDirection = "Down";
        else if (angle > -60f && angle <= 0f)
            lastDirection = "RightDown";
        else if (angle > 0f && angle <= 60f)
            lastDirection = "RightUp";
        else if (angle > 60f && angle <= 120f)
            lastDirection = "Up";
        else if (angle > 120f && angle <= 180f)
            lastDirection = "LeftUp";
        else
            lastDirection = "LeftDown";

        SafePlay("Player_Walk_" + lastDirection);
    }

    public void PlayDeath()
    {
        isDead = true;
        SafePlay("Player_Death_" + lastDirection);
    }

    void SafePlay(string stateName)
    {
        if (animator.HasState(0, Animator.StringToHash(stateName)))
            animator.Play(stateName);
        else
            Debug.LogWarning("State bulunamadý: " + stateName);
    }
}