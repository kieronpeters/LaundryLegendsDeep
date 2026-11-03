# Known Issues — 1.0.0

These are accepted for the 1.0.0 candidate. None blocks the core loop.

## Art / presentation

- First-person hands are simple procedural gloves; no finger animation.
- Arcade cabinet bodies share one base mesh and differ by theme, marquee label and game mode.
- Marquee labels use a legacy TextMesh and flat emissive panels.
- Machine shells can still read bright under the shop lights; exposure was reduced but not art-passed.
- The left storefront window is transparent now; reflections are minimal.

## Systems

- Per-machine arcade visit/income history is not saved; install/active state and upgrades are.
- Order/customer state is intentionally not saved; the queue respawns on load.
- Desktop-MCP window focus is outside the game; capture uses process-explicit tooling.

## Deferred (post-1.0)

- Unreal Engine vertical slice (decision 0016).
- Full ticket-board sync (GitHub Issues adapter exists in `tools/board`).
- Additional authored characters, animations, SFX and cabinet models.
