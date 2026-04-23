# PVZ 3D - Final Handoff

## Project summary
PVZ 3D is a VR course final project vertical slice built in Unity 6 using XR Interaction Toolkit + OpenXR.

Playable loop:
1. Main menu start
2. Resource collection (sun/coins)
3. Plant selection + placement on lane grid
4. Zombie waves and combat
5. Win/Lose resolution
6. End panel + restart/return menu
7. Save/load + idle catch-up rewards

## Git push guide (important)

### Always push
- `Assets/**` (including all `.meta` files)
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/**`
- `AGENT_IMPLEMENTATION_NOTES.md`
- `FINAL_HANDOFF.md`
- `IMPLEMENTATION_STATUS_AND_CONTROLS.md`
- Root config files that define project behavior (for example `.gitignore`, `.gitattributes`)

### Do NOT push
- `Library/`
- `Temp/`
- `Obj/`
- `Logs/`
- `UserSettings/`
- `Build/` and `Builds/`
- IDE/generated files (`.vs/`, `.vscode/`, `*.csproj`, `*.sln`, `*.slnx`, `*.user`)
- Local helper/temp folders (`.utmp/`)
- Exported binaries (`*.apk`, `*.unitypackage`) unless your instructor explicitly asks for them

### Safe pre-push check
1. `git status` and confirm only source/config/doc files are staged.
2. Ensure no `Library/`, `Temp/`, `Logs/`, or IDE-generated files are in staged changes.
3. Ensure new assets always include matching `.meta` files.

## Main script map by subsystem

### Core
- `Assets/Scripts/Core/PVZSceneBootstrap.cs`: runtime scene assembly, manager creation, XR safety setup
- `Assets/Scripts/Core/GameManager.cs`: phases, run reset, win/lose, core orchestration
- `Assets/Scripts/Core/GameEvents.cs`: event bus
- `Assets/Scripts/Core/RunStats.cs`: end-run stats model
- `Assets/Scripts/Core/AudioFeedbackManager.cs`: lightweight feedback audio

### Grid
- `Assets/Scripts/Grid/LawnGridManager.cs`
- `Assets/Scripts/Grid/GridCell.cs`

### Plants
- `Assets/Scripts/Plants/PlantPlacementManager.cs`
- `Assets/Scripts/Plants/PlantBase.cs`
- `Assets/Scripts/Plants/SunflowerPlant.cs`
- `Assets/Scripts/Plants/PeashooterPlant.cs`
- `Assets/Scripts/Plants/WallnutPlant.cs`
- `Assets/Scripts/Plants/Projectile.cs`

### Zombies / waves
- `Assets/Scripts/Waves/WaveManager.cs`
- `Assets/Scripts/Waves/ZombieSpawner.cs`
- `Assets/Scripts/Zombies/ZombieBase.cs`
- `Assets/Scripts/Zombies/ZombieMovement.cs`
- `Assets/Scripts/Zombies/ZombieAttack.cs`

### Resources / save
- `Assets/Scripts/Resources/ResourceManager.cs`
- `Assets/Scripts/Resources/SunSpawner.cs`
- `Assets/Scripts/Resources/SunPickup.cs`
- `Assets/Scripts/Resources/CoinPickup.cs`
- `Assets/Scripts/Save/SaveSystem.cs`
- `Assets/Scripts/Save/SaveData.cs`
- `Assets/Scripts/Save/IdleProgressCalculator.cs`

### UI
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/UI/HUDController.cs`
- `Assets/Scripts/UI/EndGamePanelController.cs`
- `Assets/Scripts/UI/CheatPanelController.cs`
- `Assets/Scripts/UI/UIFactory.cs`
- `Assets/Scripts/UI/UIInteractionFeedback.cs`

## Scene/bootstrap architecture
- Play mode runtime bootstrap guarantees required roots/managers exist.
- Scene hierarchy is normalized for readability:
  - `Managers`, `XR Rig`, `Environment`, `Grid`, `Spawners`, `UI`, `Runtime`, `Debug`
- Runtime spawn containers:
  - `Runtime/Plants`, `Runtime/Zombies`, `Runtime/Pickups`

## Run in Editor
1. Open `Assets/Scenes/SampleScene.unity`
2. Press Play
3. Aim at world-space Main Menu and select `Start Match`

## Validate in VR
Checklist:
1. Menu readable and clickable at spawn
2. HUD readable while facing lawn
3. Select a plant card and place to a valid lane/cell
4. Zombie waves spawn and move by lane
5. Base can be damaged, win/lose both reachable
6. End panel restart/menu works

## Where to tune gameplay quickly

### Match pacing
- `GameManager`
  - `startingSun`, `baseMaxHealth`, `totalWaves`, `prepDurationSeconds`
  - `idleSunPerMinute`, `idleCoinsPerMinute`, `idleMaxMinutes`

### Plant economy/combat
- `PlantPlacementManager`
  - default plant costs/stats used when no authored definitions are assigned

### Zombie difficulty
- `ZombieSpawner`
  - health/speed/damage/reward for basic/tough

### Wave structure
- `WaveManager`
  - `interWaveDelay`, `presets`

### Sun flow
- `SunSpawner`
  - passive interval + amount

### Demo cheat values
- `CheatPanelController`
  - cheat sun/coin amounts

### Save behavior
- `SaveSystem`
  - save file name and optional save logging

## Persistence behavior
- Save file: `Application.persistentDataPath/pvz3d_save.json` (default name)
- Corrupt save handling: fallback to defaults with warning log
- Idle progress: bounded by configurable minute cap in `GameManager`

## Known issues / risks
1. Visuals are still partly runtime-generated geometry (good for stability, limited art polish).
2. Platform-specific OpenXR warnings can depend on local build settings and plugins.
3. Some XR ergonomics (panel distance/height) may need final per-headset tuning in class environment.
4. Editor compile checks from terminal can lag new files until Unity refreshes Bee inputs; in-editor recompile resolves this.

## Suggested 60-90 second demo script
1. Start in menu, briefly mention objective: protect base through 3 waves.
2. Start match, place Sunflower then Peashooter in 2 lanes.
3. Collect sun/coins and show affordability feedback on plant cards.
4. Show combat: peashooter firing, zombie death, resource gain.
5. Let one zombie hit base once to show damage feedback.
6. Clear wave progression and trigger end result.
7. Open end panel, point to stats, press Restart, and optionally show cheat toggle for quick showcase.

## Future improvements (easy next steps)
1. Replace runtime geometry visuals with authored prefabs + materials.
2. Replace generated tones with curated audio assets.
3. Add a dedicated first-time tutorial panel.
4. Move runtime-built UI to prefab-driven authored UI while keeping same logic/events.
