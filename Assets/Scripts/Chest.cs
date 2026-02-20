// Assets/Scripts/Items/Chest.cs
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("Open Settings")]
    [Min(0f)] public float openRadius = 1.6f;   // Oyuncu bu yarýçapta durmalý
    [Min(0.1f)] public float openTime = 8f;     // Kaç saniyede açýlýr?
    [Min(0f)] public float drainSpeed = 2f;     // Dýþarýdayken saniyede ne kadar geri azalsýn?

    [Header("Loot")]
    public List<SingleUseItem> lootTable = new List<SingleUseItem>(); // eþit þans

    [Header("Spawn")]
    public Transform spawnPoint;                  // Boþsa chest pozisyonu kullanýlýr

    [Header("UI")]
    public ChestProgressUI progressUI;            // Radial bar (child world-space canvas)
    public ChestRangeRing rangeRing;             // Yakýnlýk halkasý (LineRenderer)

    // Runtime
    private Transform player;
    private float timer = 0f;

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!spawnPoint) spawnPoint = this.transform; // fallback
    }

    void Update()
    {
        if (!player) return;

        // 1) Mesafe ve durum
        float dist = Vector2.Distance(player.position, transform.position);
        bool inside = dist <= openRadius;           // açýlma yarýçapýnýn içinde miyiz?
        bool near = dist <= openRadius * 1.5f;    // halkayý göstermek için ipucu eþiði

        // 2) Zaman ilerleme (içerideyse doldur, deðilse YAVAÞÇA boþalt)
        if (inside)
        {
            timer = Mathf.Min(openTime, timer + Time.deltaTime);
        }
        else
        {
            timer = Mathf.Max(0f, timer - Mathf.Max(0f, drainSpeed) * Time.deltaTime);
        }

        // 3) Açýlma ilerlemesi UI (radial)
        if (progressUI)
        {
            float progress = (openTime > 0f) ? (timer / openTime) : 0f;
            bool show = inside || near || progress > 0f;   // yakýnken ipucu olarak göster
            progressUI.SetProgress(progress, show);
        }

        // 4) Yakýnlýk halkasý (range ring)
        if (rangeRing)
        {
            rangeRing.Draw(transform.position, openRadius);
            rangeRing.SetVisible(near);

            var lr = rangeRing.GetComponent<LineRenderer>();
            if (lr)
            {
                Color c = inside ? new Color(0f, 0.8f, 1f, 0.9f) : new Color(0f, 0.6f, 1f, 0.6f);
                lr.startColor = c;
                lr.endColor = c;
            }
        }

        // 5) Açýldý mý?
        if (timer >= openTime)
        {
            Open();
        }
    }

    void Open()
    {
        // Tekrarlý tetiklenmeyi engelle
        enabled = false;
        if (progressUI) progressUI.SetProgress(1f, false);

        // 1) Rastgele loot seç
        SingleUseItem loot = null;
        if (lootTable != null && lootTable.Count > 0)
            loot = lootTable[Random.Range(0, lootTable.Count)];

        if (loot == null)
        {
            Debug.LogWarning("[Chest] Loot yok.");
            Destroy(gameObject);
            return;
        }

        // 2) Item’ýn kendi pickup prefab’ýný kullan
        if (loot.pickupPrefab == null)
        {
            Debug.LogError($"[Chest] '{loot.name}' için pickupPrefab atanmadý!");
        }
        else
        {
            Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
            var go = Instantiate(loot.pickupPrefab, pos, Quaternion.identity);

            // Prefab'ta item boþsa doldur
            var pi = go.GetComponent<PickupItem>();
            if (pi != null && pi.item == null)
                pi.SetItem(loot);
        }

        // 3) Sandýðý kaldýr
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, openRadius);
    }
}
