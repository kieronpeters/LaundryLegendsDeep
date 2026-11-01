# Phase 8 — Arcade business loop, menu fix, character appearance

**Date:** 2026-11-01
**Phase:** 8 — First arcade machine
**Status:** Complete pending owner playtest
**Branch:** `main`

## Objective

Finish Phase 8 (purchase, install, activation, attract mode, visitor income) and fix
owner-reported defects: a game that could not be started with the mouse, grey NPCs,
and missing first-person hands.

## Defects found (with evidence)

| # | Defect | Evidence | Severity |
|---|---|---|---|
| 1 | START GAME / EXIT buttons did nothing in a build | `MainMenu.unity` had `m_OnClick.m_PersistentCalls.m_Calls: []`; `MenuSetupTool` used runtime-only `Button.onClick.AddListener` | Blocker |
| 2 | Menu had no `AudioListener`, so the menu music was silent | No `!u!81` block in `MainMenu.unity` | High |
| 3 | Two `MainMenuController` instances in the menu scene | Stale root `MainMenu` plus `MenuSetup`; Enter routed twice | High |
| 4 | NPC_Attendant and NPC_Customer rendered fully grey | Renderer materials were default `Lit` rgb 0.5,0.5,0.5 with no texture (`TestResults/appearance-dump.txt`) | High |
| 5 | First-person hands invisible | `FirstPersonHands` parented to `PlayerCamera` at local (0,0,0); hand mesh measured 37 cm *behind* its root, i.e. inside the camera | High |
| 6 | No arcade business loop | Only a $2 player-played maze game existed; no purchase/install/attract/visitor/income code | Blocker (Phase 8) |
| 7 | Stopped cabinet kept redrawing a frozen maze | `ArcadeCabinet.Stop()` nulled `_game` but `MazeGameView` kept its own reference and redrew at 12 fps | Blocker |
| 8 | Arcade income could disagree with the wallet | `TryCollectVisitIncome` credited the wallet before recording state | High |
| 9 | Visitor simulation was not deterministic | `NpcLocomotion.MoveTowards` used `Time.deltaTime`, ignoring the simulated step | Medium |

## Changes

**Menu / build**
- `Editor/MenuSetupTool.cs`: persistent listeners via `UnityEventTools.AddPersistentListener`, added `AudioListener`, menu music asset guard, widened root cleanup so the stale `MainMenu` root is removed.
- `Scripts/Core/MainMenuController.cs`: removed the Return-key shortcut that double-fired with UI submit.
- `Tests/EditMode/MainMenuButtonTests.cs`: six guards — serialized handler, enabled listener state, single controller, exactly one EventSystem/GraphicRaycaster, AudioListener + music clip, clickable size.

**Appearance**
- `Editor/CharacterAppearanceFixTool.cs`: restores textured skin/shoes/hair/suit materials from existing MakeHuman textures, and mounts the hands by measuring the mesh bounds and computing the offset (root ends at z=0.77 so the mesh sits 0.40 m ahead, 0.16 m below the eye).
- `Editor/AppearanceDiagnostics.cs`: read-only renderer/material dump used as evidence.
- `Editor/PolishFixTool.cs`: retired the obsolete skin-tinting pass that would have stripped the restored textures.
- `Scripts/Player/FirstPersonHandsView.cs`: hands visible only while `CarryController.IsCarrying`.
- `Prefabs/Gameplay/Player.prefab` and the two NPC prefabs updated; six new materials.

**Arcade**
- New: `ArcadeState`, `ArcadeService`, `ArcadeAttractMode`, `ArcadeInstallPoint`, `ArcadeVisitor`, `ArcadeTraffic`.
- `ArcadeCabinet`: `SetService`, `IsUnlocked` gating, `Stop()` now clears the view.
- `MazeGameView`: `Hide()` and texture rebinding in `Configure`.
- `EconomyService`: `TryEarnOnce` (caller-supplied id, deduplicated, released on credit failure).
- `Editor/ArcadeBusinessSetupTool.cs`: wires install point, service, attract mode and traffic into `Laundromat.unity` and attaches the hands view.
- Tests: `ArcadeBusinessTests` (9 EditMode), `ArcadeBusinessPlayModeTests` (2 PlayMode).

## Checks performed

| Check | Command | Result |
|---|---|---|
| Menu scene rebuilt | `Unity.exe -batchmode -quit -executeMethod LaundryLegends.Editor.MenuSetupTool.Build` | exit 0, `[MENU] Built: ...` (`TestResults/menu2.log`) |
| Serialized handlers present | grep `m_MethodName` in `MainMenu.unity` | `StartGame`, `QuitGame` present |
| Arcade scene wiring | `-executeMethod LaundryLegends.Editor.ArcadeBusinessSetupTool.Wire` | exit 0, `[ARCADE] Wired: room=True spawner=True screen=True visitor=True` (`TestResults/arcade-wire.log`) |
| Components persisted | GUID grep in `Laundromat.unity` | `ArcadeService`, `ArcadeAttractMode`, `ArcadeTraffic`, `ArcadeInstallPoint` each present once |
| NPC materials restored | `AppearanceDiagnostics.Dump` | every renderer now `tex=yes` (`TestResults/appearance-dump.txt`) |
| Hands mounted | fix log | root z=0.77, mesh at 0.40 m ahead (`TestResults/appearance-fix.txt`) |
| EditMode suite | `-runTests -testPlatform EditMode` | **135 / 135 passed, 0 failed** (`TestResults/edit4.xml`) |
| PlayMode suite | `-runTests -testPlatform PlayMode` | **53 passed, 0 failed, 1 skipped** (`TestResults/play4.xml`) |
| Player build | `-executeMethod LaundryLegends.Editor.BuildGameTool.BuildDirect` | exit 0, 195 MB, `scenes: Bootstrap, MainMenu, Laundromat` (`TestResults/build12.log`) |
| Mouse starts the game | colour-located click on START GAME in the built player | menu cleared (`pink pixels=0`) after one click (`TestResults/playtest6/`) |
| Movement in the built game | held W after starting, frame captured before/after | camera advanced from room centre to the machine wall (`TestResults/playtest-loop/02`, `/03`) |
| Hands hidden when empty | same capture | no hands on screen while not carrying, as required |

## Checks not performed

- **Owner playtest of the arcade payoff.** Required by the Phase 8 acceptance criteria; cannot be self-certified. The scripted pass reached the machine wall and the customer dialogue but did not navigate to the cabinet.
- **Audible check of menu music and attract cue.** The listener and clip are present and asserted, but no one has listened to the built player.
- **Hands while carrying in a real build.** Covered by a PlayMode test; not captured as a screenshot.
- No automated visual-difference test; appearance was verified by inspection of captures and material dumps.

## Known risks / limitations

- Attract-mode emission assumes the cabinet screen material supports `_EmissionColor`; it silently no-ops otherwise.
- `EconomyService._settledIncomes` is in-memory only and not rebuilt from the transaction log, so exactly-once holds in-process but must be revisited when saving lands.
- `ArcadeState.PaidVisits` grows without bound; fine for one machine, needs bounding before save system work.
- The cabinet sits in the main laundromat rather than a separated arcade room; Phase 9's spatial expansion will move it.
- `ProjectSettings` display/quality values were changed by the build tool for fullscreen playtesting and are committed intentionally.

## Recommendation

Close Phase 8 once the owner confirms the payoff, then start Phase 9 (arcade expansion) from `ROADMAP.md`.
