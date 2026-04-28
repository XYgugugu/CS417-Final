# Aaron's UI Module (PvZ VR)

> This is the UI/UX layer owned by **Aaron Wang**. All UI code lives under
> `Assets/PVZ/UI/` and is **fully decoupled** from the rest of the team's
> gameplay code — the UI never reads or writes anything outside this folder
> except via the `PVZ3D.UI.GameState` static API.
>

---

## Quick Visual Tour

| Start menu (`StartMenu.unity`) | In-game HUD (`HUDCanvas` prefab) |
|---|---|
| ![Start menu](Docs/fig1_start_menu.png) | ![In-game HUD](Docs/fig2_hud_ingame.png) |
| §0 / §6 *Quit* / §6 *Inter-session Save* | §0 / §1 / §6 *Trigger Integrations* |

| Victory screen — Scene view (3D) | Defeat screen — Game view |
|---|---|
| ![Victory](Docs/fig3_victory_scene.png) | ![Defeat](Docs/fig4_defeat_game.png) |
| §6 *Win Condition / Scoreboard* (`GameState.Win()`) | §6 *Restart* (`GameState.Lose()` triggered, screen shows RESTART / MAIN MENU / QUIT) |

---

## 0. TL;DR

**Building a new level?** Drop these two prefabs into your scene and the
entire UI layer is in:

| Prefab | Path | Contains |
|---|---|---|
| `HUDCanvas` | `Assets/PVZ/UI/Prefabs/HUDCanvas.prefab` | Health bar / Wave bar / Sun / Coin / Plant tray / Countdown / Win-Lose screen |
| `GameSceneRoot` | `Assets/PVZ/UI/Prefabs/GameSceneRoot.prefab` | Auto-managed save / Idle progress / Loss timer bridge / Plant cooldown ticking |

**Calling the UI from gameplay?** Everything goes through
`PVZ3D.UI.GameState`. See the API quick-reference in §2.

---

## 1. Architecture

```
┌──────────────────────────────────────────────────────────┐
│  Your gameplay code                                       │
│                                                          │
│   if (sunflowerProducedSun) {                            │
│       GameState.AddSun(25);    ← the only contact point  │
│   }                                                      │
└────────────────────┬─────────────────────────────────────┘
                     ▼
┌──────────────────────────────────────────────────────────┐
│  GameState  (static class, Assets/PVZ/UI/Core/)          │
│                                                          │
│   • Single source of truth (Sun / Coins / HP / Wave …)   │
│   • Broadcasts changes via C# static events              │
└────────────────────┬─────────────────────────────────────┘
                     ▼
┌──────────────────────────────────────────────────────────┐
│  Aaron's UI components (HealthBarUI, ScoreboardUI, …)    │
│                                                          │
│   void OnEnable() {                                      │
│       GameState.OnSunChanged += UpdateLabel;             │
│   }                                                      │
└──────────────────────────────────────────────────────────┘
```

**Key principles:**

1. **One-way dependency** — gameplay code calls `GameState`; UI reacts via
   events. The UI never calls into gameplay code.
2. **Zero file conflicts** — every UI script/asset lives in
   `Assets/PVZ/UI/`. Nothing else is touched.
3. **Event-driven** — never poll `GameState` every frame; subscribe instead.

---

## 2. Full API Index

> Everything below is `public static` on `PVZ3D.UI.GameState`.

### Resources

```csharp
GameState.Sun;                        // int — current sun
GameState.Coins;                      // int — current coins
GameState.AddSun(int amount);         // amount can be negative; clamps ≥ 0
GameState.AddCoins(int amount);
GameState.TrySpendSun(int cost);      // returns bool. Always true under cheat mode.
GameState.TrySpendCoins(int cost);
GameState.OnSunChanged;               // event Action<int>
GameState.OnCoinsChanged;             // event Action<int>
```

### Player Health

```csharp
GameState.Health;                     // int — current
GameState.MaxHealth;                  // int
GameState.SetHealth(int current, int max);
GameState.DamagePlayer(int amount);   // auto-fires Lose() when HP hits 0
GameState.HealPlayer(int amount);
GameState.OnHealthChanged;            // event Action<int,int>(current, max)
```

### Waves & Score

```csharp
GameState.CurrentWave;
GameState.TotalWaves;
GameState.ZombiesDefeated;
GameState.HighestWaveReached;         // persisted across sessions (high-score)
GameState.SetWave(int current, int total);
GameState.RecordZombieDefeated();
GameState.OnWaveProgressed;           // event Action<int,int>(current, total)
GameState.OnZombieDefeated;           // event Action<int>(totalSoFar)
```

### Plant Cooldowns / Unlocks

```csharp
GameState.StartPlantCooldown(string plantId, float seconds);
GameState.IsPlantReady(string plantId);
GameState.IsPlantUnlocked(string plantId);
GameState.UnlockPlant(string plantId);
GameState.PlantCooldowns;             // IReadOnlyDictionary<string, float>
GameState.OnPlantCooldownTick;        // event Action<string,float,float>(id, remain, total)
GameState.OnPlantUsed;                // event Action<string>(id) — fired the moment a card is consumed
GameState.OnPlantUnlocked;            // event Action<string>(id)
// Note: cooldown is ticked automatically by GameSceneBootstrap.Update each
// frame. You only need to call StartPlantCooldown — no manual TickCooldowns.
```

### Loss Timer

