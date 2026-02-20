// GroundTint.cs
using UnityEngine;

[DisallowMultipleComponent]
public class GroundTint : MonoBehaviour
{
    [Header("Colors")]
    public Color dayColor = new Color(1f, 1f, 1f, 1f);
    public Color nightColor = new Color(0.05f, 0.10f, 0.30f, 0.50f); // lacivert + yarý saydam

    SpriteRenderer sr;
    MeshRenderer mr;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mr = GetComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        DayNightCycle.OnDayNightChanged += Apply;
        // sahne açýldýðýnda mevcut faz neyse ona geç
        var dnc = FindObjectOfType<DayNightCycle>();
        Apply(dnc ? dnc.IsNight : false);
    }

    void OnDisable()
    {
        DayNightCycle.OnDayNightChanged -= Apply;
    }

    void Apply(bool isNight)
    {
        var col = isNight ? nightColor : dayColor;

        if (sr)
        {
            sr.color = col;
        }
        else if (mr)
        {
            // Sprites/Default "_Color" kullanýr; MPB ile instancing alloc'tan kaçýnýrýz
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", col);
            mr.SetPropertyBlock(mpb);
        }
    }
}
