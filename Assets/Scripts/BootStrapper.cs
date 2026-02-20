using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    void Awake()
    {
        // GameSession yoksa oluþtur
        if (GameSession.I == null)
        {
            var go = new GameObject("GameSession");
            var session = go.AddComponent<GameSession>();
            // DontDestroyOnLoad, GameSession.Awake içinde zaten var ama burada da olur
            DontDestroyOnLoad(go);

            // Menüden gelinmediyse rastgele seed ata
            session.SetRandomSeed();              // <<< artýk property’e yazmýyoruz
        }

        // Menülerden kalma pause vb. temizle
        Time.timeScale = 1f;
    }
}
