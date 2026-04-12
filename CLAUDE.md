# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 game project using the Universal Render Pipeline (URP). The game is a 3D arena where the player moves around, interacts with AI-controlled NPC enemies (called "CR"), and completes mission zones.

## Development Workflow

There are no CLI build or test commands. All development happens through the Unity Editor:

- **Run the game:** Open in Unity Editor → Press Play
- **Build:** File > Build and Run in Unity Editor
- **Debugging:** VS Code attach-mode debugger is configured in `.vscode/launch.json` — attach after the Unity Editor is running

The project uses Unity's modern Input System (`InputSystem_Actions.inputactions`) but the scripts currently use the legacy `Input.GetKey` API directly.

## Script Architecture

All game logic lives in `Assets/Scripts/Movement/`. Three scripts make up the entire gameplay loop:

### PlayerManager (`PlayerManager.cs`)
- **Singleton** (`public static PlayerManager Instance`) — accessed by CRManager to get player position/velocity
- WASD moves the player; Space triggers a visual flash (red for 0.25s) and is also the interaction key picked up by each CRManager
- Exposes `playerSpeed` (Vector3, velocity per frame) — currently read-only by other scripts

### CRManager (`CRManager.cs`)
- One instance per NPC. The scene has 6 CR characters.
- **`_playerTransform` must be assigned in the Inspector** for each instance — it is not auto-resolved at runtime
- Three states via `Phase` enum: `idle`, `chase`, `flee`
  - **Idle:** Wanders randomly, changes direction every `_directionChangeInterval` seconds, constrained to `_areaRadius` (25 units) around `_areaCenter` (0,1,0)
  - **Chase/Flee:** Triggered when player presses Space within `_triggerDistance` (20 units). 80% chance chase, 20% chance flee. Lasts `_chaseFleeDuration` (5 seconds), then returns to idle
  - Move speed on trigger = `_distance / 10` (farther away → faster)
- Uses Rigidbody physics: steering force → acceleration → sets `_rb.linearVelocity` directly (not `AddForce`)
- All parameters are `[SerializeField]` — tune them in the Inspector without code changes
- Has an active `Debug.Log` in `Update()` — remove it before any performance-sensitive work

### MissionSpot (`MissionSpot.cs`)
- One instance per zone. The scene has 4 spots.
- **Requires a Trigger Collider** on the same GameObject — `OnTriggerEnter/Exit` only fires if the collider's `isTrigger` is checked
- Player must stand inside for `_completeTime` (20 seconds) to complete the zone
- Color changes: green while player is inside, black when complete
- Timer does **not** reset if the player leaves — progress is preserved

## Key Design Details

- All movement is on the XZ plane. `Flatten()` in CRManager zeroes out the Y component to prevent vertical drift.
- CRManager's `Truncate()` clamps steering force between `_minForce` (5) and `_maxForce` (10) — the minimum means NPCs always move at some speed even when close to target.
- `MissionSpot` checks `coll.enabled` in trigger callbacks, but `coll` is a `[SerializeField]` that must be assigned in the Inspector (it is not fetched via `GetComponent` unlike `rend`).

## Packages

Key packages from `Packages/manifest.json`:
- `com.unity.render-pipelines.universal` 17.0.4 — URP; rendering settings are in `Assets/Settings/`
- `com.unity.ai.navigation` 2.0.6 — NavMesh support (installed but unused in current scripts)
- `com.unity.inputsystem` 1.13.1 — installed but scripts use legacy `Input` API
- `com.unity.test-framework` 1.4.6 — installed but no tests written yet
