using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Refs")]
    public GameObject enemyPrefab;
    public Transform player;
    public EnemyManager enemyManager;

    [Header("Spawn Timing")]
    public float spawnInterval = 3f;

    [Header("Screen/World")]
    public int chunkSize = 20;
    public float offscreenOffset = 2f;

    [Header("Collision Check")]
    public LayerMask spawnBlockMask;
    public float checkRadius = 0.5f;

    [Header("Pooling")]
    public int prewarmCount = 10;
    public Transform poolContainer;

    [Header("Determinism")]
    public int baseSeed = 1234567890;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private int globalSpawnIndex = 0;

    void Awake()
    {
        if (GameSession.I != null)
            baseSeed = GameSession.I.WorldSeed + 777;
    }

    void Start()
    {
        if (!enemyManager)
            enemyManager = FindObjectOfType<EnemyManager>();

        PrewarmPool();
        StartCoroutine(SpawnRoutine());
    }

    void PrewarmPool()
    {
        if (!enemyPrefab) return;

        for (int i = 0; i < prewarmCount; i++)
        {
            var go = Instantiate(enemyPrefab, Vector3.one * 99999f, Quaternion.identity, poolContainer);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            TrySpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void TrySpawnEnemy()
    {
        Camera cam = Camera.main;
        if (!cam || !enemyPrefab) return;

        Vector3 camPos = cam.transform.position;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        const int maxAttempts = 20;

        var prev = Random.state;
        Random.InitState(Mix(baseSeed, globalSpawnIndex));

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 spawnPos = GetRandomPositionOutsideCamera(camPos, camWidth, camHeight);

            if (!Physics2D.OverlapCircle(spawnPos, checkRadius, spawnBlockMask))
            {
                GameObject enemyGO = GetFromPool();
                if (!enemyGO) break;

                enemyGO.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
                enemyGO.SetActive(true);
                globalSpawnIndex++;
                Random.state = prev;
                return;
            }
        }

        Random.state = prev;
    }

    GameObject GetFromPool()
    {
        if (pool.Count > 0)
        {
            var go = pool.Dequeue();
            if (poolContainer && go.transform.parent != poolContainer)
                go.transform.SetParent(poolContainer, false);
            return go;
        }

        var created = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity, poolContainer);
        created.SetActive(false);
        return created;
    }

    public void ReturnToPool(GameObject go)
    {
        if (!go) return;
        go.SetActive(false);
        if (poolContainer) go.transform.SetParent(poolContainer, false);
        pool.Enqueue(go);
    }

    Vector2 GetRandomPositionOutsideCamera(Vector3 camPos, float camWidth, float camHeight)
    {
        float x = 0f, y = 0f;
        int side = Random.Range(0, 8);

        switch (side)
        {
            case 0:
                x = Random.Range(camPos.x - camWidth / 2f - offscreenOffset - chunkSize, camPos.x - camWidth / 2f - offscreenOffset);
                y = Random.Range(camPos.y - camHeight / 2f, camPos.y + camHeight / 2f);
                break;
            case 1:
                x = Random.Range(camPos.x + camWidth / 2f + offscreenOffset, camPos.x + camWidth / 2f + offscreenOffset + chunkSize);
                y = Random.Range(camPos.y - camHeight / 2f, camPos.y + camHeight / 2f);
                break;
            case 2:
                x = Random.Range(camPos.x - camWidth / 2f, camPos.x + camWidth / 2f);
                y = Random.Range(camPos.y - camHeight / 2f - offscreenOffset - chunkSize, camPos.y - camHeight / 2f - offscreenOffset);
                break;
            case 3:
                x = Random.Range(camPos.x - camWidth / 2f, camPos.x + camWidth / 2f);
                y = Random.Range(camPos.y + camHeight / 2f + offscreenOffset, camPos.y + camHeight / 2f + offscreenOffset + chunkSize);
                break;
            case 4:
                x = Random.Range(camPos.x - camWidth / 2f - offscreenOffset - chunkSize, camPos.x - camWidth / 2f - offscreenOffset);
                y = Random.Range(camPos.y - camHeight / 2f - offscreenOffset - chunkSize, camPos.y - camHeight / 2f - offscreenOffset);
                break;
            case 5:
                x = Random.Range(camPos.x - camWidth / 2f - offscreenOffset - chunkSize, camPos.x - camWidth / 2f - offscreenOffset);
                y = Random.Range(camPos.y + camHeight / 2f + offscreenOffset, camPos.y + camHeight / 2f + offscreenOffset + chunkSize);
                break;
            case 6:
                x = Random.Range(camPos.x + camWidth / 2f + offscreenOffset, camPos.x + camWidth / 2f + offscreenOffset + chunkSize);
                y = Random.Range(camPos.y - camHeight / 2f - offscreenOffset - chunkSize, camPos.y - camHeight / 2f - offscreenOffset);
                break;
            case 7:
                x = Random.Range(camPos.x + camWidth / 2f + offscreenOffset, camPos.x + camWidth / 2f + offscreenOffset + chunkSize);
                y = Random.Range(camPos.y + camHeight / 2f + offscreenOffset, camPos.y + camHeight / 2f + offscreenOffset + chunkSize);
                break;
        }

        return new Vector2(x, y);
    }

    int Mix(int a, int b)
    {
        unchecked { int h = a; h = (h * 16777619) ^ b; return h == 0 ? 1 : h; }
    }

    // EnemySpawner.cs
    public void ScaleSpawnRate(int minute)
    {
        if (minute <= 3)
            spawnInterval = 4f;
        else if (minute <= 8)
            spawnInterval = Mathf.Max(2.5f, spawnInterval - 0.3f);
        else if (minute <= 12)
            spawnInterval = Mathf.Max(1.0f, spawnInterval - 0.2f);
        else if (minute <= 15)
            spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.1f);

        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }
}