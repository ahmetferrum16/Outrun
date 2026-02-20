using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession I { get; private set; }

    public int WorldSeed { get; private set; }  // setter private kalsýn

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // >>> EKLEDÝÐÝMÝZ KAMU YÖNTEM
    public void SetSeed(int seed)
    {
        WorldSeed = seed;
        Random.InitState(seed);
    }

    // Ýstersen convenience:
    public void SetRandomSeed()
    {
        int seed = (int)System.DateTime.Now.Ticks & 0x7fffffff;
        SetSeed(seed);
    }
}
