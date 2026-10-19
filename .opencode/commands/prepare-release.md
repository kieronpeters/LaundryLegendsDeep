---
description: Prepare a reproducible release candidate for human review
agent: build
---

Prepare a release candidate.

1. Confirm Phase 16 prerequisites and that all required feature, QA, and polish acceptance criteria have evidence.
2. Read `ROADMAP.md`, `TESTING_GUIDE.md`, `.orchestrator/state.json`, and the release requirements.
3. Run the permitted build, test, startup, scene, save/load, and smoke checks. Record exact commands and results.
4. Verify versioning, build settings, input defaults, resolution options, credits, licenses, and asset inventory accuracy.
5. Capture release notes, known issues, and promotional material requirements.
6. Package the candidate in `Builds/` without committing binaries.
7. Do **not** publish, upload, push, or distribute. Present the candidate and evidence for human approval.

If any required check cannot run, report the release candidate as blocked or conditional, not complete.
