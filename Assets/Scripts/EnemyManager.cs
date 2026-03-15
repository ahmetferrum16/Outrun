using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private readonly HashSet<EnemyAI> allEnemies = new HashSet<EnemyAI>();

    // EnemyManager.cs
    public int EnemyCount => allEnemies.Count;

    void Start()
    {
        var enemiesInScene = FindObjectsOfType<EnemyAI>();
        foreach (var enemy in enemiesInScene)
            RegisterEnemy(enemy);

        PruneNulls();
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (enemy == null) return;
        allEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyAI enemy)
    {
        if (enemy == null) return;
        allEnemies.Remove(enemy);
    }

    public void ScaleEnemyStats(int minute)
    {
        PruneNulls();
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
                enemy.ScaleStats(minute);
        }
    }

    private void PruneNulls()
    {
        if (allEnemies.Count == 0) return;

        List<EnemyAI> toRemove = null;
        foreach (var e in allEnemies)
        {
            if (e == null)
                (toRemove ??= new List<EnemyAI>()).Add(e);
        }

        if (toRemove != null)
        {
            foreach (var e in toRemove)
                allEnemies.Remove(e);
        }
    }
}