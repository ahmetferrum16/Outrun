using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ChestRangeRing : MonoBehaviour
{
    public int segments = 64;
    public float lineWidth = 0.03f;
    public Color color = new Color(0f, 0.6f, 1f, 0.6f);

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = segments;
        lr.startColor = lr.endColor = color;
        gameObject.SetActive(false); // baþta kapalý
    }

    public void Draw(Vector3 center, float radius)
    {
        float step = 2f * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float ang = i * step;
            Vector3 p = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius;
            lr.SetPosition(i, p);
        }
    }

    public void SetVisible(bool v) => gameObject.SetActive(v);
}
