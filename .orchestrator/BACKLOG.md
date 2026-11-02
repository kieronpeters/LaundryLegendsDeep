# Laundry Legends — backlog

Self-filed tickets from the 2026-11-01/02 playtest-fix and visual pass. IDs are stable;
move them to Ready/In Progress in the ticket board when they are picked up.

Columns used: **P0** breaks the core loop, **P1** visible/playtest blocker, **P2** polish, **P3** later.
Type: bug / feature / tech-debt / art / audio / docs.

## Target phases

Tickets close in their target phase. Phase 15 is the final defect drain; it is not a
holding area for work that belongs earlier.

| Target | Tickets | Rule |
|---|---|---|
| Phase 11 - visual art | `LL-F-006`, `LL-B-004`, `LL-A-001`, `LL-A-002` | Art/visual work lands with the art phase. |
| Phase 12 - audio | (none open) | Audio tickets close with the audio phase. |
| Phase 13 - save/progression | `LL-F-005`, `LL-B-005` | Save and board metadata work lands with the save phase. |
| Phase 15 - QA/polish | `LL-B-002`, `LL-B-006`, `LL-F-002`, `LL-F-003`, `LL-T-003`, `LL-T-004`, `LL-T-005` | Must be fixed or explicitly accepted/deferred before Phase 16. |
| Post-1.0 (P3 opportunity) | `LL-F-004`, `LL-F-007` | Park with a written reason; list in release known-issues. |

**Phase 15 gate:** no open P0/P1; every P2 fixed or owner-accepted; every P3 either fixed or explicitly deferred to post-1.0. See `.orchestrator/GUARDRAILS.md` section 13.5.


