using UnityEngine;

[DisallowMultipleComponent]
public class NightTint : MonoBehaviour
{
    [Header("Colors")]
    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.15f, 0.25f, 0.55f, 1f); // lacivertimsi

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
            sr.color = col;
        else if (mr)
        {
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", col);
            mr.SetPropertyBlock(mpb);
        }
    }
}
