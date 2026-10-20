# Laundry Legends — Testing Guide

**Principle:** A feature is not verified because it compiles, because a scene opens, or because an agent says it works. Each claim needs evidence proportional to its risk.

## 1. Verification vocabulary

| Term | Meaning |
|---|---|
| Planned | Designed but not built |
| Implemented | Code/asset exists in the repository |
| Compiled | Unity or C# compilation succeeded |
| Automated-tested | A named test ran and passed |
| Playtested | A human played the scenario and observed it |
| Visually verified | A human checked the rendered result/screenshot |
| Build-verified | The behavior works in a built player |
| Verified | Required evidence for the feature level exists |

Always state which level applies.

## 2. Test levels

### Level 0 — Structural and static checks

- `tools/validate-bootstrap.ps1`
- JSON parse and task/state consistency
- Required documentation and report updates
- `git status`, tracked-file audit, ignored-artifact audit
- No secrets, builds, caches, or generated binaries in the diff
- Markdown links and command names

Run at every phase boundary and before commits that touch orchestration.

### Level 1 — Unity compilation

Once the project exists:

```powershell
$process = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' -Wait -PassThru -ArgumentList `
  "-batchmode -quit -nographics -projectPath `"C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends`" -logFile `"TestResults\compile.log`""
$process.ExitCode
```

Record the exit code and scan `TestResults\compile.log` for compiler errors, missing scripts, and import failures. A zero exit code with errors in the log is still a failure until explained.

### Level 2 — EditMode tests

```powershell
$process = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' -Wait -PassThru -ArgumentList `
  "-batchmode -nographics -projectPath `"C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends`" -runTests -testPlatform EditMode -testResults `"TestResults\editmode.xml`" -logFile `"TestResults\editmode.log`""
$process.ExitCode
```

Use for state machines, economy math, save serialization, definitions, and pure logic.

### Level 3 — PlayMode tests

```powershell
$process = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' -Wait -PassThru -ArgumentList `
  "-batchmode -projectPath `"C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends`" -runTests -testPlatform PlayMode -testResults `"TestResults\playmode.xml`" -logFile `"TestResults\playmode.log`""
$process.ExitCode
```

Use for interaction, machine cycles, order flow, customer flow, and scene smoke tests. Do not use `-nographics` unless the test has no rendering dependency.

**Batch-mode timing:** batch PlayMode runs at an uncapped frame rate. A fixed frame count is not a fixed duration. Tests that need movement or elapsed time should accumulate `Time.deltaTime` (for example, a `while (elapsed < 0.1f)` loop) instead of assuming 60 frames per second.

### Current suites (2026-10-20)

| Suite | Tests | Coverage |
|---|---:|---|
| EditMode | 10 | `GameState` money/flags/day rules, build-settings order, and the failure-reporting probe |
| PlayMode | 8 | scene routing, player movement/look, pause, interaction, greybox traversal, wall containment, secret door, scale, and lighting |

Counts are a snapshot; use the XML `test-run` totals as the authoritative result.

### Level 4 — Scene and prefab validation

- Missing scripts/references, null required fields.
- Duplicate/corrupt object IDs.
- Collider coverage on critical paths.
- Player spawn inside geometry.
- Build settings scene list and `Bootstrap` order.
- Missing materials, shaders, audio clips.
- Prefab overrides that break expected behavior.
- Lighting/reflection probe issues.

Prefer an editor validation method or PlayMode smoke test over manual inspection when practical.

### Level 5 — Manual playtest

A human must verify:

- First order end-to-end: arrival → handoff → wash → dry → fold → return → payment.
- Repeatability without console commands or scene reload.
- No soft-lock, item duplication, or lost order.
- Interaction prompt clarity.
- Camera/movement comfort.
- Machine timing/pacing.
- Secret-room discoverability and reveal.
- First arcade machine payoff and income.
- UI readability at target resolution.
- Audio balance and subtitles if voiced.
- Save/quit/continue behavior.

Record date, build/commit, resolution, input method, duration, and observations.

### Level 6 — Build validation

- Build from a clean, known commit.
- Launch the built executable from a separate directory.
- New game, first order, save, quit, relaunch, continue.
- Settings apply and persist.
- No missing content or editor-only references.
- Logs and crash handling are sane.
- Package/build output contains no credentials or development files.

## 3. Evidence storage

- Committed evidence: concise run reports, test XML summaries if small, screenshots only when intentionally part of documentation.
- Local-only evidence: full logs, large screenshots, recordings, builds. Store under `TestResults/` or `Builds/` (ignored).
- Report exact paths, commands, exit codes, and timestamps.
- Never commit secrets, raw machine logs with credentials, or multi-gigabyte artifacts.

## 4. Unity logs

- Editor log: `%LOCALAPPDATA%\Unity\Editor\Editor.log`
- Project logs: `Logs/` (ignored)
- Batch logs: path passed with `-logFile`
- Test results: XML/JSON under `TestResults/` (ignored)
- Scan for `Exception`, `error CS`, `NullReferenceException`, `MissingReferenceException`, `Shader error`, and `Aborting batchmode`.

## 5. Bootstrap checks

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\validate-bootstrap.ps1
```

