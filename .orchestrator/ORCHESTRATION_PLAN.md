# Orchestration Plan — Phase 4 realism pass

- **Manager:** lead agent (coordination, docs, state, review)
- **Updated:** 2026-11-01
- **Unity editor:** `C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe`
- **Blender:** `C:\Users\Asus\AppData\Local\Programs\BlenderFoundation\Blender-4.5.15\blender-4.5.15-windows-x64\blender.exe`

## Hard rules

1. **Only one Unity batch process at a time.** Subagents must not launch Unity unless
   explicitly told they own the editor slot. Everyone else writes code and reports.
2. **File ownership is exclusive.** Two agents never edit the same file in the same wave.
3. Every workstream ends with a worker report: files, checks, unrun checks, risks.

## Workstreams

### Phase 4 realism pass (closed)

| ID | Workstream | Owner | Unity slot | Status |
| --- | --- | --- | --- | --- |
| W1 | Real humanoid animation, staff visible carrying | ll-unity-dev | **owns now** | code written; verifying |
| W2 | Modeled plastic-wrap bundle (Blender) | ll-unity-dev | no (Blender only) | in progress |
| W3 | Dialogue bank + subtitle-first wiring | ll-unity-dev | no (code only) | in progress |
| W4 | Room realism art brief | ll-designer | no | in progress |
| W5 | Music swap (That Zen Moment + Ripples) | lead/unity-dev | queued after W1 | blocked |
| W6 | Voice rendering (local open-weight TTS) | general agent | no (in progress) | in progress |
| W7 | Docs/state/inventory updates | lead | — | continuous |

### Phase 6 / 7 (active)

| ID | Workstream | Owner | Unity slot | Status |
| --- | --- | --- | --- | --- |
| P6-A | Economy integration, upgrades, purchase + summary HUD (P6) | ll-unity-dev | no | **done** - `a6dec13` |
| P6-C | Operating costs + extra-washer effect | ll-unity-dev | no (code only) | in progress |
| P7-E | SecretRoomController + scene wiring | ll-unity-dev / lead | code now, scene after | in progress |
| P7-F | Street props placed in scene + glossiness fix | lead | Unity | queued |
| P8-A | Maze layout model | ll-unity-dev | no | **done** - `5da8cc9`, 13 tests |
| P8-B | Arcade cabinet asset | ll-unity-dev | Blender | **done** - `5da8cc9`, 892 tris |
| P8-C | Maze-chase gameplay (player, ghosts, scoring) | queued | after P6-C | blocked |
| P8-D | Cabinet RenderTexture + $2 play + scene placement | queued | after P8-C | blocked |

Coordination rule: **exactly one code editor at a time.** Other agents work in
`ArtSource/` (Blender) or `.orchestrator/` (docs) and never touch `Assets/`.

### Phase 5 (closed at `e278ae3`)

| ID | Workstream | Owner | Unity slot | Status |
| --- | --- | --- | --- | --- |
| P5-A | Order model + exactly-once payment core (P5-001, P5-005) | ll-unity-dev | no (code only) | **done** — committed `3498067`, 20 tests |
| P5-B | Character rig rescue (visual debt) | ll-unity-dev | **owns now** | **done** — clothed humanoid character, EditMode 64/64 |
| P5-C | Customer state machine (P5-002) | ll-unity-dev | no | **done** - `e3c1eb6` |
| P5-D | Order handoff + ticket UI (P5-003, P5-004) | ll-unity-dev | no | **done** - `1e61e77` |
| P5-E | Patience, queue, end-to-end (P5-006..008) | ll-unity-dev | no | **done except queue spots** - `f4bb0ef`, `6271712`, `e278ae3` |

Rule: exactly one workstream holds the Unity slot at a time; everyone else writes
code and reports. P5-A and P5-B run concurrently because they do not share files.

## Wave 1 file ownership

- **W1:** `Scripts/NPC/{NpcLocomotion,CarrySocket,CarryPose,LaundryStaff,PickupCustomer}.cs`,
  `Editor/NpcAnimationSetupTool.cs`, `Tests/PlayMode/LaundryLoopTests.cs`,
  `Tests/PlayMode/NpcAnimationTests.cs`
- **W2:** `ArtSource/Blender/wrapped_bundle.py`, `ArtSource/Exports/WrappedBundle.fbx`
- **W3:** `Scripts/NPC/NpcDialogue.cs`, `Scripts/NPC/DialogueBank.cs`,
  `Scripts/UI/SubtitleView.cs` (new), `Tests/EditMode/DialogueBankTests.cs`
- **W4:** `.orchestrator/reports/room-realism-brief.md`

## Wave 2 (after W1 releases the editor slot)

1. W5 music swap (copy tracks, update `MusicSetupTool`, re-run, test).
2. W2 Unity import of the wrapped bundle + `LaundryItem` visual swap.
3. W3 compile + dialogue tests.
4. Combined EditMode/PlayMode + captures.

## Definition of done for this pass

- NPCs animate from real clips; no T-pose, no procedural flailing.
- Staff physically carries laundry between machines, visible in a capture.
- Wrapped bundle is a modeled clear bag, not a cube.
- Music has a 10-minute primary bed and no single-track 84-second loop.
- Dialogue lines exist per trigger with subtitles, voice optional per line.
- All suites green; every unrun check listed honestly.
