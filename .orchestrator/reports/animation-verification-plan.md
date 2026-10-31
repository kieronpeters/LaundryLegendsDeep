# Animation Verification Plan — Retargeted NPC Locomotion (UAL1)

- **Owner:** QA (`ll-qa`). Verification plan only; no code, scene, prefab, or test was modified.
- **Status:** planned. **No check in this document has been run.** Unity was intentionally not launched because a project import is active in another process.
- **Grounded in:** `TESTING_GUIDE.md`, decision `0009-animation-rig-and-clips.md`, `LaundryStaff.cs`, `AmbientNpc.cs`, `PickupCustomer.cs`, `NpcPose.cs`, `MachineLoadHook.cs`, `FoldingStation.cs`, `CarrySlot.cs`, `LaundryLoopTests.cs`, `AmbientNpcTests.cs`, `Phase3CaptureTests.cs`, `AnimationSetupTool.cs`.
- **Assumed names (from decision 0009, confirm before writing tests):** component `NpcLocomotion` (`Speed` float, `Carrying` bool), `CarrySocket`, controller under `Assets/LaundryLegends/Art/Animations/`.
- **Snapshot taken 2026-10-31 (structural, read-only):** `Assets/LaundryLegends/Art/Animations/UAL1_Standard.fbx.meta` is 59 bytes (GUID only — importer settings not yet persisted); **no** `.controller`, `.anim`, or `.overrideController` asset exists; **no** `NpcLocomotion.cs` / `CarrySocket.cs` exists; NPC prefabs have no `Animator` yet (`NpcAssetSetupTool` logs "no Animator; static fallback documented"). Therefore every check below is currently *unrun/blocked on import completion*, not passing.

## 1. EditMode checks (8)

Suggested file: `Assets/LaundryLegends/Tests/EditMode/NpcAnimationAssetTests.cs`
Conventions: `namespace LaundryLegends.Tests.EditMode`, `public sealed class`, `[Test]`, `UnityEditor`/`UnityEditor.Animations` are allowed here (see `BuildSettingsTests.cs`).

| ID | Check | Assertion |
|---|---|---|
| EM-1 | Animation FBX imports as Humanoid | `ModelImporter.animationType == ModelImporterAnimationType.Human`, `avatarSetup == CreateFromThisModel`, `importAnimation == true`; an `Avatar` sub-asset exists with `isHuman == true && isValid == true` |
| EM-2 | Required clips present | `AssetDatabase.LoadAllAssetsAtPath(UAL1_Standard.fbx)` filtered to non-`__preview__` clips contains a case-insensitive `Idle`, `Walk`, and `PickUp`/`Pickup` clip; log total count (expect 100+, library claims 120+ takes) |
| EM-3 | Clip lengths sane | Idle 0.5–10 s, Walk 0.3–3 s, PickUp 0.4–4 s; no selected clip `length <= 0` |
| EM-4 | Clips have no root motion | For each selected clip: importer `clipAnimations` entry has `lockRootRotation/lockRootPositionX/Y/Z == true` (or clip has no root position curves); no motion clip passes silently |
| EM-5 | Controller asset exists and is wired | Controller loads from the expected path, has exactly the parameters `Speed` (Float) and `Carrying` (Bool), default state is Idle, and at least one Walk state whose motion is a clip from `UAL1_Standard.fbx` (no null motions) |
| EM-6 | NPC prefabs can be driven | Each of `NPC_Customer`, `NPC_Attendant`, `NPC_Regular`: one `Animator` under `Model`, `avatar.isHuman && avatar.isValid`, non-null `runtimeAnimatorController`, `applyRootMotion == false`, and no enabled `NpcPose` alongside a controller |
| EM-7 | Carry socket exists and is a hand | Staff prefab has `CarrySocket` whose transform is a descendant of a humanoid bone (`Animator.GetBoneTransform(HumanBodyBones.LeftHand/RightHand)` chain), no `Collider`/`Rigidbody`, and item parented there stays within 0.6 m of the socket |
| EM-8 | Avatar bone mapping is complete | For each NPC model Avatar: `isValid`, plus non-empty `humanDescription` mapping for Hips, Head, LeftFoot, RightFoot (a missing leg/foot mapping is the usual retarget-collapse cause) |

