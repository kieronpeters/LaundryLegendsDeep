# Laundry Legends — Development Log

Chronological record of meaningful events, decisions, bugs, fixes, checks, and milestones. A feature is marked **verified** only when the evidence exists.

## 2026-10-19 — Phase 0 bootstrap

**Phase:** 0 — Project bootstrap
**Run:** `2026-10-19-bootstrap`
**Status:** Complete (machine-verified; human review recommended)

### Environment verified

- Workspace was empty before bootstrap; no user files were present.
- OpenCode CLI `2.0.28`; background server reported `2.0.32`.
- Current primary model: `opencode/deepseek-v4.1-flash-free`; provider catalog verified through the model tool.
- Git `2.55.0.windows.3`; Git LFS `3.7.1`; .NET SDK `10.0.401`; Node `24.21.0`; npm `11.19.0`; Python `3.15.0`.
- Unity Hub `43.5.0`.
- Unity editors installed: `6000.0.0f1`, `6000.0.64f1`, `6000.5.7f1`.
- Selected editor: `6000.0.64f1` (Unity 6.0 LTS), with Windows standalone playback engine installed.
- Unity entitlement license file exists; batch-mode launch not yet tested.
- No Blender, FFmpeg, ImageMagick, or GitHub CLI on `PATH`.
- No dedicated image, 3D, audio, or voice-generation tool verified.
- No MCP servers or plugins configured in the global OpenCode config.
- The global OpenCode config defaults to `ollama/qwen3.5:4b`; the project config selects the verified DeepSeek model. No credentials were read or exposed.

### Decisions

- **Unity 6.0 LTS / Built-In Render Pipeline.** Rationale: installed, supported, Windows module present, simpler for a small stylized 3D game, no new pipeline dependency. See `.orchestrator/decisions/0001-unity-version-and-render-pipeline.md`.
- **Legacy Input Manager initially.** Avoid a package change before it is justified.
- **Laundry loop before arcade systems.** The secret arcade remains the central progression hook, but the first playable must be the laundry loop.
- **Single `main` branch, local checkpoints, no remote yet.** Destructive Git operations and pushes are blocked/approval-gated.
- **Project-local OpenCode agents and commands.** Lead is built-in `build`; specialists are `ll-unity-dev`, `ll-designer`, `ll-reviewer`, and `ll-qa`.
- **Persistent state is split between human-readable Markdown and machine-readable JSON.** No database or service.

### Created

- Root documentation: `README.md`, `AGENTS.md`, `PROJECT_STATE.md`, `MASTER_CONTEXT.md`, `GAME_DESIGN.md`, `ARCHITECTURE.md`, `ROADMAP.md`, `ASSET_INVENTORY.md`, `AGENT_GUIDE.md`, `TESTING_GUIDE.md`, `YOUTUBE_LOG.md`, `DEVELOPMENT_LOG.md`.
- `.opencode/opencode.json`, four subagents, and ten slash commands.
- `.orchestrator/` state, budgets, task definitions, decision records, reports, and recovery guide.
- `.gitignore`, `tools/validate-bootstrap.ps1`, `tools/status.ps1`.
- Empty `ArtSource/`, `Builds/`, `TestResults/` directories with markers.

### Checks performed

- `git init -b main`; local `core.autocrlf=false` and `core.safecrlf=false`.
- JSON/frontmatter/command/document structural validation via `tools/validate-bootstrap.ps1` (result recorded in the bootstrap report).
- Git status/ignore audit before checkpoint.
- No Unity compile, Unity test, scene, gameplay, build, or playtest was run because no Unity project exists.

### Not performed / limitations

- Unity batch-mode licensing and project creation.
- Any C# compilation or Unity test.
- Any visual, audio, gameplay, performance, or build verification.
- Any asset creation or integration.
- Any external publishing or remote push.

### Git

- Initial branch: `main`.
- Baseline commit: `b78218cccfeea741d3d227ffa95e679e9ba92a72` (`chore: bootstrap Laundry Legends development system`).
- Bootstrap checkpoint tag: `bootstrap-v0.1` on the state-record commit.
- Post-checkpoint validation passed with exit code 0 at 2026-10-19 20:34 (UTC+08:00); log: `TestResults/bootstrap-validation-2026-10-19.log`. The tag is reachable from `HEAD` and the working tree is clean after the documentation correction commit.

### Independent review

A read-only subagent reviewed the bootstrap before the checkpoint. It found pre-checkpoint completion overclaims, Git-write exposure for built-in subagents, a designer permission that was too broad, a QA task scope mismatch, a test-folder inconsistency, and two documentation contradictions. These were corrected before the checkpoint. The final post-checkpoint validation is recorded in the bootstrap run report.

### Next

`LL-P1-001` — create and validate the Unity 6.0 LTS project at the repository root, preserving bootstrap files, then verify the editor log before relying on batch-mode automation.

## 2026-10-19 — LL-P1-001 Unity project creation

**Phase:** 1 — Unity foundation
**Task:** `LL-P1-001`
**Status:** Complete (license refreshed by the human; batch launch and compile verified)

### What happened

- Initial batch probe failed with exit `198`: the Unity Personal offline license validity window had expired (`LicenseGroupOfflineValidityPeriodIsExpired`, token cache empty).
- Unity Hub was launched; the human signed in and refreshed the license.
- The license probe retry exited `0` and resolved entitlements.
- Unity project created at the repository root with `6000.0.64f1`; all bootstrap files remained intact.
- Added only the pre-approved baseline packages `com.unity.ugui` 2.0.0 (includes TextMeshPro in Unity 6) and `com.unity.test-framework` 1.6.0. Both resolved from the editor's local built-in packages; no remote download.
- Compile smoke check exited `0` with no compiler errors.

### Verified baseline

- `ProjectSettings/ProjectVersion.txt`: `6000.0.64f1 (5360b7cd7953)`.
- Built-In Render Pipeline: `m_CustomRenderPipeline: {fileID: 0}`; no URP/HDRP package.
- Legacy Input Manager: `activeInputHandler: 0`.
- 35 direct package dependencies; 38 resolved packages, all `builtin`.
- `Assets/` exists but is empty.

### Deviations deferred for approval

- Gamma color space (planned: Linear).
- Ultra quality default (planned: one modest preset).
- Full-screen mode (planned: windowed).
- `DefaultCompany` company name and `LaundryLegends` product name.
- Unused `com.unity.multiplayer.center` editor-default package.

### Checks and evidence

- `TestResults/unity-create.log` — exit `0`.
- `TestResults/unity-compile-smoke.log` — exit `0`, no errors.
- `TestResults/unity-license-probe-2026-10-19.log` — evidence of the expired-license failure before refresh.
- `git status` — only `Packages/` and `ProjectSettings/` added plus intended state/doc edits; bootstrap intact.

### Incident

An early `-createProject` attempt passed an unquoted path containing spaces. Unity truncated the project path and wrote a 995-byte log to `C:\Users\Asus\Desktop\AI`. No repository files were affected. The stray file is outside the repository; deletion was blocked by project permissions and requires cleanup approval.

### Not performed

- Unity tests, scene validation, playtest, and player build.

### Next

`LL-P1-002` — review and approve the baseline manifest and project settings.
