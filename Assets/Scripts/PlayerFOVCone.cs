using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlayerFOVCone : MonoBehaviour
{
    [Header("FOV")]
    [Range(1, 360)] public float viewAngle = 90f;
    [Min(0.1f)] public float viewDistance = 12f;
    [Range(8, 256)] public int segments = 90;

    [Header("Refs")]
    public Transform origin;
    public LayerMask obstacleMask;
    public DayNightCycle dayNight;

    [Header("Smoothing")]
    [Tooltip("Yön değişimi ne kadar hızlı takip etsin (daha büyük = daha hızlı).")]
    public float turnSmoothing = 10f;
    [Tooltip("Hareket girişi bu değerin altındaysa eski yön korunur.")]
    public float inputDeadZone = 0.05f;


    [Tooltip("Ray, engeli vurduğunda deliği yüzeyin içine şu kadar taşır.")]
    public float surfaceEpsilon = 0.5f;   // 1–5 cm gibi düşün


    Mesh mesh;
    MeshRenderer mr;

    // Yumuşatılan ileri yön vektörü
    Vector2 smoothedForward = Vector2.right;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mr = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        if (!origin)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) origin = p.transform;
        }
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
        gameObject.SetActive(isNight);
    }

    void LateUpdate()
    {
        if (!origin) return;

        // 1) Hedef yön: klavye girdisi (istersen Rigidbody2D.velocity de kullanabilirsin)
        Vector2 targetDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (targetDir.sqrMagnitude > inputDeadZone * inputDeadZone)
        {
            targetDir.Normalize();
        }
        else
        {
            // girdi yoksa mevcut yönü koru
            targetDir = smoothedForward;
        }

        // 2) Yumuşatma: mevcut ileri yönü hedefe doğru akıt
        smoothedForward = Vector2.Lerp(smoothedForward, targetDir, Time.deltaTime * turnSmoothing);
        if (smoothedForward.sqrMagnitude > 0.0001f) smoothedForward.Normalize();

        // 3) Koniyi çiz
        DrawCone(origin.position, smoothedForward);
    }

    void DrawCone(Vector2 center, Vector2 forward)
    {
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float startAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - viewAngle * 0.5f;
        float step = viewAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float ang = startAngle + step * i;
            float rad = ang * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            RaycastHit2D hit = Physics2D.Raycast(center, dir, viewDistance, obstacleMask);

            // 🔧 ESKİ: end = hit.collider ? hit.point : (center + dir * viewDistance);
            // 🔧 YENİ: yüzeyin içine küçük bir pay bırak (stencil deliği engeli “kapsasın”)
            Vector2 end = hit.collider
                ? hit.point + dir * surfaceEpsilon
                : (center + dir * viewDistance);

            vertices[i + 1] = transform.InverseTransformPoint(end);
        }

        for (int i = 0; i < segments; i++)
        {
            int t = i * 3;
            triangles[t] = 0;
            triangles[t + 1] = i + 1;
            triangles[t + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        transform.position = center;
    }

}
