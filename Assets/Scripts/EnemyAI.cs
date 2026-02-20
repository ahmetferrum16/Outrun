using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // ========= Public Config =========
    public enum State { Patrolling, Stunned, Chasing }

    [Header("State")]
    public State currentState = State.Patrolling;

    [Header("Perception (FOV)")]
    [Min(0f)] public float viewDistance = 7f;
    [Range(0f, 360f)] public float viewAngle = 90f;
    public LayerMask obstacleMask;
    public LayerMask pathfindingMask; // Obstacle + EnemyObstacle

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 2f;
    [Min(0f)] public float rotationSpeed = 10f;
    [Min(0f)] public float patrolRange = 5f;

    [Header("Chase Behaviour")]
    [Min(0f)] public float chaseMemoryDuration = 4f;
    public GameObject questionMark;

    [Header("Stun")]
    [Min(0f)] public float stunDuration = 0.25f;

    [Header("Pathfinding")]
    [Min(0.1f)] public float gridCellSize = 0.5f;
    [Min(1)] public int gridRadius = 20;
    [Min(0.1f)] public float waypointReachDistance = 0.4f;
    [Min(0.1f)] public float pathRecalculateDistance = 2f;

    [Header("Debug")]
    [SerializeField] private Vector2 debugPatrolTarget;

    // ========= Runtime / Private =========
    private Rigidbody2D rb;
    private EnemyManager manager;
    private Transform player;

    private bool canSeePlayer = false;
    private Vector2 currentLookDirection = Vector2.right;
    private Vector2 patrolTarget;
    private Vector2 lastKnownPlayerPos;

    private float stunTimer = 0f;
    private float lastSeenTime = -999f;

    // Pathfinding
    private List<Vector2> currentPath = new List<Vector2>();
    private int waypointIndex = 0;
    private Vector2 lastPathTarget = Vector2.positiveInfinity;
    private bool isCalculatingPath = false;

    // #region PATCH — chase prediction
    private Vector2 lastKnownPlayerVelocity = Vector2.right;
    // #endregion

    private bool isSearching = false;
    private float searchTimer = 0f;
    private float searchDuration = 0.6f;
    private float searchStartAngle = 0f;

    // ========= Public API =========
    public Vector2 CurrentLookDirection => currentLookDirection;
    public bool IsChasing => currentState == State.Chasing;

    // ========= Unity Lifecycle =========
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void OnEnable()
    {
        SetRandomPatrolTarget();
        if (!manager) manager = FindObjectOfType<EnemyManager>();
        manager?.RegisterEnemy(this);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        manager?.UnregisterEnemy(this);
    }

    void Update()
    {
        UpdateState();
        UpdateLookDirection();

        if (!isCalculatingPath && currentState == State.Patrolling)
        {
            Vector2 target = GetCurrentTarget();
            if (Vector2.Distance(target, lastPathTarget) > pathRecalculateDistance)
                StartCoroutine(FindPathAsync(rb.position, target));
        }

        // Chase — player kaybolunca BFS ile lastKnownPlayerPos'a git
        if (!isCalculatingPath && currentState == State.Chasing)
        {
            Vector2 chaseGoal = canSeePlayer ? (Vector2)player.position : lastKnownPlayerPos;
            if (Vector2.Distance(chaseGoal, lastPathTarget) > pathRecalculateDistance)
                StartCoroutine(FindPathAsync(rb.position, chaseGoal));
        }
    }

    void FixedUpdate()
    {
        Move();
    }

    // ========= State Machine =========
    void UpdateState()
    {
        switch (currentState)
        {
            case State.Patrolling:
                if (canSeePlayer)
                {
                    stunTimer = stunDuration;
                    currentState = State.Stunned;
                    if (questionMark) questionMark.SetActive(true);
                }
                break;

            case State.Stunned:
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                {
                    currentState = State.Chasing;
                    if (questionMark) questionMark.SetActive(false);
                    lastKnownPlayerPos = player.position;
                    lastSeenTime = Time.time;
                    ClearPath();
                }
                break;

            case State.Chasing:
                if (canSeePlayer)
                {
                    Vector2 currentPlayerPos = player.position;
                    Vector2 delta = currentPlayerPos - lastKnownPlayerPos;
                    if (delta.sqrMagnitude > 0.0001f)
                        lastKnownPlayerVelocity = delta.normalized;
                    lastSeenTime = Time.time;
                    lastKnownPlayerPos = currentPlayerPos;
                    isSearching = false;
                }
                else if (!isSearching && Vector2.Distance(rb.position, lastKnownPlayerPos) < waypointReachDistance * 2f)
                {
                    // lastKnownPlayerPos'a ulaştı — aramaya başla
                    isSearching = true;
                    searchTimer = 0f;
                    searchStartAngle = Mathf.Atan2(currentLookDirection.y, currentLookDirection.x) * Mathf.Rad2Deg;
                    ClearPath();
                }
                else if (isSearching)
                {
                    searchTimer += Time.deltaTime;
                    float t = searchTimer / searchDuration;

                    // 0→0.5: sağa 90°, 0.5→1: sola 90° (başlangıç açısına göre)
                    float angle;
                    if (t < 0.5f)
                        angle = searchStartAngle + Mathf.Lerp(0f, 90f, t / 0.5f);
                    else
                        angle = searchStartAngle + Mathf.Lerp(90f, -90f, (t - 0.5f) / 0.5f);

                    float rad = angle * Mathf.Deg2Rad;
                    currentLookDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                    if (searchTimer >= searchDuration)
                    {
                        isSearching = false;
                        currentState = State.Patrolling;
                        SetRandomPatrolTarget();
                        if (questionMark) questionMark.SetActive(false);
                    }
                }
                else if (Time.time - lastSeenTime > chaseMemoryDuration)
                {
                    currentState = State.Patrolling;
                    SetRandomPatrolTarget();
                    ClearPath();
                    if (questionMark) questionMark.SetActive(false);
                }
                break;
        }
    }

    // ========= Movement =========
    void Move()
    {
        if (currentState == State.Stunned) return;

        Vector2 pos = rb.position;
        Vector2 target = GetCurrentTarget();

        if (currentState == State.Patrolling)
        {
            if (Vector2.Distance(pos, target) < waypointReachDistance)
            {
                SetRandomPatrolTarget();
                ClearPath();
                return;
            }

            if (currentPath.Count > 0 && waypointIndex < currentPath.Count)
            {
                Vector2 waypoint = currentPath[waypointIndex];
                if (Vector2.Distance(pos, waypoint) < waypointReachDistance)
                {
                    waypointIndex++;
                    return;
                }
                rb.MovePosition(pos + (waypoint - pos).normalized * moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                rb.MovePosition(pos + (target - pos).normalized * moveSpeed * Time.fixedDeltaTime);
            }
        }
        // SONRA
        else if (currentState == State.Chasing)
        {
            if (isSearching) return; // arama sırasında yerinde dur

            if (currentPath.Count > 0 && waypointIndex < currentPath.Count)
            {
                Vector2 waypoint = currentPath[waypointIndex];
                if (Vector2.Distance(pos, waypoint) < waypointReachDistance)
                {
                    waypointIndex++;
                    return;
                }
                rb.MovePosition(pos + (waypoint - pos).normalized * moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                Vector2 chaseGoal = canSeePlayer ? (Vector2)player.position : lastKnownPlayerPos;
                Vector2 dir = (chaseGoal - pos).normalized;
                if (dir.sqrMagnitude > 0.001f)
                    rb.MovePosition(pos + dir * moveSpeed * Time.fixedDeltaTime);
            }
        }
    }

    // SONRA
    Vector2 GetCurrentTarget()
    {
        if (currentState == State.Chasing)
        {
            if (canSeePlayer) return player.position;
            if (currentPath.Count > 0 && waypointIndex < currentPath.Count)
                return currentPath[currentPath.Count - 1]; // path'in son noktası
            return lastKnownPlayerPos;
        }
        return patrolTarget;
    }

    void ClearPath()
    {
        StopAllCoroutines();
        isCalculatingPath = false;
        currentPath.Clear();
        waypointIndex = 0;
        lastPathTarget = Vector2.positiveInfinity;
    }

    // ========= Pathfinding (BFS — Coroutine) =========
    IEnumerator FindPathAsync(Vector2 start, Vector2 goal)
    {
        isCalculatingPath = true;
        lastPathTarget = goal;

        Vector2Int startCell = WorldToGrid(start);
        Vector2Int goalCell = WorldToGrid(goal);

        // Başlangıç veya hedef blocked ise en yakın boş hücreye taşı
        if (IsBlocked(startCell)) startCell = FindNearestFreeCell(startCell);
        if (IsBlocked(goalCell)) goalCell = FindNearestFreeCell(goalCell);

        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        queue.Enqueue(startCell);
        visited.Add(startCell);

        bool found = false;
        int iterations = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == goalCell) { found = true; break; }

            if (Mathf.Abs(current.x - startCell.x) > gridRadius ||
                Mathf.Abs(current.y - startCell.y) > gridRadius)
                continue;

            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (visited.Contains(neighbor)) continue;
                if (IsBlocked(neighbor)) continue;

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }

            iterations++;
            if (iterations % 50 == 0)
                yield return null;
        }

        if (found)
        {
            var path = new List<Vector2>();
            Vector2Int step = goalCell;
            int safety = 0;
            while (step != startCell && safety < 1000)
            {
                path.Add(GridToWorld(step));
                step = cameFrom[step];
                safety++;
            }
            path.Reverse();

            // Waypoint'leri duvardan uzaklaştır
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 nudged = NudgeAwayFromWalls(path[i]);
                path[i] = nudged;
            }

            currentPath = path;
            waypointIndex = 0;
        }
        // Path bulunamazsa hareket etme — direkt hedefe yürüme kaldırıldı
        else
        {
            currentPath.Clear();
            waypointIndex = 0;
            // Path bulunamadı — yeni hedef seç
            if (currentState == State.Patrolling)
                SetRandomPatrolTarget();
            else if (currentState == State.Chasing)
                lastSeenTime = -999f; // hafızayı sıfırla, patrol'a dönsün
        }

        isCalculatingPath = false;
    }

    Vector2 NudgeAwayFromWalls(Vector2 point)
    {
        Vector2 push = Vector2.zero;
        float checkDist = gridCellSize;

        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 dir in dirs)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, dir, checkDist, obstacleMask);
            if (hit.collider)
                push -= dir * (checkDist - hit.distance);
        }

        return point + push * 0.5f;
    }
    Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / gridCellSize),
            Mathf.RoundToInt(worldPos.y / gridCellSize)
        );
    }

    Vector2 GridToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x * gridCellSize, cell.y * gridCellSize);
    }

    bool IsBlocked(Vector2Int cell)
    {
        Vector2 worldPos = GridToWorld(cell);
        return Physics2D.OverlapBox(worldPos, Vector2.one * gridCellSize * 0.7f, 0f, pathfindingMask);
    }

    List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(cell.x + 1, cell.y),
            new Vector2Int(cell.x - 1, cell.y),
            new Vector2Int(cell.x, cell.y + 1),
            new Vector2Int(cell.x, cell.y - 1),
            new Vector2Int(cell.x + 1, cell.y + 1),
            new Vector2Int(cell.x + 1, cell.y - 1),
            new Vector2Int(cell.x - 1, cell.y + 1),
            new Vector2Int(cell.x - 1, cell.y - 1),
        };
    }

    // ========= Look Direction =========
    void UpdateLookDirection()
    {
        Vector2 targetPos = GetCurrentTarget();
        if (currentState == State.Patrolling && currentPath.Count > 0 && waypointIndex < currentPath.Count)
            targetPos = currentPath[waypointIndex];

        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude > 0.01f)
            currentLookDirection = Vector2.Lerp(currentLookDirection, dir, rotationSpeed * Time.deltaTime);
        // dir sıfırsa currentLookDirection değişmez — son geçerli yön korunur
    }

    // ========= Patrol =========
    void SetRandomPatrolTarget()
    {
        Vector2 origin = transform.position;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        if (randomDir.sqrMagnitude < 0.0001f) randomDir = Vector2.right;
        float randomDist = Random.Range(patrolRange * 0.3f, patrolRange);
        patrolTarget = origin + randomDir * randomDist;

        // Hedef blocked ise en yakın boş hücreye taşı
        Vector2Int targetCell = WorldToGrid(patrolTarget);
        if (IsBlocked(targetCell))
            patrolTarget = GridToWorld(FindNearestFreeCell(targetCell));

        debugPatrolTarget = patrolTarget;
    }

    // ========= Public API =========
    public void SetPlayerVisible(bool visible)
    {
        canSeePlayer = visible;
    }

    public void ScaleStats(int minute)
    {
        moveSpeed += 0.1f;
        viewAngle = Mathf.Min(viewAngle + 2f, 170f);
        viewDistance += 0.3f;
        chaseMemoryDuration = Mathf.Min(chaseMemoryDuration + 0.25f, 10f);
    }

    // ========= Damage / Triggers =========
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null && !pm.IsInvulnerable())
        {
            pm.stats.TakeDamage();
            pm.StartInvulnerability(1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, patrolTarget);
        Gizmos.DrawSphere(patrolTarget, 0.15f);

        if (currentPath == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < currentPath.Count - 1; i++)
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
    }

    Vector2Int FindNearestFreeCell(Vector2Int cell)
    {
        for (int r = 1; r <= 5; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    if (Mathf.Abs(x) != r && Mathf.Abs(y) != r) continue;
                    Vector2Int candidate = new Vector2Int(cell.x + x, cell.y + y);
                    if (!IsBlocked(candidate)) return candidate;
                }
            }
        }
        return cell;
    }
}