## 2. PlayMode checks (9)

Suggested files: extend `LaundryLoopTests.cs` (PM-5) and add `Assets/LaundryLegends/Tests/PlayMode/NpcAnimationTests.cs` plus `NpcAnimationCaptureTests.cs` (explicit, PM-9).
Conventions: `namespace LaundryLegends.Tests.PlayMode`, `[UnitySetUp]` loads scene `"Laundromat"` then `yield return null` twice, `Object.FindFirstObjectByType<T>()`, accumulate `Time.deltaTime` (never fixed frame counts), **no `UnityEditor` using** in PlayMode tests.

| ID | Check | Assertion |
|---|---|---|
| PM-1 | Walk state while moving, idle when stopped | After ≥0.5 s of staff movement: `Animator.GetFloat("Speed") > 0.1` and layer-0 state is the walk/blend state (`IsName` or short-name hash); after ≥0.4 s stopped: `Speed < 0.05` and state is Idle |
| PM-2 | Staff visibly carries laundry | During washer→dryer transit the active `LaundryItem` is a child of `CarrySocket` and `renderer.enabled && activeInHierarchy`; after reaching the dryer the item is no longer a socket child and the socket has no `LaundryItem` child |
| PM-3 | No teleporting | Sample staff `position` every frame; assert `maxPerFrameDisplacement <= speed * deltaTime * 1.5 + 0.02f` and no single-frame jump > 0.5 m (also catches root-motion drift) |
| PM-4 | Animation does not fight the state machine | Full loop with `SetDuration(0.1f)` still reaches `MachineState.Running → Complete` and `FoldingStation.IsReady`; `NpcPose.enabled == false` (or absent) while a controller is assigned; `LogAssert.NoUnexpectedReceived()` |
| PM-5 | Laundry loop still completes (regression) | Keep `StaffRunsWasherDryerAndWrapLoop` passing, but raise the wait to ~6 s after item placement (walking now precedes machine work) and assert the same `LaundryItem` instance ends `Wrapped` (no duplication) |
| PM-6 | Clip time actually advances | `animator.GetCurrentAnimatorClipInfo(0).Length > 0`, `animator.speed > 0`, and normalized time increases between samples (proves the Animator evaluates, not just that a controller is assigned) |
| PM-7 | Facing and speed agree with motion | While walking, `Vector3.Dot(transform.forward, flatVelocity.normalized) > 0.9`; record measured m/s and `Speed` once per 0.25 s so foot-slide review has numbers. Assert staff m/s ∈ [0.6, 1.8] |
| PM-8 | Retarget scale/proportions survive | Rendered height (renderer bounds) is 1.5–1.9 m in the walk pose and `CarrySocket` world height is 0.8–1.6 m (the NPC import uses per-archetype `globalScale` 14 / 15.5 / 14) |
| PM-9 | Capture (explicit) | `[UnityTest, Explicit("Evidence capture; run with -testFilter NpcAnimationCaptureTests without -nographics.")]` writes `TestResults/npc-walk.png`, `npc-carry.png`, `npc-staff-dryer.png`; must not run in the default suite |

## 3. Evidence requirements

Run only **after** the external import finishes; never a second Unity batch process concurrently.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe'
$project = 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends'

$clips = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod LaundryLegends.Editor.AnimationSetupTool.ReportClips -logFile `"TestResults\anim-clips.log`""
$clips.ExitCode

