[2.md](https://github.com/user-attachments/files/25720705/2.md)

ChatBot Canvas

# Project API Registry & Build Rules (Reusable Mode Spec)

> **Purpose:** Single source of truth I will follow when generating patches. Paste this entire document into any new chat, and I'll adhere strictly to it.

---

## 🔁 Canvas Güncelleme Sistemi (Workflow Kuralları)

### Temel Kurallar
- **"," sembolü = onaylıyorum.** Kullanıcı "," yazıp enter'a basarsa bu onay anlamına gelir, tekrar sormaya gerek yok.
- **Kod yazarken:** Her onaylanan değişiklik zihinde takip edilir.
- **Reddedilen versiyonlar:** Yalnızca son onaylanan versiyon canvas'a yansıtılır. Ara versiyonlar yok sayılır.
- **"Canvas'ı gönder" komutu:** O ana kadarki tüm konuşma taranır, onaylanan değişiklikler derlenir ve bu belgenin güncel hali dosya olarak sunulur.
- **Kullanıcı bağımsız kod eklemişse:** Bunu belirtecek, Claude da canvas'ı buna göre güncelleyecek.
- **Makul aralıklarla canvas al:** Yaklaşık 80-100 mesajda bir canvas almak kayıpsız aktarımı garantiler.

### Yeni Chat Geçiş Prosedürü
1. Mevcut chatte "canvas'ı gönder" de
2. Claude güncel canvas dosyasını oluşturur
3. Yeni chat aç → canvas içeriğini yapıştır → devam et

---

## 🔹 Starter Block (yeni chate kopyala)

```
PROJECT MODE SPEC v1 — paste-in

Bu belgedeki kurallara sıkı sıkıya uy:
- Yalnızca API Registry'de listelenen public API'leri kullan.
- Yeni class/method üretme; Mode Spec'te OK-TO-ADD izni olmadan.
- Çıktıyı unified diff + exact placement talimatlarıyla ver.
- Frame başına GC alloc = 0.
- Eksik API varsa icat etme, raporla.
- Canvas Güncelleme Sistemi kurallarını uygula (belgenin üst bölümüne bak).
- "," sembolü = onaylıyorum.

Bugünkü odak:
- <buraya yaz>
```

---

## 1) Project Overview

- **Oyun türü:** Top-down 2D action
- **Engine:** Unity 6000.3.10f1, Built-in Render Pipeline (SRP = None)
- **Hedef platform:** Windows (PC)
- **Sahneler:** Scenes/MainMenu, Scenes/Game
- **Core loop:** dodge → survive → upgrades
- **Core sistemler:** PlayerMovement, EnemyAI, EnemyFOVMesh, EnemyManager, EnemySpawner, ChunkLoader, Buff System, GameManager, DayNightCycle, GameSession

---

## 2) API Registry (authoritative)

---

### Bootstrapper.cs (MonoBehaviour)
- **Awake():** GameSession.I yoksa yeni GameObject("GameSession") + AddComponent + DontDestroyOnLoad. SetRandomSeed(). Time.timeScale = 1.

---

### Buff.cs (ScriptableObject)
- `public enum BuffType { Speed, Stamina, Cooldown, SprintSpeed, StaminaRegen, DashDistance, CameraZoom }`
- `public string buffName`, `public BuffType type`, `public float amount`

---

### BuffButton.cs (MonoBehaviour)
- `public void Setup(Buff buff, GameManager manager)`
- `public void OnClick()` — gameManager.ApplyBuff(currentBuff)

---

### CameraFollow.cs (MonoBehaviour)
- `public Transform target`, `public float smoothSpeed = 0.125f`, `public Vector3 offset`
- **LateUpdate():** Vector3.Lerp; Z sabit

---

### Chest.cs (MonoBehaviour)
- `public float openRadius, openTime, drainSpeed`
- `public List<SingleUseItem> lootTable`
- `public Transform spawnPoint`
- `public ChestProgressUI progressUI`, `public ChestRangeRing rangeRing`
- **Runtime private:** Transform player, float timer
- **Open():** rastgele loot → PickupItem.SetItem(loot) → Destroy

---

### ChestProgressUI.cs (MonoBehaviour)
- `public void SetProgress(float progress01, bool visible)`

---

### ChestRangeRing.cs (MonoBehaviour)
- `public void Draw(Vector3 center, float radius)`, `public void SetVisible(bool v)`

---

### ChunkLoader.cs (MonoBehaviour)
- `public Transform player`, `public GameObject groundPrefab`
- `int chunkSize=16`, `int loadRadius=2`, `int unloadBuffer=0`
- `int worldSeed` (GameSession.I.WorldSeed ile override)
- `GameObject[] spawnPrefabs=[Tree,Wall]`, `int objectsPerChunk=10`, `float spawnCheckRadius=1`, `LayerMask obstacleMask`
- `Vector2 obstacleScaleRange=(0.8,2)`
- `bool usePooling=true`, `int prewarmPerPrefab=12`
- `GameObject chestPrefab`, `int chestEveryMinChunks=1`, `int chestEveryMaxChunks=3`, `float chestMinDistanceFromPlayer=6`, `float chestCheckRadius=0.6`, `LayerMask chestBlockMask=Enemy|Obstacle|Player`
- **NOT:** `TrySpawnChestInChunk` ve `FindFreePositionInChunk` `#if UNITY_EDITOR` bloğunun **dışında** olmalı — aksi halde build'de chest spawn olmaz.

---

### DayNightCycle.cs (MonoBehaviour)
- `public float cycleDuration=60f`
- `public GameObject playerFOVObject` (Inspector'da None)
- `public bool IsNight { get; }`, `public float PhaseProgress01 { get; }`
- `public static event DayNightChange OnDayNightChanged`
- `public void AddTime(float seconds)`

---

### DayNightUI.cs (MonoBehaviour)
- `public DayNightCycle dayNight`, `public TextMeshProUGUI label`, `public Image fillImage`
- `public string dayText="DAY"`, `nightText="NIGHT"`

---

### EnemyAI.cs (MonoBehaviour)
- `public enum State { Patrolling, Stunned, Chasing }`
- **Perception:** `public float viewDistance=7f`, `public float viewAngle=90f`, `public LayerMask obstacleMask`, `public LayerMask pathfindingMask`
- **Movement:** `public float moveSpeed=2f`, `rotationSpeed=10f`, `patrolRange=5f`
- **Chase:** `public float chaseMemoryDuration=4f`, `public GameObject questionMark`
- **Stun:** `public float stunDuration=0.25f`
- **Pathfinding:** `public float gridCellSize=0.5f`, `public int gridRadius=20`, `public float waypointReachDistance=0.4f`, `public float pathRecalculateDistance=2f`
- **Search Behaviour:** `private float searchDuration=0.6f` — lastKnownPlayerPos'a ulaşınca sağ-sol 90° tarama, bulamazsa patrol'e dön
- **Props:** `public Vector2 CurrentLookDirection { get; }`, `public bool IsChasing { get; }`
- **Public methods:** `public void SetPlayerVisible(bool)`, `public void ScaleStats(int minute)`
- **OnEnable():** SetRandomPatrolTarget() + RegisterEnemy
- **OnDisable():** StopAllCoroutines() + UnregisterEnemy
- **Patrol:** BFS tabanlı (FindPathAsync coroutine); patrol hedefine ulaşınca yeni rastgele hedef; hedef obstacle içindeyse FindNearestFreeCell; SetRandomPatrolTarget'ta IsBlocked kontrolü yapılır
- **Stunned:** stunDuration kadar dur, questionMark aktif
- **Chase:** BFS ile player'a (canSeePlayer=true) veya lastKnownPlayerPos'a (canSeePlayer=false) yürür; lastKnownPlayerPos'a ulaşınca search behaviour başlar
- **Pathfinding (private):** FindPathAsync, IsBlocked (pathfindingMask), FindNearestFreeCell, NudgeAwayFromWalls, WorldToGrid, GridToWorld, GetNeighbors
- **OnTriggerEnter2D:** IsInvulnerable() → TakeDamage() → StartInvulnerability(1f)
- **Depends on:** EnemyManager, PlayerMovement
- **KALDIRILDI:** `chaseNoise` field'ı (kullanılmıyor)

---

### EnemyFOVMesh.cs (MonoBehaviour)
- `public float viewDistance=7f`, `viewAngle=90f`, `public int segments=20`
- `public Transform origin`, `public EnemyAI enemyAI`, `public Transform player`
- `public Color patrolColor`, `chaseColor`
- **Start():** mesh kurulum; meshRenderer.enabled=true; player = FindWithTag("Player")
- **LateUpdate():** UpdateFOVColor() + DrawFOV(); CheckPlayerInFOV()
- **DrawFOV():** Physics2D.Raycast + enemyAI.obstacleMask; vertices[0]=Vector3.zero; world→local transform.InverseTransformPoint
- **CheckPlayerInFOV():** → enemyAI.SetPlayerVisible(bool)
- **NOT:** DayNightCycle event aboneliği yok — mesh her zaman görünür

---

### EnemyManager.cs (MonoBehaviour)
- `public void RegisterEnemy(EnemyAI)`, `public void UnregisterEnemy(EnemyAI)`
- `public void ScaleEnemyStats(int minute)`
- **Internal:** HashSet<EnemyAI> ile takip, PruneNulls() ile null temizleme

---

### EnemySpawner.cs (MonoBehaviour)
- `public GameObject enemyPrefab`, `public Transform player`, `public EnemyManager enemyManager`
- `public float spawnInterval=3f`, `public int chunkSize=20`, `public float offscreenOffset=2`
- `public LayerMask spawnBlockMask`, `public float checkRadius=0.5f`
- `public int prewarmCount=10`, `public Transform poolContainer`, `public int baseSeed=1234567890`
- `public void ReturnToPool(GameObject go)`, `public void ScaleSpawnRate(int minute)`
- **KALDIRILDI:** `obstacleLayer` field'ı (kullanılmıyordu, spawnBlockMask kullanılıyor)

---

### FollowCameraOverlay.cs (MonoBehaviour)
- `public Camera cam`, `public DayNightCycle dayNight`

---

### GameManager.cs (MonoBehaviour)
- **OnNewMinute(int minute):** milestone + ScaleStats + ScaleEnemyStats + ScaleSpawnRate
- **ShowRandomBuffs():** 3 seçenek; Time.timeScale=0
- **ApplyBuff(Buff):** PlayerStats güncelle + UpdateStaminaBarVisual() + Time.timeScale=1

---

### GameOverManager.cs (MonoBehaviour)
- `public void ShowGameOver()`, `HideGameOver()`, `TryAgain()`, `ExitGame()`

---

### GameSession.cs (MonoBehaviour, Singleton)
- `public static GameSession I { get; }`, `public int WorldSeed { get; private set; }`
- `public void SetSeed(int seed)`, `public void SetRandomSeed()`

---

### GroundTint.cs / NightTint.cs (MonoBehaviour)
- `public Color dayColor, nightColor`
- **Apply(bool isNight):** MPB ile renk

---

### HealthUIManager.cs (MonoBehaviour)
- `public void UpdateHealthDisplay(int current, int max)`

---

### MainMenu.cs (MonoBehaviour)
- `public void OnClickStart()`, `public void OnClickQuit()`
- **Depends on:** SeedUtil.FromInput(string)

---

### PickupItem.cs (MonoBehaviour)
- `public void SetItem(SingleUseItem newItem)` — item set edilince ApplyIcon() çağrılır
- **Depends on:** SingleUseInventory.Add(SingleUseItem)

---

### PlayerFOVCone.cs (MonoBehaviour)
- `public float viewAngle=90f`, `viewDistance=12`, `public int segments=90`
- `public Transform origin`, `public LayerMask obstacleMask`, `public DayNightCycle dayNight`
- **OnPhaseChanged(bool):** yalnızca gece aktif

---

### PlayerMovement.cs (MonoBehaviour)
- `public PlayerStats stats`
- `public void UpdateStaminaBarVisual()`, `public bool IsInvulnerable()`, `public void StartInvulnerability(float)`, `public void RefillStamina()`

---

### PlayerPositionDisplay.cs (MonoBehaviour)
- `public Transform player`, `public TextMeshProUGUI positionText`

---

## 3) Layers / Tags

- **Layers:** Player, Enemy, Obstacle, EnemyObstacle, FOV, UI, Ground, IgnoreRaycast
- **Tags:** Player, Enemy
- **Mask kuralları:**
  - `obstacleMask` → sadece **Obstacle** (FOV raycast için)
  - `pathfindingMask` → **Obstacle + EnemyObstacle** (BFS için)
- **Enemy↔Enemy çarpışması:** Physics2D Layer Collision Matrix'te **kapalı** (birbirinden geçerler)
- **Tree/Wall prefabları:** Child "EnemyCollider" GameObject → Layer=**EnemyObstacle**, CircleCollider2D/BoxCollider2D Is Trigger=true, Radius≈0.8
- **Shaders:** FOVMaskShader, FOV/StencilDarken, FOV/StencilWrite

---

## 4) Scene & Inspector Snapshot

### Game.unity Root Objects
Main Camera (Ortho=5), NightOverlay, Player, PlayerFOVCone, EventSystem, Canvas, EnemySpawner, GameOverController, GameManager, EnemyManager, Enemies

### Player
- normalSpeed=5, sprintSpeed=10, stamina=100, staminaRegen=10, dashCooldown=10s, dashDist=2, health=3
- CircleCollider2D r=0.5; Rigidbody2D Dynamic, Continuous, Interpolate On

### PlayerFOVCone
- Angle=90°, Distance=12, Segments=90; ObstacleMask=Obstacle; Turn=10, DeadZone=0.05, SurfaceEpsilon=0.1

### EnemySpawner
- spawnInterval=3s, chunkSize=20, offscreenOffset=2, checkRadius=0.5, prewarm=10, baseSeed=1234567890

### GameManager → DayNightCycle
- cycleDuration=60s, playerFOVObject=None, debugHotkeys=off

### ChunkLoader
- chunkSize=16, loadRadius=2, worldSeed=12345, objectsPerChunk=10, obstacleScaleRange=(0.8,2), usePooling=true, prewarmPerPrefab=12

### Enemy Prefab Hierarchy
- Enemy → EnemyViewCone, EnemyFOVRenderer, LeftEye, RightEye, QuestionMark, PlayerTrigger, **EnemyCollider** (EnemyObstacle layer, trigger)
- Layer=Enemy
- **EnemyFOVMesh:** Origin field'ına EnemyFOVRenderer'ın kendisi atanmalı

### MainMenu.unity
- SeedInput, StartButton, QuitButton; gameSceneName="Game"

---

## 5) Global Build Rules

- Yeni class/file yok (OK-TO-ADD izni olmadan)
- Public API adları değiştirilmez
- Frame başına 0 alloc; object pooling
- Kod öncesi açıklama + onay; sonra yaz
- Debug.Log final patch'ten önce kaldırılır ✅ (tüm scriptlerde kaldırıldı)

---

## 6) OK-TO-ADD

- Private field (timer, cache vb.)
- Private helper method
- #region PATCH markerları

---

## 7) Bekleyen Görevler

*(Tüm görevler tamamlandı)*

---

## 8) Version History

- **v1** — Skeleton
- **v1.1 (2025-10-12)** — API Registry ilk doldurma
- **v1.2 (2026-02-20)** — Canvas sistemi + Inspector snapshot
- **v1.3 (2026-02-20)** — EnemyAI yeniden yazıldı (Patrol BFS + Stun + Chase). EnemyObstacle layer. Tree/Wall EnemyCollider child. Enemy↔Enemy çarpışma kapatıldı. Chest.isOpening silindi. AstarPathfinding kaldırıldı. "," = onaylıyorum.
- **v1.4 (2026-02-20)** — Chase BFS eklendi. Search behaviour (sağ-sol tarama) eklendi. FOV mesh düzeltildi (origin→EnemyFOVRenderer, vertices[0]=zero). EnemyCollider layer Obstacle→EnemyObstacle düzeltildi. SetRandomPatrolTarget'a IsBlocked kontrolü eklendi. Tüm Debug.Log'lar kaldırıldı. ChunkLoader #if UNITY_EDITOR hatası düzeltildi (TrySpawnChest bloğun dışına taşındı). EnemySpawner.obstacleLayer kaldırıldı. EnemyAI.chaseNoise kaldırıldı. PickupItem.SetItem'da ApplyIcon çağrısı eklendi.
