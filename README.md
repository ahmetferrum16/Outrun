# Outrun

> A top-down 2D action survival game built with Unity. Dodge enemies, survive the night, and grow stronger with each passing minute.

---

## 🎮 Gameplay

- **Dodge & Survive** — Avoid enemies as they grow stronger over time
- **Upgrade System** — Every few minutes, choose one of three random buffs to enhance your character
- **Day/Night Cycle** — Night shrinks your visibility; use the darkness to your advantage
- **Procedural World** — The map generates infinitely around you; no two runs are the same

---

## ⚙️ Technical Highlights

| System | Description |
|---|---|
| **Enemy AI** | BFS-based pathfinding with patrol, chase, and search states |
| **FOV Mesh** | Real-time field-of-view cone rendered via dynamic mesh generation |
| **Procedural Generation** | Chunk-based infinite world with seeded obstacle and chest spawning |
| **Object Pooling** | Zero per-frame GC allocations via pre-warmed enemy and obstacle pools |
| **Day/Night Cycle** | Event-driven phase system affecting visibility and enemy behavior |
| **Buff System** | ScriptableObject-based upgrade system with 7 buff types |
| **Persistent Session** | Seed-based game session with DontDestroyOnLoad singleton |

---

## 🧠 Enemy AI Details

Enemies operate on a three-state finite state machine:

- **Patrolling** — Wanders randomly using BFS pathfinding, avoids obstacles
- **Chasing** — Locks onto the player when spotted within FOV cone; follows last known position if line of sight is lost
- **Search** — Upon reaching the last known position, sweeps left and right 90° before returning to patrol
- Enemy stats (speed, view distance) scale dynamically with elapsed game time

---

## 🛠️ Built With

- **Unity 6000.3.10f1** — Built-in Render Pipeline
- **C#**
- **TextMeshPro**
- Custom shaders for FOV masking and night overlay (StencilWrite / StencilDarken)

---

## 🚀 How to Run

1. Clone the repository
2. Open with **Unity 6000.3.10f1** (Built-in Render Pipeline)
3. Open `Scenes/MainMenu` and press Play
4. Optionally enter a custom world seed on the main menu

---

## 🎮 Controls

| Key | Action |
|---|---|
| `WASD` | Move |
| `Shift` | Sprint |
| `Space` | Dash |

---

## 📌 Status

> 🚧 In active development — gameplay systems complete, polish ongoing.
