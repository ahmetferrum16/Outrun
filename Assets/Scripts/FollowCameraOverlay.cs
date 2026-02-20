// Assets/Scripts/FollowCameraOverlay.cs
using UnityEngine;

[DisallowMultipleComponent]
public class FollowCameraOverlay : MonoBehaviour
{
    public Camera cam;
    public DayNightCycle dayNight;

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!dayNight) dayNight = FindObjectOfType<DayNightCycle>();
        DayNightCycle.OnDayNightChanged += OnPhaseChanged;
        OnPhaseChanged(dayNight ? dayNight.IsNight : false);
    }

    void OnDestroy()
    {
        DayNightCycle.OnDayNightChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(bool isNight)
    {
        gameObject.SetActive(isNight); // sadece gece overlay aktif
    }

    void LateUpdate()
    {
        if (!cam) return;
        // Ortho kamera boyutuna göre quad’ý ekraný kaplayacak þekilde ölçekle
        float h = 2f * cam.orthographicSize;
        float w = h * cam.aspect;
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z + 1f);
        transform.localScale = new Vector3(w, h, 1f);
    }
}
