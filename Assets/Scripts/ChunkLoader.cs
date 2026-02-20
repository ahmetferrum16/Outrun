using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ChunkLoader : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject groundPrefab;

    [Header("Chunk Settings")]
    [Min(1)] public int chunkSize = 16;
    [Min(0)] public int loadRadius = 2;        // Yüklenecek kare yarıçapı
    [Min(0)] public int unloadBuffer = 0;      // Sökme tamponu (0 genelde yeter)

    [Header("World Generation (Deterministic)")]
    public int worldSeed = 12345;              // Awake'te GameSession'dan çekilir

    [Header("Spawn Settings")]
    public GameObject[] spawnPrefabs;          // Engeller (Wall/Tree/Stone vs.)
    [Min(0)] public int objectsPerChunk = 10;
    [Min(0f)] public float spawnCheckRadius = 1f;
    [Tooltip("Yalnızca bu katmanlardaki collider'lar engel sayılır (Wall/Tree vs).")]
    public LayerMask obstacleMask;

    [Header("Obstacle Scaling")]
    public Vector2 obstacleScaleRange = new Vector2(0.8f, 2f);

    [Header("Pooling")]
    public bool usePooling = true;
    [Min(0)] public int prewarmPerPrefab = 12; // Her prefab için başlangıç adedi
    public Transform chunksRoot;               // Aktif chunk parent'ları burada
    public Transform poolRoot;                 // Havuzdaki objeler burada

    // ---- Runtime ----
    private readonly Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<GameObject, Queue<GameObject>> prefabPools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Queue<GameObject> chunkParentPool = new Queue<GameObject>();

    [Header("Chest Spawning")]
    public GameObject chestPrefab;               // Chest prefab (üzerinde Chest.cs olmalı)
    [Min(1)] public int chestEveryMinChunks = 10;
    [Min(1)] public int chestEveryMaxChunks = 20;
    [Min(0f)] public float chestMinDistanceFromPlayer = 6f;
    [Min(0.1f)] public float chestCheckRadius = 0.6f;
    public LayerMask chestBlockMask;             // Player/Enemy/Obstacle vb.

    private int chunksUntilNextChest = 0;
    void Awake()
    {
        // Seed’i GameSession’dan çek
        if (GameSession.I != null)
            worldSeed = GameSession.I.WorldSeed;

        // Kökleri yoksa oluştur
        if (!chunksRoot)
        {
            var go = new GameObject("Chunks_Root");
            chunksRoot = go.transform;
        }
        if (!poolRoot)
        {
            var go = new GameObject("Pool_Root");
            poolRoot = go.transform;
        }

        // Pool'ları hazırla
        if (usePooling)
        {
            PrewarmPools();
        }

        // İlk hedefi belirle (10–20 arası)
        if (chestEveryMaxChunks < chestEveryMinChunks)
            chestEveryMaxChunks = chestEveryMinChunks;

        chunksUntilNextChest = Random.Range(chestEveryMinChunks, chestEveryMaxChunks + 1);

    }



    void Update()
    {
        if (!player) return;

        Vector2Int currentChunk = GetChunkCoord(player.position);

        // 1) Gerekli chunk seti
        HashSet<Vector2Int> required = new HashSet<Vector2Int>();
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int y = -loadRadius; y <= loadRadius; y++)
            {
                required.Add(currentChunk + new Vector2Int(x, y));
            }
        }

        // 2) Eksikleri yükle
        foreach (var coord in required)
        {
            if (!activeChunks.ContainsKey(coord))
                LoadChunk(coord);
        }

        // 3) Fazlaları (görüş dışı) unload et → havuza iade
        List<Vector2Int> toUnload = null;
        foreach (var kvp in activeChunks)
        {
            if (!IsWithinRadius(kvp.Key, currentChunk, loadRadius + unloadBuffer))
            {
                (toUnload ??= new List<Vector2Int>()).Add(kvp.Key);
            }
        }
        if (toUnload != null)
        {
            foreach (var coord in toUnload)
                UnloadChunk(coord);
        }
    }

    // --- Generation helpers ---

    Vector2Int GetChunkCoord(Vector2 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = Mathf.FloorToInt(position.y / chunkSize);
        return new Vector2Int(x, y);
    }

    bool IsWithinRadius(Vector2Int a, Vector2Int center, int radius)
    {
        // Chebyshev (kare alan) uygun: kare bir yükleme alanı
        int dx = Mathf.Abs(a.x - center.x);
        int dy = Mathf.Abs(a.y - center.y);
        return Mathf.Max(dx, dy) <= radius;
    }

    void LoadChunk(Vector2Int chunkCoord)
    {
        // Chunk parent'ını hazırla
        GameObject parent = GetChunkParent();
        parent.name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}";
        parent.transform.SetParent(chunksRoot, false);
        parent.transform.position = new Vector3(
            chunkCoord.x * chunkSize + chunkSize * 0.5f,
            chunkCoord.y * chunkSize + chunkSize * 0.5f,
            0f
        );
        parent.SetActive(true);
        activeChunks[chunkCoord] = parent;

        Vector2 chunkCenter = parent.transform.position;

        // Deterministic RNG (global RNG'yi kirletme)
        var prevState = Random.state;
        Random.InitState(ComputeChunkSeed(chunkCoord));

        // 🟩 Zemin
        if (groundPrefab)
        {
            var ground = GetFromPool(groundPrefab);
            ground.transform.SetParent(parent.transform, false);
            ground.transform.position = chunkCenter;
            ground.transform.localScale = new Vector3(chunkSize, chunkSize, 1f);
            ground.SetActive(true);
        }

        // 🔁 Engeller
        if (spawnPrefabs == null || spawnPrefabs.Length == 0)
        {
            Debug.LogWarning("ChunkLoader: 'spawnPrefabs' boş; engel spawn edilmeyecek.");
        }
        else
        {
            int spawned = 0;
            int maxAttempts = objectsPerChunk * 10;

            while (spawned < objectsPerChunk && maxAttempts-- > 0)
            {
                Vector2 randomOffset = new Vector2(
                    Random.Range(-chunkSize / 2f, chunkSize / 2f),
                    Random.Range(-chunkSize / 2f, chunkSize / 2f)
                );

                Vector2 spawnPos = chunkCenter + randomOffset;

                // Oyuncuya çok yakınsa geç
                if (player && Vector2.Distance(spawnPos, player.position) < 5f)
                    continue;

                if (IsPositionFree(spawnPos, spawnCheckRadius))
                {
                    GameObject prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
                    GameObject instance = GetFromPool(prefab);

                    instance.transform.SetParent(parent.transform, false);
                    instance.transform.position = spawnPos;

                    // Basit tip bazlı ölçeklendirme
                    if (prefab.name.Contains("Wall"))
                    {
                        float wallLength = Random.Range(3f, 12f);
                        bool isVertical = Random.value < 0.5f;
                        instance.transform.localScale = isVertical
                            ? new Vector3(0.3f, wallLength, 1f)
                            : new Vector3(wallLength, 0.3f, 1f);
                    }
                    else
                    {
                        float s = Random.Range(obstacleScaleRange.x, obstacleScaleRange.y);
                        instance.transform.localScale = new Vector3(s, s, 1f);
                    }

                    instance.SetActive(true);
                    spawned++;
                }
            }
        }

        // 🌟 Seyrek Chest spawn (10–20 chunk'ta bir)
        TrySpawnChestInChunk(chunkCoord, chunkCenter, parent.transform);

        // RNG state geri yükle
        Random.state = prevState;
    }

    void UnloadChunk(Vector2Int chunkCoord)
    {
        if (!activeChunks.TryGetValue(chunkCoord, out var parent) || parent == null)
        {
            activeChunks.Remove(chunkCoord);
            return;
        }

        // Çocukları havuza iade et
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            var child = parent.transform.GetChild(i).gameObject;
            ReturnToPool(child);
        }

        // Parent'ı havuza iade et
        ReturnChunkParent(parent);
        activeChunks.Remove(chunkCoord);
    }

    int ComputeChunkSeed(Vector2Int coord)
    {
        unchecked
        {
            int hash = worldSeed;
            hash = (hash * 73856093) ^ coord.x;
            hash = (hash * 19349663) ^ coord.y;
            hash ^= (hash << 13);
            hash ^= (hash >> 17);
            hash ^= (hash << 5);
            return hash == 0 ? 1 : hash;
        }
    }

    private bool IsPositionFree(Vector2 position, float radius)
    {
        // Sadece obstacleMask'taki collider'lar engel sayılır
        Collider2D hit = Physics2D.OverlapCircle(position, radius, obstacleMask);
        return hit == null;
    }

    // -------- Pooling --------

    void PrewarmPools()
    {
        // Chunk parent prewarm (yaklaşık görünür kare sayısı kadar)
        int visibleSquares = (2 * (loadRadius + unloadBuffer) + 1);
        int targetParents = Mathf.Max(visibleSquares * visibleSquares, 4);
        for (int i = 0; i < targetParents; i++)
        {
            var parent = CreateChunkParent();
            parent.SetActive(false);
            parent.transform.SetParent(poolRoot, false);
            chunkParentPool.Enqueue(parent);
        }

        // Obstacle/ground pool prewarm
        if (groundPrefab)
            EnsurePoolFor(groundPrefab, prewarmPerPrefab);

        if (spawnPrefabs != null)
        {
            foreach (var pf in spawnPrefabs)
            {
                if (!pf) continue;
                EnsurePoolFor(pf, prewarmPerPrefab);
            }
        }
    }

    void EnsurePoolFor(GameObject prefab, int count)
    {
        if (!prefabPools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>(count);
            prefabPools[prefab] = q;
        }

        while (q.Count < count)
        {
            var go = Instantiate(prefab, poolRoot);
            go.SetActive(false);
            // Prefab’ın kendisine işaret bırak (hangi havuza döneceğini bilelim)
            AttachPoolMarker(go, prefab);
            q.Enqueue(go);
        }
    }

    GameObject GetChunkParent()
    {
        if (!usePooling)
            return CreateChunkParent();

        if (chunkParentPool.Count > 0)
        {
            var go = chunkParentPool.Dequeue();
            go.transform.SetParent(chunksRoot, false);
            return go;
        }
        return CreateChunkParent();
    }

    void ReturnChunkParent(GameObject parent)
    {
        if (!usePooling)
        {
            Destroy(parent);
            return;
        }

        parent.SetActive(false);
        parent.transform.SetParent(poolRoot, false);
        chunkParentPool.Enqueue(parent);
    }

    GameObject CreateChunkParent()
    {
        var go = new GameObject("ChunkParent");
        return go;
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (!usePooling)
            return Instantiate(prefab);

        if (!prefabPools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            prefabPools[prefab] = q;
        }

        if (q.Count > 0)
        {
            var go = q.Dequeue();
            // Parent/pos/scale dışarıda ayarlanacak
            return go;
        }
        else
        {
            var go = Instantiate(prefab, poolRoot);
            go.SetActive(false);
            AttachPoolMarker(go, prefab);
            return go;
        }
    }

    void ReturnToPool(GameObject go)
    {
        if (!usePooling)
        {
            Destroy(go);
            return;
        }

        var marker = go.GetComponent<_PoolMarker>();
        if (!marker || !marker.prefab)
        {
            // Havuz bilgisi yoksa yine de güvenli şekilde sakla
            go.SetActive(false);
            go.transform.SetParent(poolRoot, false);
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(poolRoot, false);

        if (!prefabPools.TryGetValue(marker.prefab, out var q))
        {
            q = new Queue<GameObject>();
            prefabPools[marker.prefab] = q;
        }
        q.Enqueue(go);
    }

    void AttachPoolMarker(GameObject go, GameObject prefab)
    {
        var m = go.GetComponent<_PoolMarker>();
        if (!m) m = go.AddComponent<_PoolMarker>();
        m.prefab = prefab;
    }

    // Havuz kimliği (hangi prefab'a döneceğini bilir)
    private class _PoolMarker : MonoBehaviour
    {
        public GameObject prefab;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (chunkSize < 1) chunkSize = 1;
        if (objectsPerChunk < 0) objectsPerChunk = 0;

        if (spawnPrefabs == null || spawnPrefabs.Length == 0)
            UnityEngine.Debug.LogWarning("ChunkLoader: 'spawnPrefabs' boş – engeller oluşturulmaz.");

        if (obstacleMask == 0)
            UnityEngine.Debug.LogWarning("ChunkLoader: 'obstacleMask' atanmadı – boş yer kontrolü hatalı olabilir.");
    }
#endif

    void TrySpawnChestInChunk(Vector2Int chunkCoord, Vector2 chunkCenter, Transform parent)
    {
        if (!chestPrefab) return;

        chunksUntilNextChest--;
        if (chunksUntilNextChest > 0) return;

        const int chestMaxAttempts = 40;
        if (FindFreePositionInChunk(chunkCenter, chestMinDistanceFromPlayer, chestCheckRadius, chestMaxAttempts, out Vector2 chestPos))
        {
            var chest = Instantiate(chestPrefab, chestPos, Quaternion.identity);
            chest.transform.SetParent(parent, true);

            int minN = Mathf.Max(1, chestEveryMinChunks);
            int maxN = Mathf.Max(minN, chestEveryMaxChunks);
            chunksUntilNextChest = Random.Range(minN, maxN + 1);
        }
        else
        {
            chunksUntilNextChest = 1;
        }
    }

    bool FindFreePositionInChunk(Vector2 chunkCenter, float minDistFromPlayer, float radius, int maxAttempts, out Vector2 result)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 offset = new Vector2(
                Random.Range(-chunkSize * 0.5f, chunkSize * 0.5f),
                Random.Range(-chunkSize * 0.5f, chunkSize * 0.5f)
            );

            Vector2 candidate = chunkCenter + offset;

            if (player && Vector2.Distance(candidate, player.position) < minDistFromPlayer)
                continue;

            bool blocked = Physics2D.OverlapCircle(candidate, radius, chestBlockMask);
            if (!blocked)
            {
                result = candidate;
                return true;
            }
        }

        result = chunkCenter;
        return false;
    }
}