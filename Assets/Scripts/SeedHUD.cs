// SeedHUD.cs
using TMPro;
using UnityEngine;

public class SeedHUD : MonoBehaviour
{
    public TextMeshProUGUI seedText;

    void Start()
    {
        UpdateText();
    }

    void OnEnable()
    {
        UpdateText();
    }

    void UpdateText()
    {
        int seed = 0;

        if (GameSession.I != null)
            seed = GameSession.I.WorldSeed;
        else
        {
            // Fallback: sahneden çek (olmazsa 0 yazýlýr)
            var cl = FindObjectOfType<ChunkLoader>();
            if (cl) seed = cl.worldSeed;
            else
            {
                var sp = FindObjectOfType<EnemySpawner>();
                if (sp) seed = sp.baseSeed;
            }
        }

        if (seedText) seedText.text = $"Seed: {seed}";
    }
}
