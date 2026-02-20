using UnityEngine;

[CreateAssetMenu(fileName = "New Buff", menuName = "Buff")]
public class Buff : ScriptableObject
{
    public enum BuffType
    {
        Speed,
        Stamina,
        Cooldown,
        SprintSpeed,
        StaminaRegen,
        DashDistance,
        CameraZoom
    }

    public string buffName;
    public BuffType type;
    public float amount;
}