It verifies required files, JSON parseability, task/state consistency, command presence, Git hygiene, and basic secret-file exclusions. It does **not** validate gameplay.

### Foundation validation commands

These editor methods validate the player prefab and the greybox scene without running the game:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe'
$project = 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends'

$player = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod LaundryLegends.Editor.PlayerSetupTool.ValidatePlayerSetup -logFile `"TestResults\unity-player-validate.log`""
$player.ExitCode

$greybox = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -quit -nographics -projectPath `"$project`" -executeMethod LaundryLegends.Editor.GreyboxSetupTool.ValidateGreyboxRoom -logFile `"TestResults\unity-greybox-validate.log`""
$greybox.ExitCode
```

### Reproducible batch test sequence

Use `Start-Process -Wait` and a single quoted argument string. Passing an array of arguments can split the project path at its spaces.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe'
$project = 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends'

$compile = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -quit -nographics -projectPath `"$project`" -logFile `"TestResults\compile.log`""
$compile.ExitCode

$editMode = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -nographics -projectPath `"$project`" -runTests -testPlatform EditMode -testResults `"TestResults\editmode.xml`" -logFile `"TestResults\editmode.log`""
$editMode.ExitCode

$playMode = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -projectPath `"$project`" -runTests -testPlatform PlayMode -testResults `"TestResults\playmode.xml`" -logFile `"TestResults\playmode.log`""
$playMode.ExitCode
```

Exit `0` means the command and all selected tests passed. Exit `2` means the test run completed with failures. Any other nonzero exit needs log inspection. Then scan all logs:

```powershell
Select-String -Path 'TestResults\*.log' -Pattern 'error CS|Exception|NullReferenceException|MissingReferenceException|Shader error|Aborting batchmode'
```

### Failure-reporting probe

`FailureReportingTests.DeliberateFailureProbe` stays green in normal runs. Set `LL_FAIL_PROBE=1` to make it fail on purpose and prove the runner records a real failure rather than skipping it:

```powershell
$env:LL_FAIL_PROBE = '1'
$probe = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList `
  "-batchmode -nographics -projectPath `"$project`" -runTests -testPlatform EditMode -testFilter FailureReportingTests -testResults `"TestResults\editmode-probe.xml`" -logFile `"TestResults\editmode-probe.log`""
$probe.ExitCode
Remove-Item Env:\LL_FAIL_PROBE
```

Expected evidence: exit `2`, `result="Failed"`, and the message `Deliberate failure probe active: 1`. Run the normal EditMode suite afterward to confirm it is green again.

### Test scaffolding isolation

- `LaundryLegends.Runtime` has no references to test assemblies.
- `LaundryLegends.Tests.EditMode` and `LaundryLegends.Tests.PlayMode` reference only the runtime assembly and Unity Test Runner, and are gated by `UNITY_INCLUDE_TESTS`.
- EditMode tests compile for the Editor platform only.
- Test code lives under `Assets/LaundryLegends/Tests/`, never under `Scripts/`.
- Test XML and logs remain under ignored `TestResults/`; they are evidence, not shipped content.

## 6. Save/load checks

- New profile creation.
- Save → quit → relaunch → continue.
- Autosave at safe points.
- Corrupt JSON fallback.
- Missing field/default handling.
- Future/unknown version handling.
- Money/order/machine/arcade state round-trip.
- No duplicate payment or item duplication after reload.

## 7. Performance checks

After the greybox runs:

- Target resolution and quality preset.
- Frame time on the development machine.
- CPU/GPU bottlenecks during machine cycles and arcade traffic.
- Memory growth over a 15-minute session.
- Load time for the main scene.
- Draw calls/materials for final art.
- No per-frame allocations in hot interaction paths.

Record the hardware and build commit; do not compare measurements across different settings.

## 8. Accessibility checks

- Text size/readability.
- Color-independent state cues.
- Subtitle availability and background.
- Hold vs toggle interactions.
- Camera motion/FOV options.
- Invert Y, sensitivity, volume.
- Keyboard-only menu navigation where feasible.

## 9. Failure and retry policy

- Fix the smallest root cause.
- Re-run the failing check after each fix.
- Maximum 3 repair attempts per check, then stop and record a blocker.
- Do not weaken a test merely to make it pass without documenting why.
- Do not mark a flaky test as passed; record flakiness and investigate.

## 10. Honest reporting

A report must say:

- What ran.
- What did not run.
- What passed.
- What failed.
- What was manually observed versus inferred.
- What remains unverified.

If evidence is missing, the status is unverified, not passed.