```csharp
GameState.LossTimerRemain;            // float — seconds remaining
GameState.LossTimerTotal;
GameState.LossTimerRunning;
// Usually you don't call these directly — LossTimerBridge mirrors GameManager
// for you. If you want to drive the timer manually instead:
GameState.StartLossTimer(120f);
GameState.TickLossTimer(remain);      // call every frame with new remaining seconds
GameState.StopLossTimer();
GameState.OnLossTimerTick;            // event Action<float,float>(remain, total)
```

### Game Flow

```csharp
GameState.IsGameOver;
GameState.DidWin;
GameState.CheatModeEnabled;
GameState.Win();                      // shows victory screen
GameState.Lose();                     // shows defeat screen
GameState.SetCheatMode(bool);         // also injects +9999 sun & coins when enabled
GameState.OnGameWon;                  // event Action
GameState.OnGameLost;                 // event Action
GameState.OnStateReset;               // event Action — fires when a new run starts
```

---

## 3. Adding / Replacing Plants (no code required)

The full step-by-step is in the XML doc on top of
[`Core/PlantCardData.cs`](Core/PlantCardData.cs). Short version:

1. Drop a portrait PNG into `Assets/PVZ/UI/Sprites/PlantCards/`.
2. Right-click in Project → `Create → PVZ → Plant Card Data`. Fill the
   fields (`plantId`, `displayName`, `icon`, `sunCost`, `cooldownSeconds`,
   `unlockedByDefault`).
3. (Optional) Drag the new asset into
   `GameSettings_Default → Default Unlocked Plants` so it's available in a
   fresh save.
4. Drag `Assets/PVZ/UI/Prefabs/PlantCard.prefab` into
   `HUDCanvas/HUDRoot/PlantTray`.
5. On the new instance's `PlantCardSlotUI`, change the `Card` field to your
   new `PlantCardData` asset.
6. Right-click the `PlantCardSlotUI` header → **Sync Icon From Card Data**
   to bake the sprite into the scene file (so the editor preview matches
   Play mode).

---

## 4. File Layout

```
Assets/PVZ/UI/
├── README.md                   ← you're reading it
├── Core/                       ← data layer + bridges (zero scene deps)
│   ├── GameState.cs            ⭐ static event bus (your API entry)
│   ├── GameSceneBootstrap.cs   gameplay-scene lifecycle
│   ├── SaveSystem.cs           JSON save (Application.persistentDataPath/pvz_save.json)
│   ├── SaveData.cs             serializable struct
│   ├── IdleProgressCalculator.cs   offline-time → resource gain
│   ├── LossTimerBridge.cs      mirrors PVZ3D.Core.GameManager.LossTimer
│   ├── StartMenuRequest.cs     cross-scene intent (PlayerPrefs)
│   ├── GameSettings.cs         SO: global config (HP, idle rates…)
│   └── PlantCardData.cs        SO: per-plant config
├── HUD/                        ← in-game UI elements
│   ├── HUDController.cs        toggles HUD vs Win-Lose root
│   ├── HealthBarUI.cs
│   ├── SunCounterUI.cs
│   ├── CoinCounterUI.cs
│   ├── ScoreboardUI.cs         wave bar + sliding flag (with brain badge)
│   ├── PlantCardSlotUI.cs      ⭐ single plant card (cooldown / flash / dim)
│   ├── CountdownUI.cs          MM:SS + bar + low-time pulse
│   └── IdleProgressToastUI.cs  "Welcome back" toast
├── Menu/                       ← main menu
│   ├── StartMenuController.cs  New Game / Continue / Cheat / Quit wiring
│   └── TombstoneButton.cs      hover/press animation
├── WinLose/
│   └── WinLoseUI.cs            VICTORY! / GAME OVER screen
├── Prefabs/
│   ├── HUDCanvas.prefab        ← drag into any gameplay scene
│   ├── GameSceneRoot.prefab    ← drag into any gameplay scene
│   └── PlantCard.prefab        ← template for individual plant cards
├── Fonts/
│   ├── Creepster-Regular.ttf
│   └── Creepster SDF.asset     TMP SDF font asset
├── Sprites/
│   ├── hp_health.png, sun_icon.png, coin_icon.png, progress_flag.png
│   └── PlantCards/             15 plant portraits
├── GameSettings_Default.asset
├── PlantCard_PeaShooter.asset
└── PlantCard_Sunflower.asset
```

---

## 5. Course Rubric Mapping (13 pts)

| Item | Implementation | How to verify |
|---|---|---|
| Win Condition / Scoreboard (2) | `ScoreboardUI` + `WinLoseUI` | Call `SetWave(2,5)` then `Win()` |
| Restart (1) | `WinLoseUI.HandleRestart` → `GameSceneBootstrap.RestartGameplayScene` | Trigger `GameState.Win()` or `Lose()` first to surface the screen, then click RESTART. After click: save is deleted, scene reloads with fresh defaults (cheat boost still applies if Cheat Mode is on). |
| Quit (1) | `StartMenuController.HandleQuit` + `WinLoseUI.HandleQuit` | Click QUIT in start menu / win-lose screen |
| Inter-session Save (3) | `SaveSystem` (full state JSON) | Change sun in Play, exit, re-enter — value persists |
| Idle Progress (3) | `IdleProgressCalculator` + `GameSceneBootstrap.ApplyIdleRewards` | Quit Play, wait a few minutes, re-enter — sun/coins +N |
| Trigger Integrations (3) | 12 `GameState` events; UI subscribes per-component | Call any `GameState.*` mutator; UI updates instantly |

