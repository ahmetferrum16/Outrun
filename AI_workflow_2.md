# PROJECT API REGISTRY & BUILD RULES

*Reusable Mode Spec | v1.6*

Son güncelleme: 2026-03-15

---

## 🔁 Canvas Güncelleme Sistemi

### Temel Kurallar

- "," sembolü = onaylıyorum. Kullanıcı "," yazıp enter'a basarsa onay anlamına gelir.
- Reddedilen versiyonlar yok sayılır; yalnızca son onaylanan versiyon canvas'a yansıtılır.
- "Canvas'ı gönder" komutu: tüm konuşma taranır, onaylanan değişiklikler derlenir, dosya sunulur.
- Kullanıcı bağımsız kod eklemişse bunu belirtir, Claude canvas'ı buna göre günceller.
- Makul aralıklarla canvas al: ~80-100 mesajda bir.
- Fonksiyon paylaşımlarında üste `// ClassName.cs` notu zorunlu.
- Kod paylaşımlarında her zaman **komple fonksiyon** ver, yarım kod verme.

### Yeni Chat Geçiş Prosedürü

1. Mevcut chatte "canvas'ı gönder" de
2. Claude güncel canvas dosyasını oluşturur
3. Yeni chat aç → canvas içeriğini yapıştır → devam et

### Starter Block (yeni chate kopyala)

```
PROJECT MODE SPEC v1 --- paste-in

Bu belgedeki kurallara sıkı sıkıya uy:

- Yalnızca API Registry'de listelenen public API'leri kullan.
- Yeni class/method üretme; Mode Spec'te OK-TO-ADD izni olmadan.
- Çıktıyı unified diff + exact placement talimatlarıyla ver.
- Frame başına GC alloc = 0.
- Eksik API varsa icat etme, raporla.
- Canvas Güncelleme Sistemi kurallarını uygula (belgenin üst bölümüne bak).
- "," sembolü = onaylıyorum.
- Fonksiyon paylaşımlarında üste // ClassName.cs notu zorunlu.
- Kod paylaşımlarında her zaman komple fonksiyon ver.

Bugünkü odak:
- <buraya yaz>
```

---

## 1) Project Overview

| Alan | Değer |
|---|---|
| Oyun türü | Top-down 2D action |
| Engine | Unity 6000.3.10f1, Built-in Render Pipeline (SRP = None) |
| Hedef platform | Windows (PC) |
| Sahneler | Scenes/MainMenu, Scenes/Game |
| Core loop | dodge → survive → upgrades |
| Core sistemler | PlayerMovement, PlayerAnimationController, EnemyAI, EnemyFOVMesh, EnemyManager, EnemySpawner, ChunkLoader, Buff System, GameManager, DayNightCycle, GameSession |

---

## 2) API Registry (authoritative)

> ⚠️ NOT: Aşağıdaki API'lerin dışına çıkılmaz. Eksik API varsa icat etme, raporla.

### Bootstrapper.cs (MonoBehaviour)

- `Awake()`: GameSession.I yoksa yeni `GameObject('GameSession')` + AddComponent + DontDestroyOnLoad. SetRandomSeed(). `Time.timeScale = 1`.

### Buff.cs (ScriptableObject)

- `public enum BuffType { Speed, Stamina, Cooldown, SprintSpeed, StaminaRegen, DashDistance, CameraZoom }`
- `public string buffName`, `public BuffType type`, `public float amount`

### BuffButton.cs (MonoBehaviour)

- `public void Setup(Buff buff, GameManager manager)`
- `public void OnClick()` --- `gameManager.ApplyBuff(currentBuff)`

### CameraFollow.cs (MonoBehaviour)

- `public Transform target`, `public float smoothSpeed = 0.125f`, `public Vector3 offset`
- `LateUpdate()`: `Vector3.Lerp`; Z sabit

### Chest.cs (MonoBehaviour)

- `public float openRadius, openTime, drainSpeed`
- `public List<SingleUseItem> lootTable`
- `public Transform spawnPoint`
- `public ChestProgressUI progressUI`, `public ChestRangeRing rangeRing`
- Runtime private: `Transform player`, `float timer`
- `Open()`: rastgele loot → `PickupItem.SetItem(loot)` → Destroy

