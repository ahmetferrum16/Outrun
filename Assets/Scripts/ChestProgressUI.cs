using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class ChestProgressUI : MonoBehaviour
{
    [Header("UI")]
    public Image fillImage;   // Image Type = Filled, Fill Method = Radial 360

    [Header("Placement")]
    public bool faceCamera = true;
    public Vector3 offset = new Vector3(0f, 1f, 0f);
    public float worldSize = 1.0f;   // dünyada çap ~1 birim görünür (scale ile çarpılır)

    private Transform target; // Chest (parent)
    private Camera cam;
    private Canvas canvas;

    void Awake()
    {
        target = transform.parent ? transform.parent : null;
        cam = Camera.main;
        canvas = GetComponent<Canvas>();

        // 🔒 Canvas'ı Dünya Moduna zorla
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        // 🔒 Boyut/ölçekleri güvenli bir değere çek
        var rt = transform as RectTransform;
        rt.sizeDelta = new Vector2(100f, 100f);     // 100x100 px
        transform.localScale = Vector3.one * (worldSize / 100f);
        // worldSize=1 → 100px / 100 = 0.01 ölçek ⇒ dünyada ~1 birim çap

        if (fillImage != null)
        {
            // Dairenin taşmaması için rect'i tam kare tut
            var frt = fillImage.transform as RectTransform;
            frt.anchorMin = new Vector2(0.5f, 0.5f);
            frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(100f, 100f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.enabled = false;
            fillImage.fillAmount = 0f;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // Chest'in üstünde dursun
        transform.position = target.position + offset;

        // Kameraya dönsün (istersen kapat)
        if (faceCamera && cam)
            transform.rotation = cam.transform.rotation;
    }

    /// <summary>
    /// progress: 0..1, visible: göster/gizle (yakında/ içeride)
    /// </summary>
    public void SetProgress(float progress, bool visible)
    {
        if (!fillImage) return;

        fillImage.fillAmount = Mathf.Clamp01(progress);
        fillImage.enabled = visible || progress > 0f;
    }
}
