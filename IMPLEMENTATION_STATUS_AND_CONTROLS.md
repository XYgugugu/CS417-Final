# PVZ 3D – Current Implementation, Controls, and Scene Authoring Guide

This document describes the **current playable implementation** in this branch, including:
- what is implemented,
- which buttons/inputs do what,
- how the systems are wired,
- and how to open or regenerate the full scene in the Unity Editor.

---

## 1) Current Implementation Status

The project is currently a **single-scene VR vertical slice** with:
- Main menu, HUD, endgame panel, and cheat toggle (world-space UI)
- Match loop: Menu -> Prep -> Battle -> Win/Lose -> Restart/Menu
- 3 core plants (Sunflower, Peashooter, Wallnut)
- Zombie waves with lane movement and base damage
- Resource loop (sun + coins), including pickup collection
- Save/load + idle progress reward on startup
- VR-first interaction flow (controller ray UI + drag seeds from tray)

Primary scene target:
- `Assets/Scenes/SampleScene.unity`

---

## 2) Controller Input Map (Meta Quest)

### Movement and View
- **Left thumbstick**: move player in the horizontal plane.
- **Right thumbstick X**: smooth yaw turn (left/right look).
- **Right thumbstick Y**: disabled by default in comfort mode (to reduce motion sickness).

Implementation:
- `Assets/Scripts/Interaction/XRFlightLocomotionController.cs`
- `comfortMode = true` forces right-stick pitch off by default.

### Menu Recall / Pause / Resume
- **Left menu button** (or fallback **Left secondary button = Y**) toggles menu behavior:
1. If in `Prep` or `Battle`: pauses match and shows menu in front.
2. If in `Paused`: resumes match and returns to HUD.
3. If already in menu-like state: repositions menu in front.

Implementation:
- `Assets/Scripts/Interaction/XRMenuHotkeyController.cs`

### Plant Interaction (Drag-and-Drop)
- **Trigger** is used as grab/select for seed objects.
- Grab a seed from the tray, move it over a valid grid cell, release to place.
- If drop is invalid, placement fails safely and seed returns home.

Implementation:
- `Assets/Scripts/Interaction/XRControllerTriggerGrabConfigurator.cs`
- `Assets/Scripts/Interaction/PlantDragSeed.cs`
- `Assets/Scripts/Interaction/PlantTraySpawner.cs`

### Sun/Coin Collection
- Sweep the controller ray over pickups to collect.
- Current default is **no button hold required** (`requireButtonHold = false`).
- Optional hold mode supports grip / primary / secondary buttons if enabled later.

Implementation:
- `Assets/Scripts/Interaction/XRPickupSweepCollector.cs`

### UI Clicks
- Use controller ray + trigger select on world-space UI buttons/toggles.
- Event system is configured for XR UI module and tracked raycaster support.

Implementation:
- `Assets/Scripts/UI/UIManager.cs`
- `Assets/Scripts/Core/PVZSceneBootstrap.cs`

---

## 3) Editor Keyboard Fallback (for quick debugging)

When running in Editor, fallback keys are available:
- `Enter`: Start match (from menu)
- `Esc`: Pause/Resume/Menu toggle behavior
- `R`: Restart (from win/lose)
- `1/2/3`: Select plant slot
- `Q/W/E/A/S`: Quick place selected plant into lanes 1-5

Implementation:
- `Assets/Scripts/Interaction/VRControllerGameplayFallback.cs`

---

## 4) How the Scene Is Built and Shown in Editor

The project supports **authored static scene content** and a **runtime safety bootstrap**.

### Intended workflow
1. Open: `Assets/Scenes/SampleScene.unity`
2. If scene is empty/minimal, auto-bake hook can populate and save authoring hierarchy.
3. If needed, manually run:
   - `PVZ3D -> Authoring -> Bake Scene Static (Save)`

Editor baker implementation:
- `Assets/Scripts/Editor/PVZSceneAuthoringBaker.cs`

Runtime fallback/bootstrap:
- `Assets/Scripts/Core/PVZSceneBootstrap.cs`

Expected top-level hierarchy after bake/bootstrap:
- `Managers`
- `XR Rig`
- `Environment`
- `Grid`
- `Spawners`
- `UI`
- `Runtime`
- `Debug`

---

## 5) Core System Wiring (Implementation Overview)

### Match and state
- `GameManager` is source of truth for phase, resources snapshot, base health, wave state, run stats.
- Event-driven updates are broadcast via `GameEvents`.

Files:
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Core/GameState.cs`
- `Assets/Scripts/Core/GameEvents.cs`
- `Assets/Scripts/Core/RunStats.cs`

### Grid, plants, zombies, waves
- Grid occupancy and lane indexing via `LawnGridManager` + `GridCell`.
- Plant costs/placement/combat in plant system scripts.
- Wave and zombie spawning in wave/zombie scripts.

### UI and UX flow
- UIManager creates/binds world-space canvases and controls menu/HUD/end panels.
- Menu in front behavior is safe-positioned to avoid occlusion overlap.

### Save and idle progress
- Persistent save JSON under `Application.persistentDataPath`.
- Idle rewards are bounded and applied at startup.

Files:
- `Assets/Scripts/Save/SaveSystem.cs`
- `Assets/Scripts/Save/SaveData.cs`
- `Assets/Scripts/Save/IdleProgressCalculator.cs`

---

## 6) Gameplay Flow (What players should see)

1. Player enters scene and sees/recalls the main menu panel.
2. Press **Start Match**.
3. During prep/battle:
   - collect sun by sweeping ray over sun pickups,
   - grab seeds from tray and drop onto grid cells,
   - defend lanes as zombies approach.
4. End state:
   - victory or defeat panel appears with run stats,
   - restart or return to menu.

---

## 7) Quick Troubleshooting

### I cannot see the full scene in Editor
- Open exactly `Assets/Scenes/SampleScene.unity`.
- Run `PVZ3D -> Authoring -> Bake Scene Static (Save)`.
- Check hierarchy for `Managers/XR Rig/Environment/Grid/UI`.

### Menu seems missing in headset
- Press **left menu button** or **Y** (fallback) to recall it.
- In gameplay phase this pauses; pressing again resumes.

### Turning feels nauseating
- Keep `comfortMode = true` in `XRFlightLocomotionController`.
- This disables right-stick vertical pitch by default.

---

## 8) Pre-Push Checklist

- Open `Assets/Scenes/SampleScene.unity`
- Verify scene has full hierarchy (not only camera/light)
- Enter Play and validate:
  - menu start
  - plant drag-drop
  - pickup collection
  - win/lose panel and restart
- Save scene and project before push
- Commit updated scripts + this document

### What should be pushed
- `Assets/**` (and every corresponding `.meta`)
- `Packages/manifest.json` and `Packages/packages-lock.json`
- `ProjectSettings/**`
- Root docs and config (`.gitignore`, `.gitattributes`, `*.md`)

### What should NOT be pushed
- `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`
- `Build/`, `Builds/`, and generated binaries (`*.apk`)
- IDE/generated files (`.vs/`, `.vscode/`, `*.csproj`, `*.sln`, `*.slnx`, `*.user`)
- Local temp folder (`.utmp/`)