### ChestProgressUI.cs (MonoBehaviour)

- `public void SetProgress(float progress01, bool visible)`

### ChestRangeRing.cs (MonoBehaviour)

- `public void Draw(Vector3 center, float radius)`
- `public void SetVisible(bool v)`

### ChunkLoader.cs (MonoBehaviour)

- `public Transform player`, `public GameObject groundPrefab`
- `int chunkSize=16`, `int loadRadius=2`, `int unloadBuffer=0`
- `int worldSeed` (GameSession.I.WorldSeed ile override)
- `GameObject[] spawnPrefabs=[Tree,Wall]`, `int objectsPerChunk=10`, `float spawnCheckRadius=1`, `LayerMask obstacleMask`
- `Vector2 obstacleScaleRange=(0.8,2)`
- `bool usePooling=true`, `int prewarmPerPrefab=12`
- `GameObject chestPrefab`, `int chestEveryMinChunks=1`, `int chestEveryMaxChunks=3`, `float chestMinDistanceFromPlayer=6`, `float chestCheckRadius=0.6`, `LayerMask chestBlockMask=Enemy|Obstacle|Player`
- ⚠️ NOT: `TrySpawnChestInChunk` ve `FindFreePositionInChunk` `#if UNITY_EDITOR` bloğunun DIŞINDA olmalı --- aksi halde build'de chest spawn olmaz.

### DayNightCycle.cs (MonoBehaviour)

