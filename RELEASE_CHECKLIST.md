# Release Checklist — 1.0.0

## Build

- [ ] `main` is clean at the release commit.
- [ ] `Laundry Legends/Phase 17/Build Release Candidate` completes with 0 errors.
- [ ] `Build/LaundryLegends.exe` and `Build/LaundryLegends_Data/` exist.
- [ ] `Build/release-manifest.json` matches the commit and version.
- [ ] `RELEASE_NOTES.md`, `KNOWN_ISSUES.md`, `CREDITS.md` copied into `Build/`.

## Smoke test (release build)

- [ ] Launches to the main menu.
- [ ] Start Game reaches the laundromat.
- [ ] One order can be taken, processed and paid.
- [ ] `U` purchases, `B` bills, `N` day skip, `M` map, `Esc` pause.
- [ ] Audio sliders change volume.
- [ ] Save, quit, relaunch, continue keeps wallet/day/stock.
- [ ] Hidden room and arcade cabinet can be reached.

## Visual pass

- [ ] Menu storefront/sign readable.
- [ ] Machine wall and clothes read correctly.
- [ ] No magenta/missing materials.
- [ ] HUD does not clip or overlap at 1920x1080.

## Release hygiene

- [ ] No `Library/`, `Logs/`, `TestResults/`, saves or credentials in the shipped folder.
- [ ] Known issues reviewed.
- [ ] Owner approves publication as a separate action.

## Sign-off

- Version: 1.0.0
- Commit: ________
- Build date: ________
- Approved by: ________
