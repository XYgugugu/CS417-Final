# PVZ 3D - Agent Implementation Notes (Final)

## Project overview
PVZ 3D is a single-scene Unity 6 VR vertical slice (Plants vs. Zombies style) with runtime bootstrap, event-driven gameplay state, and world-space XR UI.

Core loop:
Menu -> Start -> Prep -> Battle -> Win/Lose -> End Panel -> Restart/Menu

## What is implemented

### 1) Core architecture (stable)
- Scene bootstrap and hierarchy auto-setup: `Assets/Scripts/Core/PVZSceneBootstrap.cs`
- Game state + run state + events:
  - `Assets/Scripts/Core/GameManager.cs`
  - `Assets/Scripts/Core/GameState.cs`
  - `Assets/Scripts/Core/GameEvents.cs`
  - `Assets/Scripts/Core/RunStats.cs`

### 2) Grid / lane gameplay
- Grid cells and lane metadata:
  - `Assets/Scripts/Grid/GridCell.cs`
  - `Assets/Scripts/Grid/LawnGridManager.cs`
- 5 lanes x 7 columns default
- Per-lane zombie spawn points
- Base endpoint at left side

### 3) Resources and economy
- Resource state:
  - `Assets/Scripts/Resources/ResourceManager.cs`
- Passive + collectible sun:
  - `Assets/Scripts/Resources/SunSpawner.cs`
  - `Assets/Scripts/Resources/SunPickup.cs`
- Collectible coins:
  - `Assets/Scripts/Resources/CoinPickup.cs`

### 4) Plants
- Plant data/logic:
  - `Assets/Scripts/Plants/PlantDefinition.cs`
  - `Assets/Scripts/Plants/PlantBase.cs`
  - `Assets/Scripts/Plants/SunflowerPlant.cs`
  - `Assets/Scripts/Plants/PeashooterPlant.cs`
  - `Assets/Scripts/Plants/WallnutPlant.cs`
  - `Assets/Scripts/Plants/Projectile.cs`
  - `Assets/Scripts/Plants/PlantPlacementManager.cs`

### 5) Zombies and waves
- Zombie behavior:
  - `Assets/Scripts/Zombies/ZombieBase.cs`
  - `Assets/Scripts/Zombies/ZombieMovement.cs`
  - `Assets/Scripts/Zombies/ZombieAttack.cs`
- Wave flow:
  - `Assets/Scripts/Waves/WaveManager.cs`
  - `Assets/Scripts/Waves/ZombieSpawner.cs`

### 6) UI / UX (Aaron scope completed)
- UI manager/controllers:
  - `Assets/Scripts/UI/UIManager.cs`
  - `Assets/Scripts/UI/MainMenuController.cs`
  - `Assets/Scripts/UI/HUDController.cs`
  - `Assets/Scripts/UI/EndGamePanelController.cs`
  - `Assets/Scripts/UI/CheatPanelController.cs`
  - `Assets/Scripts/UI/UIFactory.cs`
  - `Assets/Scripts/UI/UIInteractionFeedback.cs`
- World-space panels, affordability states, selection state, run summary, restart/menu flow

### 7) Save + idle progress
- Save model/system:
  - `Assets/Scripts/Save/SaveData.cs`
  - `Assets/Scripts/Save/SaveSystem.cs`
- Idle progress:
  - `Assets/Scripts/Save/IdleProgressCalculator.cs`

### 8) Lightweight polish
- Unified game feedback tones/events:
  - `Assets/Scripts/Core/AudioFeedbackManager.cs`
- Added events for fire/base damage feedback in `GameEvents`

## Runtime hierarchy conventions
Bootstrap ensures clear top-level groups:
- `Managers`
- `XR Rig`
- `Environment`
- `Grid`
- `Spawners`
- `UI`
- `Runtime`
- `Debug`

Runtime children are normalized:
- `Runtime/Plants`
- `Runtime/Zombies`
- `Runtime/Pickups`

## Inspector tuning entry points (recommended)
- Match / idle tuning: `GameManager`
- Plant default costs/stats: `PlantPlacementManager`
- Zombie stats: `ZombieSpawner`
- Wave pacing: `WaveManager`
- Sun economy: `SunSpawner`
- Cheat increments: `CheatPanelController`
- Save path/logging: `SaveSystem`

## How to run (Editor)
1. Open project in Unity 6.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Enter Play mode.
4. Start from world-space menu.

## XR validation quick path
- Use XR Device Simulator in Editor (if no headset connected).
- Confirm UI ray click works on Main Menu / HUD / End panel.
- Confirm grid cell placement works via XR interaction.

## Known limitations
- Many visuals are still runtime-authored geometric meshes, not fully hand-authored art prefabs.
- Some systems remain runtime-built for robustness and quick class iteration.
- OpenXR/Quest platform warnings may still appear depending on local build profile settings.

## Suggested next improvements (post-submission)
1. Replace runtime-generated visuals with authored prefabs/material sets.
2. Add real audio assets replacing generated tones.
3. Convert major runtime UI panels into prefab assets for artist/designer iteration.
4. Add one dedicated tutorial prompt panel for first-time users.