- `public float cycleDuration=60f`
- `public GameObject playerFOVObject` (Inspector'da None)
- `public bool IsNight { get; }`, `public float PhaseProgress01 { get; }`
- `public static event DayNightChange OnDayNightChanged`
- `public void AddTime(float seconds)`

### DayNightUI.cs (MonoBehaviour)

- `public DayNightCycle dayNight`, `public TextMeshProUGUI label`, `public Image fillImage`
- `public string dayText='DAY'`, `nightText='NIGHT'`

### EnemyAI.cs (MonoBehaviour)

- `public enum State { Patrolling, Stunned, Chasing }`
- Perception: `public float viewDistance=7f`, `viewAngle=90f`, `public LayerMask obstacleMask`, `pathfindingMask`
- Movement: `public float moveSpeed=2f`, `rotationSpeed=10f`, `patrolRange=5f`
- Chase: `public float chaseMemoryDuration=4f`, `public GameObject questionMark`
- Stun: `public float stunDuration=0.25f`
- Pathfinding: `public float gridCellSize=0.5f`, `public int gridRadius=20`, `waypointReachDistance=0.4f`, `pathRecalculateDistance=2f`
- Search Behaviour: `private float searchDuration=0.6f` --- lastKnownPlayerPos'a ulaşınca sağ-sol 90° tarama, bulamazsa patrol'e dön
- Props: `public Vector2 CurrentLookDirection { get; }`, `public bool IsChasing { get; }`
- Public methods: `public void SetPlayerVisible(bool)`, `public void ScaleStats(int minute)`
- `OnEnable()`: SetRandomPatrolTarget() + RegisterEnemy | `OnDisable()`: StopAllCoroutines() + UnregisterEnemy
- KALDIRILDI: `chaseNoise` field'ı

### EnemyFOVMesh.cs (MonoBehaviour)

- `public float viewDistance=7f`, `viewAngle=90f`, `public int segments=20`
- `public Transform origin`, `public EnemyAI enemyAI`, `public Transform player`
- `public Color patrolColor`, `chaseColor`
- `Start()`: mesh kurulum; `player = FindWithTag('Player')`
- `LateUpdate()`: `UpdateFOVColor()` + `DrawFOV()`; `CheckPlayerInFOV()`
- NOT: DayNightCycle event aboneliği yok --- mesh her zaman görünür

### EnemyManager.cs (MonoBehaviour)

- `public void RegisterEnemy(EnemyAI)`, `public void UnregisterEnemy(EnemyAI)`
- `public void ScaleEnemyStats(int minute)`
- `public int EnemyCount => allEnemies.Count`
- Internal: `HashSet<EnemyAI>` ile takip, `PruneNulls()` ile null temizleme

### EnemySpawner.cs (MonoBehaviour)

- `public GameObject enemyPrefab`, `public Transform player`, `public EnemyManager enemyManager`
- `public float spawnInterval=4f` (başlangıç), `public int chunkSize=20`, `public float offscreenOffset=2`
- `public LayerMask spawnBlockMask`, `public float checkRadius=0.5f`
- `public int prewarmCount=10`, `public Transform poolContainer`, `public int baseSeed=1234567890`
- `public void ReturnToPool(GameObject go)`
- `public void ScaleSpawnRate(int minute)`
- KALDIRILDI: `obstacleLayer` field'ı

### GameManager.cs (MonoBehaviour)

- `OnNewMinute(int minute)`: milestone + ScaleStats + ScaleEnemyStats + ScaleSpawnRate
- `ShowRandomBuffs()`: 3 seçenek; `Time.timeScale=0`
- `ApplyBuff(Buff)`: PlayerStats güncelle + `UpdateStaminaBarVisual()` + `Time.timeScale=1`

### GameOverManager.cs (MonoBehaviour)

- `public void ShowGameOver(PlayerAnimationController animController = null)`
- `private IEnumerator GameOverRoutine(PlayerAnimationController animController)`: ölüm animasyonu oynar (1s), sonra `Time.timeScale=0`, gameOverPanel aktif
- `public void HideGameOver()`, `TryAgain()`, `ExitGame()`

### GameSession.cs (MonoBehaviour, Singleton)

- `public static GameSession I { get; }`, `public int WorldSeed { get; private set; }`
- `public void SetSeed(int seed)`, `public void SetRandomSeed()`

### GroundTint.cs / NightTint.cs (MonoBehaviour)

- `public Color dayColor, nightColor`
- `Apply(bool isNight)`: MPB ile renk

### HealthUIManager.cs (MonoBehaviour)

- `public void UpdateHealthDisplay(int current, int max)`

### MainMenu.cs (MonoBehaviour)

- `public void OnClickStart()`, `public void OnClickQuit()`
- Depends on: `SeedUtil.FromInput(string)`

### PickupItem.cs (MonoBehaviour)

- `public void SetItem(SingleUseItem newItem)` --- item set edilince `ApplyIcon()` çağrılır
- Depends on: `SingleUseInventory.Add(SingleUseItem)`

### PlayerAnimationController.cs (MonoBehaviour)

- Animator ve Rigidbody2D ile çalışır
- 6 yön desteği: Down, RightDown, RightUp, Up, LeftUp, LeftDown
- Animasyon tipleri: Idle, Walk, Dash, Death (her biri 6 yönlü)
- Sprite: The Adventurer (Male) — 48x64px, 8 frame/yön, PPU=32
- Sample Rate: 8 (Dash: 16)
- Animator Controller: PlayerAnimator.controller
- `public void PlayDeath()`: ölüm animasyonunu tetikler, hareketi durdurur
- ⚠️ BİLİNEN BUG: Dash cooldown beklenirken shift'e basılsa bile animasyon tetiklenebilir (düşük öncelik)

### PlayerFOVCone.cs (MonoBehaviour)

- `public float viewAngle=90f`, `viewDistance=12`, `public int segments=90`
- `public Transform origin`, `public LayerMask obstacleMask`, `public DayNightCycle dayNight`
- `OnPhaseChanged(bool)`: yalnızca gece aktif

### PlayerMovement.cs (MonoBehaviour)

- `public PlayerStats stats`
- `public bool CanDash => stats.hasDash && Time.time >= lastDashTime + stats.dashCooldown`
- `public void SetDead()`: hareketi durdurur (isDead=true, velocity=zero)
- Dash: coroutine tabanlı (0.15s süreli hızlı hareket), teleport değil
- `public void UpdateStaminaBarVisual()`, `public bool IsInvulnerable()`, `public void StartInvulnerability(float)`, `public void RefillStamina()`

### PlayerPositionDisplay.cs (MonoBehaviour)

- `public Transform player`, `public TextMeshProUGUI positionText`

### PlayerStats.cs (MonoBehaviour)

- `public void TakeDamage()`: can azaltır; 0'da `SetDead()` + `ShowGameOver(animController)` çağırır
- `public void ScaleStats(int minute)`: staminaRecoverRate += 0.5f

### QuickbarUI.cs (MonoBehaviour)

- `public Image[] slotIcons = new Image[5]`
- `public Image[] slotFrames`
- `public SingleUseInventory inventory`
- `public void Refresh()` --- sprite atar; `SetNativeSize()` KULLANILMAZ

### SingleUseInventory.cs (MonoBehaviour)

- `public const int SlotCount = 5`
- `public SingleUseItem[] Slots`, `public int SelectedIndex`
- `public Action OnChanged`
- `public bool Add(SingleUseItem)`, `public void Select(int)`, `public void UseSelected()`

---

## 3) Sprite Sistemi

### Player Sprite (The Adventurer - Male, Sscary)

| Parametre | Değer |
|---|---|
| Kare boyutu | 48x64 px |
| PPU | 32 |
| Filter Mode | Point (no filter) |
| Animasyonlar | Idle, Walk, Dash, Death |
| Yönler | Down, RightDown, RightUp, Up, LeftUp, LeftDown |
| Sample Rate | 8 (Dash: 16) |
| Animator | PlayerAnimator.controller |

### Spritesheet Satır Düzeni (idle, walk, dash, death)

| Satır | Kareler | Yön |
|---|---|---|
| 0 | 0-7 | Down |
| 1 | 8-15 | LeftDown |
| 2 | 16-23 | LeftUp |
| 3 | 24-31 | Up |
| 4 | 32-39 | RightUp |
| 5 | 40-47 | RightDown |

### Animator Klasör Yapısı

```
Assets/Animations/Player/
    PlayerAnimator.controller
    Idle/   → Player_Idle_Down, LeftDown, LeftUp, Up, RightUp, RightDown
    Walk/   → Player_Walk_Down, LeftDown, LeftUp, Up, RightUp, RightDown
    Dash/   → Player_Dash_Down, LeftDown, LeftUp, Up, RightUp, RightDown
    Death/  → Player_Death_Down, LeftDown, LeftUp, Up, RightUp, RightDown
```

### Enemy Sprite

- Henüz eklenmedi (sonraki oturumda)

---

## 4) Layers / Tags

| Alan | Değer |
|---|---|
| Layers | Player, Enemy, Obstacle, EnemyObstacle, FOV, UI, Ground, IgnoreRaycast |
| Tags | Player, Enemy |
| obstacleMask | Sadece Obstacle (FOV raycast için) |
| pathfindingMask | Obstacle + EnemyObstacle (BFS için) |
| Enemy↔Enemy çarpışma | Physics2D Layer Collision Matrix'te KAPALI |
| Tree/Wall prefabları | Child 'EnemyCollider' → Layer=EnemyObstacle, CircleCollider2D/BoxCollider2D Is Trigger=true, Radius≈0.8 |
| Shaders | FOVMaskShader, FOV/StencilDarken, FOV/StencilWrite |

---

## 5) Scene & Inspector Snapshot

### Game.unity Root Objects

`Main Camera (Ortho=5)`, `NightOverlay`, `Player`, `PlayerFOVCone`, `EventSystem`, `Canvas`, `EnemySpawner`, `GameOverController`, `GameManager`, `EnemyManager`, `Enemies`

### Player

| Stat | Değer |
|---|---|
| normalSpeed | 5 |
| sprintSpeed | 10 |
| stamina | 100 |
| staminaRegen | 10 |
| dashCooldown | 10s |
| dashDist | 2 |
| health | 3 |
| Collider | CircleCollider2D r=0.5 |
| Rigidbody2D | Dynamic, Continuous, Interpolate, **Freeze Rotation Z ✅** |

### Player Components

- PlayerMovement
- PlayerStats
- PlayerAnimationController
- Animator (Controller: PlayerAnimator)
- Sprite Renderer
- Rigidbody2D
- CircleCollider2D
- SingleUseInventory

### EnemySpawner

| Parametre | Değer |
|---|---|
| spawnInterval (başlangıç) | 4s |
| chunkSize | 20 |
| offscreenOffset | 2 |
| checkRadius | 0.5 |
| prewarm | 10 |
| baseSeed | 1234567890 |

### GameManager → DayNightCycle

`cycleDuration=60s`, `playerFOVObject=None`, `debugHotkeys=off`

### ChunkLoader

`chunkSize=16`, `loadRadius=2`, `worldSeed=12345`, `objectsPerChunk=10`, `obstacleScaleRange=(0.8,2)`, `usePooling=true`, `prewarmPerPrefab=12`

### Enemy Prefab Hierarchy

`Enemy` → `EnemyViewCone`, `EnemyFOVRenderer`, `LeftEye`, `RightEye`, `QuestionMark`, `PlayerTrigger`, `EnemyCollider` (EnemyObstacle layer, trigger)

Layer=Enemy

> ⚠️ NOT: EnemyFOVMesh: Origin field'ına EnemyFOVRenderer'ın kendisi atanmalı

### MainMenu.unity

`SeedInput`, `StartButton`, `QuitButton`; `gameSceneName='Game'`

---

## 6) Global Build Rules

- Yeni class/file yok (OK-TO-ADD izni olmadan)
- Public API adları değiştirilmez
- Frame başına 0 alloc; object pooling
- Kod paylaşımında her zaman **komple fonksiyon** ver, yarım kod verme
- `Debug.Log` final patch'ten önce kaldırılır ✅
- Fonksiyon paylaşımlarında üste `// ClassName.cs` notu zorunlu

---

## 7) OK-TO-ADD

- Private field (timer, cache vb.)
- Private helper method
- `#region PATCH` markerları

---

## 8) Zorluk Sistemi

### EnemySpawner — ScaleSpawnRate Eğrisi

```csharp
// EnemySpawner.cs
public void ScaleSpawnRate(int minute)
{
    if (minute <= 3) spawnInterval = 4f;
    else if (minute <= 8) spawnInterval = Mathf.Max(2.5f, spawnInterval - 0.3f);
    else if (minute <= 12) spawnInterval = Mathf.Max(1.0f, spawnInterval - 0.2f);
    else if (minute <= 15) spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.1f);
    // 15+ dakika 0.5f'de sabit

    StopAllCoroutines();
    StartCoroutine(SpawnRoutine());
}
```

### EnemyAI — ScaleStats (her dakika)

| Stat | Başlangıç | Artış/dk | 10dk'da |
|---|---|---|---|
| moveSpeed | 2.0 | +0.1 | 3.0 |
| viewAngle | 90° | +2° | 110° |
| viewDistance | 7.0 | +0.3 | 10.0 |
| chaseMemory | 4.0s | +0.25s | 6.5s |

### PlayerStats — ScaleStats (her dakika)

- `staminaRecoverRate += 0.5f` (başlangıç 10f)
- Hız, can, dash sabit --- buff sistemi üzerinden güçlenir

---

## 9) Debug / Test

- **T tuşu**: zamanı hızlandırır (SkipTime + `dayNightCycle.AddTime`)
- **OnGUI (GameManager)**: Minute, Spawn Interval, Enemy Count --- test sonrası kaldır

---

## 10) Bekleyen Görevler

- Enemy sprite entegrasyonu (Normal, Dasher, Watcher)
- Nuke item (knockback, ~8 birim yarıçap, öldürmez)
- Wall Block item (8 yönlü ok tuşları, EnemyObstacle layer, geçici)
- Dasher enemy type (chase'de hızlı dash, nadir)
- Watcher enemy type (360° görüş, nadir)
- Labirent sistemi (not: kesin değil — içinde chest + labirent zombileri)

---

## 11) Version History

| Versiyon | Notlar |
|---|---|
| v1 | Skeleton |
| v1.1 (2025-10-12) | API Registry ilk doldurma |
| v1.2 (2026-02-20) | Canvas sistemi + Inspector snapshot |
| v1.3 (2026-02-20) | EnemyAI yeniden yazıldı (Patrol BFS + Stun + Chase) |
| v1.4 (2026-02-20) | Chase BFS + Search behaviour. FOV mesh düzeltildi |
| v1.5 (2026-03-04) | Görsel sistem, QuickbarUI, spawn eğrisi, EnemyManager.EnemyCount |
| v1.6 (2026-03-15) | Player sprite sistemi (The Adventurer Male). PlayerAnimationController eklendi. Dash coroutine tabanlı yapıya geçildi. GameOverManager death animasyonu desteği aldı. Rigidbody2D Freeze Rotation Z eklendi. PlayerMovement.SetDead() eklendi. |
