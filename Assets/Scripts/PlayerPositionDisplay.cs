using TMPro;
using UnityEngine;

public class PlayerPositionDisplay : MonoBehaviour
{
    public Transform player;             // Player referansý
    public TextMeshProUGUI positionText; // Text UI referansý

    void Update()
    {
        if (player != null && positionText != null)
        {
            Vector2 pos = player.position;
            positionText.text = $"X: {pos.x:F2}\nY: {pos.y:F2}";
        }
    }
}
