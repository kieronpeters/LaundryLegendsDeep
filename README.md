# Laundry Legends

A 3D laundry-business simulation whose long-term secret is a hidden arcade that grows from a locked back room into a second, increasingly valuable business.

**Current status:** Bootstrap complete. No Unity project or gameplay exists yet. The next task is Phase 1, Unity foundation. See [PROJECT_STATE.md](PROJECT_STATE.md).

## Verified development environment

| Item | Verified value |
|---|---|
| OpenCode CLI | `2.0.28` (server reported `2.0.32`) |
| Primary model | `opencode/deepseek-v4.1-flash-free` |
| Git | `2.55.0.windows.3` |
| Unity Hub | `43.5.0` |
| Unity Editor selected | `6000.0.64f1` (Unity 6.0 LTS) |
| Render pipeline | Built-In Render Pipeline |
| Windows standalone module | Installed for `6000.0.64f1` |
| .NET SDK | `10.0.401` |
| Node / npm | `24.21.0` / `11.19.0` |
| Python | `3.15.0` (no Pillow, NumPy, SciPy, soundfile, or trimesh) |
| Git LFS | `3.7.1` |
| Blender / FFmpeg / ImageMagick / GitHub CLI | Not found on `PATH` |

The Unity entitlement file exists, but a batch-mode Unity launch has **not** been verified yet. That is a Phase 1 check.

## Start here

Read in this order:

1. [MASTER_CONTEXT.md](MASTER_CONTEXT.md) — durable vision and operating rules.
2. [PROJECT_STATE.md](PROJECT_STATE.md) — current truth and next action.
3. [ROADMAP.md](ROADMAP.md) — phased plan and acceptance criteria.
4. [AGENT_GUIDE.md](AGENT_GUIDE.md) — roles, permissions, and task contracts.
5. [TESTING_GUIDE.md](TESTING_GUIDE.md) — evidence required before calling work verified.
6. `.orchestrator/state.json` and `.orchestrator/tasks/` — machine-readable queue.

## Commands

The repository provides OpenCode slash commands. Natural-language equivalents are in [MASTER_CONTEXT.md](MASTER_CONTEXT.md#resuming-and-commands).

| Command | Purpose |
|---|---|
| `/start-phase 1` | Start or resume a roadmap phase |
| `/continue-phase` | Continue the active phase |
| `/status` | Show project status and last run |
| `/task-queue` | Show the current task queue |
| `/review-last-run` | Inspect the last run report and diff |
| `/retry-task LL-P1-003` | Retry one failed task safely |
| `/resume` | Resume after interruption or a new session |
| `/run-phases 1 2` | Queue eligible consecutive phases |
| `/stop-safe` | Stop at a safe checkpoint |
| `/prepare-release` | Prepare a release candidate for human review |

## Repository layout

```text
.
├── .opencode/              Project-local OpenCode config, agents, and commands
├── .orchestrator/          Persistent state, tasks, budgets, decisions, run reports
├── ArtSource/              Editable source art (Blender, texture, audio source)
├── Builds/                 Local PC builds (ignored except marker)
├── TestResults/            Local test output (ignored except marker)
├── tools/                  Validation and status scripts
├── AGENTS.md               Persistent instructions loaded by OpenCode
├── PROJECT_STATE.md        Authoritative current status
├── MASTER_CONTEXT.md       Durable vision and resume contract
├── GAME_DESIGN.md          Game design bible
├── ARCHITECTURE.md         Technical architecture and conventions
├── ROADMAP.md              Full phase plan
├── ASSET_INVENTORY.md      Asset registry and pipeline status
├── AGENT_GUIDE.md          Agent roles and working agreements
├── TESTING_GUIDE.md        Verification standards
├── YOUTUBE_LOG.md          Development-series moment log
├── DEVELOPMENT_LOG.md      Chronological development record
└── README.md               This file
```

## Safety

No machine-wide installs, external publishing, destructive Git operations, or unapproved dependency changes. See `.orchestrator/GUARDRAILS.md` and [AGENT_GUIDE.md](AGENT_GUIDE.md#approval-gates).

## First recommended task

**LL-P1-001 — Create and validate the Unity 6.0 LTS project at the repository root using the Built-In Render Pipeline**, preserving all bootstrap files. Then continue Phase 1 in dependency order.