$compile = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -quit -nographics -projectPath `"$project`" -logFile `"TestResults\compile.log`""
$compile.ExitCode

$editMode = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -nographics -projectPath `"$project`" -runTests -testPlatform EditMode -testResults `"TestResults\editmode.xml`" -logFile `"TestResults\editmode.log`""
$editMode.ExitCode

$playMode = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -projectPath `"$project`" -runTests -testPlatform PlayMode -testResults `"TestResults\playmode.xml`" -logFile `"TestResults\playmode.log`""
$playMode.ExitCode

$capture = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -projectPath `"$project`" -runTests -testPlatform PlayMode -testFilter LaundryLegends.Tests.PlayMode.NpcAnimationCaptureTests -testResults `"TestResults\anim-capture.xml`" -logFile `"TestResults\anim-capture.log`""
$capture.ExitCode

Select-String -Path 'TestResults\*.log' -Pattern 'error CS|Exception|NullReferenceException|MissingReferenceException|Shader error|Aborting batchmode'
```

- **Keep:** `TestResults/anim-clips.log` (clip inventory incl. `[ANIM] CLIP|name|length`), `compile.log`, `editmode.{log,xml}`, `playmode.{log,xml}`, `anim-capture.{log,xml}`, and the three PNGs.
- **Passed** requires all of: exit `0` for the relevant command, `test-run` totals in the XML showing the named test and zero failures, and a clean log scan. Exit `2` = real failure; any other nonzero = inspect logs before judging.
- **`anim-clips.log` must show** `isHuman=True isValid=True` and one `CLIP|` line per required clip; a 24.9 MB FBX with `Clip count=0` means the import or Humanoid mapping failed, not that clips are absent upstream.
- **Unrun, not passed:** PM-9 captures without a graphics device (`-nographics` crashed with exit `-1073741819` in Phase 2); every "no flailing / looks right" judgment; any Game-view playtest; any built-player check. Agent-inspected screenshots are **visually inspected by agent**, never `Visually verified`.
- **Blocked:** PM-1..PM-8 cannot run meaningfully while the controller asset, `NpcLocomotion`, or `CarrySocket` are absent; report them as `blocked` with the missing-asset snapshot above, not as failing tests.

## 4. Risks and the check that catches each

| # | Risk | Caught by |
|---|---|---|
| 1 | Retarget scale mismatch — NPC FBX import `globalScale` is 14 / 15.5 / 14 against one humanoid library, so retargeted clips can sink, float, or shrink NPCs | EM-8 (valid, mapped avatar) + PM-8 (1.5–1.9 m body, 0.8–1.6 m hand height) + PM-9 capture |
| 2 | Foot sliding — movement stays transform/waypoint-driven while the clip has a fixed stride cadence | PM-7 (measured m/s vs `Speed`, facing dot) and PM-3 (step size); final judgment needs human capture review |
| 3 | Clips carry root motion and fight the waypoint transform (drift, jitter, overshoot) | EM-4 (importer `lockRoot*`) + EM-6 (`applyRootMotion == false`) + PM-3 (per-frame displacement cap) |
| 4 | Avatar mapping failure on an NPC (missing foot/leg bone) — T-pose or collapsed mesh instead of a walk | EM-8 + PM-6 (clip time advances, non-empty clip info) + PM-9 capture |
| 5 | Upper-body carry bias in `LateUpdate` fights the Animator, or two posture systems run at once | PM-4 (`NpcPose` disabled, no unexpected log) + PM-2 + capture |
| 6 | Headless tests silently pass with no Animator (graceful degradation masks a broken prefab) | EM-5/EM-6 asset assertions; PM-1/PM-6 assert the Animator exists and evaluates rather than skipping |

## 5. Honest status

- Checks defined: **8 EditMode, 9 PlayMode**.
- Checks run: **0**. Checks skipped: **all**. Reason: animation FBX import in progress in another process; no controller/script assets exist yet.
- Not a substitute for: human "no flailing" review, Game-view comfort check, or build validation.
