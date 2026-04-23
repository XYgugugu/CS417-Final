# PVZ 3D (CS417 VR Final Project)

A Unity 6 VR course project inspired by Plants vs. Zombies.

## Current Status

This repository contains an in-progress vertical slice with core gameplay systems implemented in code, but it is **not fully QA-complete**.

- Main systems exist: menu/state flow, grid placement, plants, zombies, waves, resources, save/idle data.
- XR interaction is implemented for Meta Quest style controllers.
- Scene authoring and runtime bootstrap are both present.
- There are still known rough edges (see **Known Issues** below).

## What Is Implemented

- Single playable scene target: `Assets/Scenes/SampleScene.unity`
- Match flow: menu -> start -> prep/battle -> win/lose -> restart/menu
- Grid lane defense gameplay (5x7 style lawn)
- Plant types: Sunflower, Peashooter, Wallnut
- Zombie spawning, lane movement, plant/base damage
- Sun and coin economy
- World-space UI (menu, HUD, end panel, cheat toggle)
- Save/load and bounded idle progress logic

## Quick Start (Editor)

1. Open project in Unity 6.
2. Open `Assets/Scenes/SampleScene.unity`.
3. If hierarchy looks incomplete, run:
   - `PVZ3D -> Authoring -> Bake Scene Static (Save)`
4. Enter Play Mode.

## Controls (Current Mapping)

### Meta Quest controllers
- Left thumbstick: move on horizontal plane
- Right thumbstick X: smooth yaw turn
- Right thumbstick Y: disabled in comfort mode by default
- Trigger: grab/select (used for seed drag-drop and UI ray select)
- Left menu button (fallback: Y): recall/toggle menu behavior

### Pickup collection
- Current logic is sweep-based: move controller ray through sun/coin pickups.

### Editor fallback keys
- `Enter`: start match (from menu)
- `Esc`: menu/pause toggle behavior
- `R`: restart after end state
- `1/2/3`: select plant slot
- `Q/W/E/A/S`: quick place selected plant in lanes

## Known Issues 

- Sunlight collection logic not working
- In game control panel placement issuse 
- Some visuals are still runtime-authored/simple placeholders.
- Magenta/purple materials can appear when render pipeline/material references are broken or not repaired correctly.
- OpenXR/Meta warnings may appear depending on local editor/build profile setup.
- Headset ergonomics (panel distance/angle/comfort tuning) may still need device-specific adjustment.

## Main Technical Documents

- `AGENT_IMPLEMENTATION_NOTES.md`
- `IMPLEMENTATION_STATUS_AND_CONTROLS.md`
- `FINAL_HANDOFF.md`

## What To Commit / What Not To Commit

### Commit
- `Assets/**` (with all `.meta` files)
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/**`
- Root docs/config files (`*.md`, `.gitignore`, `.gitattributes`)

### Do not commit
- `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`
- `Build/`, `Builds/`, `*.apk`
- IDE/generated files (`.vs/`, `.vscode/`, `*.csproj`, `*.sln`, `*.slnx`, `*.user`)
- Local temp/helper folders (for example `.utmp/`)

## Note for Teammates

If something looks missing in scene view, first confirm you are in `SampleScene.unity`, then run the authoring bake command before assuming content is lost.