| ID | Type | Pri | Title | Description | Evidence |
|---|---|---|---|---|---|
| LL-B-001 | bug | P2 | Wrapped bundle reads as a flat sheet | The authored `WrappedBundle.fbx` is a thin sheet; on the folding table it looks like paper, not a wrapped parcel. Replace the visual or rebuild it as a parcel (cloth + film) in Blender. | `TestResults/captures/08-wrapped-bundle.png` |
| LL-B-002 | bug | P2 | Arcade screen dark until a visitor plays | The cabinet's screen material is unlit/blank in captures even when the service is active. Confirm the fight view renders on the screen at idle/attract time and add a capture test. | `TestResults/captures/19-backroom-cabinet-front.png` |
| LL-B-003 | bug | P2 | Door opening still narrows the passage | The opened leaf sits inside the 1.6 m doorway (north hinge). Widen the cut to 2.0 m or tuck the leaf fully against the wall; player reported "still a little blocking". | `SecretRoomController._doorHingeLocal`, `PlaytestFixTool` |
| LL-B-004 | bug | P3 | Poster title text is raster-drawn | `tools/make-posters.ps1` draws titles with GDI+; text is legible but soft at distance. Replace with an authored SDF/text layer or a crisper 1024² atlas. | `Art/Textures/Posters/*` |
| LL-B-005 | bug | P2 | `_tags`/`_work` fields absent on backlog | The board spec asks for tags and work notes; this file has no per-ticket tags/work columns yet. Add them when the Trello board exists. | this file |
| LL-B-006 | bug | P1 | Desktop MCP can focus the wrong window | `focus_window("Laundry Legends")` matched the OpenCode window title; any focus-then-type flow could type into the editor. Use process-explicit focus (`game-vision.ps1`) or rename the session. | `.orchestrator/reports/2026-11-02-tooling-computer-use.md` |
| LL-B-007 | bug | P2 | NPC hand pose looks clawed while carrying | The lowered-arm pose plus the blocky hand mesh reads as claws when holding laundry. Improve the hand pose/weights in the Blender source. | `TestResults/captures/15-customer-outfit.png` |
| LL-F-001 | feature | P2 | Ticket board (Trello or Vikunja) integration | Owner adds cards; agent moves Ready → In Progress → Done and comments report links. Needs API key/token in env and a `tools/board` wrapper. | conversation 2026-11-02 |
| LL-F-002 | feature | P2 | Arcade poster set | Add 2–4 more wordplay posters (`LAUNDRO FIGHTER`, `SUDS OF FURY`) and vary walls. | `HiddenRoomSetupTool.AddPoster` |
| LL-F-003 | feature | P3 | Vending machine / coin op prop | The back room is sparse; add a soda machine and a change machine (Blender). | captures 16/19 |
| LL-F-004 | feature | P3 | Unreal sample parity check | `UnrealSample/` has a ready project + `build_laundromat_scene.py`; install UE5 on a capable machine and run it to compare the scene. | `.orchestrator/reports/2026-11-02-unreal-sample.md` |
| LL-F-005 | feature | P3 | Save/load arcade state | Phase 8 acceptance says arcade state persists once saving exists. Add arcade + room state to the save payload. | ROADMAP Phase 8 |
| LL-F-006 | feature | P2 | Fighter visual upgrade toward SF2 | The cabinet fight is a readable 512x384 pixel homage with blocky fighters. Improve with sprite-style limbs and idle/punch/kick/hit poses, parallax background, portrait boxes, round pips, FIGHT/KO banners, hit sparks and screen shake. True SF2 fidelity needs imported sprite art (AI image credits or a licensed/free pack); the current renderer is procedural. | `FightGameView.cs`, `FightGame.cs`; owner request 2026-11-02 |
| LL-F-007 | feature | P3 | Post-release Unreal vertical slice | After the Unity release is frozen, evaluate an Unreal Engine 5 vertical slice built from the shipped design and assets (FBX, textures, audio, animation, balance) plus the existing `UnrealSample/build_laundromat_scene.py`. This is a remake/sample, not a code port; it needs a decision record, a scoped slice, and a machine that meets UE5 specs. Keep `UnrealSample/` as the starting point. | conversation 2026-11-02; `UnrealSample/README.md` |
| LL-T-001 | tech-debt | P2 | `CharacterAppearanceFixTool` hard-codes outfit names | It was switched to `female_casualsuit01`; make the material mapping data-driven from the FBX renderer names so a re-export cannot silently keep an old suit. | `Editor/CharacterAppearanceFixTool.cs` |
| LL-T-002 | tech-debt | P2 | MakeHuman export needs a one-time bootstrap | `settings.ini` (55 bytes, UTF-8 no BOM) and the plugin copy are required; document it in `ASSET_PIPELINE.md` or auto-create it in `makehuman_npc_run.ps1`. | `ArtSource/Downloads/MakeHuman/userhome` |
| LL-T-003 | tech-debt | P3 | Scene repair tools mutate the scene in place | `PlaytestFixTool`, `VisualPolishTool`, `HiddenRoomSetupTool` are idempotent but write the scene; add a dry-run/validate-only switch and a single "run all" entry point. | `Editor/*FixTool.cs` |
| LL-T-004 | tech-debt | P3 | Batch PlayMode timing assumptions | Tests that use `Time.captureFramerate` run game time faster than wall time; document the measured ratio so future tests choose sane caps. | `TESTING_GUIDE.md` |
| LL-T-005 | tech-debt | P3 | Duplicate/competing wall-gap logic | The door cut, back-room west wall, threshold, and hinge are spread across three tools. Consolidate into one `BackRoomBuilder` with one gap constant. | `HiddenRoomSetupTool`, `PlaytestFixTool` |
| LL-A-001 | art | P2 | First-person hands removed, not replaced | The placeholder hands were removed (weird shapes). If hands are wanted, author proper hands at the camera offset and re-enable `FirstPersonHandsView`. | `Player.prefab` |
| LL-A-002 | art | P3 | Emissive/marquee on the cabinet | The marquee is flat pink; add an emissive texture and a subtle point light when the machine is active. | `Art/Materials/MAT_Cabinet*` |

## Resolved in the 2026-11-02 stabilization pass

| ID | Resolution |
|---|---|
| LL-B-003 | The open door leaf's colliders are disabled while the door is open, so the leaf is decoration and no longer narrows the passage. |
| LL-B-001 | Replaced the flat authored bundle sheet with a procedural cloth parcel in a translucent film bag (`LaundryItem.CreateParcelVisual`); it sits on the folding table as a package. |
| LL-T-002 | `makehuman_npc_run.ps1` now creates the 55-byte `settings.ini` (UTF-8, no BOM) and the plugin folder automatically. |
| LL-T-001 | `CharacterAppearanceFixTool` discovers the suit renderer from the prefab and derives the texture name, so a re-export cannot silently keep an old outfit. |
| LL-B-007 | The clawed hand was the body/pelvis tuck artefact; the masked MakeHuman re-export removed it. Captures show normal hands. |
| LL-A-001 | Decision: first-person hands stay off until a proper hand asset exists (placeholder ones were removed). Tracked here, not in the active set. |
