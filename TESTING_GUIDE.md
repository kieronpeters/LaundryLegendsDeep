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
& 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' `
  -batchmode -quit -nographics `
  -projectPath 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends' `
  -logFile 'TestResults\compile.log'
```

Record the exit code and scan `TestResults\compile.log` for compiler errors, missing scripts, and import failures. A zero exit code with errors in the log is still a failure until explained.

### Level 2 — EditMode tests

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends' `
  -runTests -testPlatform EditMode `
  -testResults 'TestResults\editmode.xml' `
  -logFile 'TestResults\editmode.log'
```

Use for state machines, economy math, save serialization, definitions, and pure logic.

### Level 3 — PlayMode tests

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'C:\Users\Asus\Desktop\AI Coding Test\LaundryLegends' `
  -runTests -testPlatform PlayMode `
  -testResults 'TestResults\playmode.xml' `
  -logFile 'TestResults\playmode.log'
```

Use for interaction, machine cycles, order flow, customer flow, and scene smoke tests. Do not use `-nographics` unless the test has no rendering dependency.

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
