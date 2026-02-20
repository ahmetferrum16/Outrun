using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnemyFOVMesh : MonoBehaviour
{
    public float viewDistance = 7f;
    public float viewAngle = 90f;
    public int segments = 20;
    public Transform origin;
    public EnemyAI enemyAI;
    public Transform player;

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    public Color patrolColor = new Color(1f, 1f, 0f, 0.3f);
    public Color chaseColor = new Color(1f, 0f, 0f, 0.3f);

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material.color = patrolColor;
        meshRenderer.enabled = true;
    }

    void LateUpdate()
    {
        if (meshRenderer && meshRenderer.enabled)
        {
            UpdateFOVColor();
            DrawFOV();
        }

        CheckPlayerInFOV();
    }

    void UpdateFOVColor()
    {
        if (enemyAI != null && meshRenderer != null)
            meshRenderer.material.color = enemyAI.IsChasing ? chaseColor : patrolColor;
    }

    void DrawFOV()
    {
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        Vector2 forward = enemyAI.CurrentLookDirection.normalized;
        float startAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - viewAngle / 2f;

        Vector2 worldOrigin = origin.position;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + i * (viewAngle / segments);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            Vector2 worldPoint;
            RaycastHit2D hit = Physics2D.Raycast(worldOrigin, dir, viewDistance, enemyAI.obstacleMask);
            worldPoint = hit.collider ? hit.point : worldOrigin + dir * viewDistance;

            vertices[i + 1] = transform.InverseTransformPoint(worldPoint);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void CheckPlayerInFOV()
    {
        if (!player) return;

        Vector2 dirToPlayer = player.position - origin.position;
        float distance = dirToPlayer.magnitude;

        if (distance < viewDistance)
        {
            Vector2 forward = enemyAI.CurrentLookDirection.normalized;
            float angle = Vector2.Angle(forward, dirToPlayer);

            if (angle < viewAngle / 2f)
            {
                RaycastHit2D hit = Physics2D.Raycast(origin.position, dirToPlayer.normalized, distance, enemyAI.obstacleMask);
                if (!hit.collider)
                {
                    enemyAI.SetPlayerVisible(true);
                    return;
                }
            }
        }

        enemyAI.SetPlayerVisible(false);
    }
}