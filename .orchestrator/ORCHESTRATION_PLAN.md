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

| ID | Workstream | Owner | Unity slot | Status |
| --- | --- | --- | --- | --- |
| W1 | Real humanoid animation, staff visible carrying | ll-unity-dev | **owns now** | code written; verifying |
| W2 | Modeled plastic-wrap bundle (Blender) | ll-unity-dev | no (Blender only) | in progress |
| W3 | Dialogue bank + subtitle-first wiring | ll-unity-dev | no (code only) | in progress |
| W4 | Room realism art brief | ll-designer | no | in progress |
| W5 | Music swap (That Zen Moment + Ripples) | lead/unity-dev | queued after W1 | blocked |
| W6 | Voice rendering (local open-weight TTS) | general agent | no (in progress) | in progress |
| W7 | Docs/state/inventory updates | lead | — | continuous |

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
