# Laundry Legends — backlog

Self-filed tickets from the 2026-11-01/02 playtest-fix and visual pass. IDs are stable;
move them to Ready/In Progress in the ticket board when they are picked up.

Columns used: **P0** breaks the core loop, **P1** visible/playtest blocker, **P2** polish, **P3** later.
Type: bug / feature / tech-debt / art / audio / docs.

## Target phases

Tickets close in their target phase. Phase 16 is the final defect drain; it is not a
holding area for work that belongs earlier.

| Target | Tickets | Rule |
|---|---|---|
| Phase 10 - business operations | `LL-F-008`, `LL-B-010`, `LL-B-012` | Stock, order queue and bills; the order-stall investigation lands here. |
| Phase 11 - characters and animation | `LL-B-009`, `LL-B-013`, `LL-F-009` | NPC route, model and voice cleanup lands here. |
| Phase 12 - visual art | `LL-F-006`, `LL-B-004`, `LL-A-001`, `LL-A-002`, `LL-A-003`, `LL-A-004`, `LL-A-005`, `LL-A-006`, `LL-B-014`, `LL-B-015`, `LL-F-010` | Art/visual work and arcade presentation land with the art phase. |
| Phase 13 - audio | (none open) | Audio tickets close with the audio phase. |
| Phase 14 - save/progression | `LL-F-005`, `LL-B-005` | Save and board metadata work lands with the save phase. |
| Phase 16 - QA/polish | `LL-B-002`, `LL-B-006`, `LL-B-008`, `LL-F-002`, `LL-F-003`, `LL-T-003`, `LL-T-004`, `LL-T-005` | Must be fixed or explicitly accepted/deferred before Phase 17. |
| Post-1.0 (P3 opportunity) | `LL-F-004`, `LL-F-007` | Park with a written reason; list in release known-issues. |

**Phase 16 gate:** no open P0/P1; every P2 fixed or owner-accepted; every P3 either fixed or explicitly deferred to post-1.0. See `.orchestrator/GUARDRAILS.md` section 13.5.


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
| LL-F-008 | feature | P1 | Consumables, order queue and 30-day bills | Phase 10 feature block: detergent/fragrance/plastic shelves (10 each, one unit per purchase), stock consumption with out-of-stock blocking, ten active orders, a ten-slot ready-order shelf, and rent/electricity/water bills every 30 in-game days. | decision 0013; `phase-10.json` |
| LL-T-001 | tech-debt | P2 | `CharacterAppearanceFixTool` hard-codes outfit names | It was switched to `female_casualsuit01`; make the material mapping data-driven from the FBX renderer names so a re-export cannot silently keep an old suit. | `Editor/CharacterAppearanceFixTool.cs` |
| LL-T-002 | tech-debt | P2 | MakeHuman export needs a one-time bootstrap | `settings.ini` (55 bytes, UTF-8 no BOM) and the plugin copy are required; document it in `ASSET_PIPELINE.md` or auto-create it in `makehuman_npc_run.ps1`. | `ArtSource/Downloads/MakeHuman/userhome` |
| LL-T-003 | tech-debt | P3 | Scene repair tools mutate the scene in place | `PlaytestFixTool`, `VisualPolishTool`, `HiddenRoomSetupTool` are idempotent but write the scene; add a dry-run/validate-only switch and a single "run all" entry point. | `Editor/*FixTool.cs` |
| LL-T-004 | tech-debt | P3 | Batch PlayMode timing assumptions | Tests that use `Time.captureFramerate` run game time faster than wall time; document the measured ratio so future tests choose sane caps. | `TESTING_GUIDE.md` |
| LL-T-005 | tech-debt | P3 | Duplicate/competing wall-gap logic | The door cut, back-room west wall, threshold, and hinge are spread across three tools. Consolidate into one `BackRoomBuilder` with one gap constant. | `HiddenRoomSetupTool`, `PlaytestFixTool` |
| LL-A-001 | art | P2 | First-person hands removed, not replaced | The placeholder hands were removed (weird shapes). If hands are wanted, author proper hands at the camera offset and re-enable `FirstPersonHandsView`. | `Player.prefab` |
| LL-A-002 | art | P3 | Emissive/marquee on the cabinet | The marquee is flat pink; add an emissive texture and a subtle point light when the machine is active. | `Art/Materials/MAT_Cabinet*` |
| LL-A-003 | art | P2 | Extra machines reuse the Fighter cabinet | Phase 9 duplicates the shipped cabinet for Racer and Puzzle. They differ in price/income/attract weight only; author distinct cabinet bodies and screen art so the three machines read apart at a glance. | `HiddenRoomSetupTool.CreateExtraMachineSlot`, scene `ArcadeCabinet_Racer/Puzzle` |
| LL-A-004 | art | P2 | Arcade upgrade decor is placeholder primitives | The neon sign, speakers and rug are boxes. Replace with authored props and a lit marquee once art is available. | `HiddenRoomSetupTool.ConfigurePhase9`, `Decor_NeonSign`, `Decor_Speakers`, `Decor_ArcadeRug` |
| LL-A-005 | art | P2 | Replace the washer/dryer blocks with real appliance models | The eight machines still read as square blocks. Replace all eight with actual front-load washer and dryer models: rounded body, recessed door glass, control panel, detergent drawer, drum, feet, hoses and material wear. Keep the existing load/dry hooks and drum anchors. | review 2026-11-03; `TestResults/captures/03-machine-wall.png`; `ApplianceHD.blend` |
| LL-A-006 | art | P2 | Arcade cabinet side art and marquee | The cabinet is plain on the sides and has no side logo. Add side decals, a lit marquee, a control-panel overlay and a screen bezel so it reads as a real cabinet from every angle. | review 2026-11-03; `ArcadeCabinet.fbx` |
| LL-F-010 | feature | P2 | Per-machine arcade game variety | Racer and Puzzle currently launch the same fight game. Give at least one extra machine a distinct mode (the existing maze game is a candidate) or a visibly different opponent/palette so the catalogue is not cosmetic. | review 2026-11-03; `ArcadeCabinet`, `FightSession`, `MazeGame` |
| LL-B-011 | bug | P3 | Purchase menu shows two Racer and two Puzzle rows | The initial and bay anchors share definition names, so the U list shows duplicate labels. Append "(bay)" or a slot number to the bay entries. | review 2026-11-03; `PurchaseMenuView.Rebuild` |
| LL-B-012 | bug | P1 | Orders stop processing when entering the back room | Owner observed the order loop stall after walking from the main shop into the hidden room. Needs a runtime audit of the customer spawner, staff, drop-off rack and any room/culling state that changes on entry. | review 2026-11-03 |
| LL-B-013 | bug | P2 | Phone regular stands in storefront debris | The ambient `PhoneRegular` is placed in the window debris at `(5.6, 0, -5.2)`. Move him into the waiting area or remove the placement. | review 2026-11-03; `NpcSceneSetupTool` |
| LL-B-014 | bug | P2 | Dryer drum clothes do not tumble | The drum face spins, but the visible clothes stay static. Make the drum's clothes the visible spinner, or parent the loaded item to the drum while running. | review 2026-11-03; `MachineLoadHook`, `MachineSetupTool` |
| LL-B-015 | bug | P2 | Expansion carpet is a small rug | The carpet upgrade covers only a 2.6x1.6 m rug on the white bay floor. Replace it with a full-bay patterned carpet in a bar/casino style, distinct from the back-room carpet, gated to the expansion. | review 2026-11-03; `HiddenRoomSetupTool` |
| LL-B-009 | bug | P2 | Ambient customer stands on the counter | The second woman is the ambient `StreetCustomer`; her route's counter waypoint sits on the counter and she has no order slot by design, so she looks stuck and cannot hand off her parcel. Move the waypoint into the waiting area and stop her carrying the parcel to the counter, or remove her until the queue rework. | review 2026-11-03; `NpcSceneSetupTool` waypoints |
| LL-B-010 | bug | P2 | Staff reuses the leftmost washer/dryer | `LaundryStaff` sorts hooks by name and picks the first empty one, so it reuses `Machine_1/2` whenever they are free. It only moves to the others if the preferred unit is occupied. Add least-recently-used or round-robin selection so all eight are exercised. | review 2026-11-03; `LaundryStaff.FindMachine` |
| LL-F-009 | feature | P2 | Male customer voice reassignment | The hidden-room visitor was removed, the staff commentary was silenced, and the order customer now uses the male model. Remaining: move the staff's male voice clips to that customer (or copy them to the DropOff archetype) without restoring the staff's commentary. | review 2026-11-03 |
| LL-B-008 | bug | P2 | Phase 9 bay/kiosk/HUD visual review | No captures were taken of the expansion bay, upgrade kiosks or IMGUI arcade panel. Verify lighting, readability, overlap with the order ticket at 1920x1080, and that the open bay reads as a space. | Phase 9 report; `ArcadePanelView`, `ExpansionBay` |

## Phase 10-12 closeout (2026-11-04, branch `phase-10-12`)

Resolved in this branch, with evidence in `TestResults/phase12/` and the final suites:

| Ticket | Outcome | Evidence |
|---|---|---|
| LL-F-008 | Consumables, ten-order queue, ten-slot rack and 30-day bills implemented and wired. | `BusinessSetupTool.ValidateBusinessSystems`; `BusinessOperationsTests`; `BusinessSceneTests` |
| LL-B-012 | Order stall could not be reproduced after the queue rework; the diagnostic order completes with the player in the back room. Shortened per-slot machine reuse and stock retry removed the stuck states. | `OrderStallDiagnosticTests`; `TwoOrderPlayModeTests` |
| LL-B-013 | Phone regular moved to `(6.0, 0, -4.2)`, clear of both chair sets; legacy disabled pickup component removed. | `12-waiting-area.png`; `Phase12SceneTests.WaitingAreaNpcsAreClearOfFurniture` |
| LL-B-009 | Ambient route now ends at `Waypoint_Waiting (4.3, 0, -4.2)`; the counter waypoint is gone and each leg has a 20 s timeout. | `AmbientNpcTests`; scene waypoint check |
| LL-F-009 | Order customer uses the male DropOff/Norman voice and subtitles; the staff stays silent (the "staff male clips" request was adapted because the only staff clips are female Cori). | `NPC_OrderCustomer.prefab`; `menu-b.png` subtitle |
| LL-A-005 | All eight machines replaced with the 13.3k/14.1k-tri detailed washer and dryer FBX models; 100 material slots converted to URP/Lit. | `machine-wall-4.png`; `ApplianceIntegrationTool.Validate`; `Phase12SceneTests` |
| LL-B-014 | Drum clothes now sit in the detailed glass door and the drum assembly rotates while a cycle runs; the synthetic dark face was removed. | `Phase12SceneTests.EveryMachineHasDetailedModelAndTurnsItsDrum` (angle > 45°) |
| LL-B-015 | Full-bay 6x4 m casino-lattice carpet replaces the small rug; still gated to the expansion + carpet purchase and parented inside the bay. | `12-bay-carpet.png`, `12-bay-entry.png` |
| LL-A-006 | Double-sided Racer Riot / Spin Cycle Showdown art added to every cabinet side; the bay poster moved off the expansion seal. | `12-cabinet-side.png`, `12-bay-poster.png` |
| LL-B-010 | Staff now cycles through washers and dryers with a per-kind cursor instead of always reusing the leftmost unit. | `LaundryStaff.FindMachine`; `LaundryLoopTests` |
| LL-B-011 | Duplicate U-menu rows fixed: repeated machine names get a `(#slot)` suffix. | `12/menu-u-2.png`; `ArcadeExpansionPlayModeTests` uniqueness assert |

Partially resolved (ticket stays open):

- LL-A-004: the carpet is now authored art; the neon sign and speakers are still primitives.
- LL-B-008: bay captures now exist (`12-bay-entry`, `12-bay-carpet`, `12-bay-poster`, `12-cabinet-side`); the IMGUI HUD overlap at 1920x1080 still needs a layout pass.

Still open: LL-F-010 (per-machine game variety - the maze game exists but is not wired to a cabinet), LL-B-003, LL-B-002, LL-B-007, LL-A-001, LL-A-002, LL-A-003.

## Open-issue pass (2026-11-04, branch `phase-10-12`)

Resolved in this pass:

| Ticket | Outcome | Evidence |
|---|---|---|
| LL-F-010 | Puzzle cabinets now launch the maze game; fight cabinets keep per-machine opponents (`BULL`/`TURBO`) and theme colours. | `arcade-maze.png`, `arcade-fight-turbo.png`; `ArcadeGameVarietyTests` |
| LL-F-002 | Added `LAUNDRO FIGHTER` and `SUDS OF FURY` posters; placed in the back room and bay. | `Art/Textures/Posters/`; back-room build log |
| LL-F-003 | Procedural soda and change machines with lit panels, can grid, coin slot and dispenser. | `12-vending.png` |
| LL-B-002 | Idle cabinets now draw a generated attract image and pulse emission instead of staying black. | `ArcadeGameVarietyTests.IdleCabinetShowsLitAttractImage` |
| LL-B-004 | Posters re-rendered at 2x (1024x1536) with anti-aliased text. | `tools/make-posters.ps1` |
| LL-B-008 | Business HUD height raised and made collision-free against the order ticket/arcade panel from 1280 to 1920 wide; bay, kiosk, cabinet and decor captures added. | `hud-layout.png`, `12-bay-*`, `12-cabinet-*`, `12-vending.png` |
| LL-F-006 | Fight screen gained a parallax skyline, portrait boxes, hit sparks, screen shake and a complete pixel font (V and digits were missing). | `arcade-fight-turbo.png` |
| LL-A-002 | Marquee material is emissive per machine and a point light follows the slot power state. | `12-cabinet-front.png`; `ArcadeMarqueeLight` |
| LL-A-003 | Cabinets now differ in body tint, control-panel/trim colour, marquee and opponent, not just price. | `12-cabinet-front.png`; definition themes |
| LL-A-004 | Neon sign rebuilt from tubes/connectors/glow; speakers gained cones and tweeters. | `12-cabinet-front.png` background; `HiddenRoomSetupTool` |
| LL-T-004 | Documented the `Time.captureFramerate` wall/game ratio and test rules. | `TESTING_GUIDE.md` |

Also landed for Phase 11: `Load`, `Fold` and `Handoff` animator states driven by `NpcLocomotion.PlayLoad/PlayFold/PlayHandoff`, called from the staff take/load/fold/handoff steps using the existing CC0 `Interact` and `PickUp_Table` takes.

Still open with reasons:

- `LL-B-005`, `LL-F-001`: require a real ticket-board service/API token; the backlog format is unchanged until then.
- `LL-B-006`: the desktop MCP focus bug lives outside the repository; all capture/focus flows now use process-explicit `game-vision.ps1`, so the repo risk is mitigated.
- `LL-F-004`, `LL-F-007`: need Unreal Engine 5 hardware/install and a post-1.0 decision.
- `LL-F-005`: save/load is Phase 14 work.
- `LL-T-003`, `LL-T-005`: P3 refactors deferred to avoid destabilizing the shared scene tools this late in the branch.
- `LL-A-001`: first-person hands stay off until a proper hand asset exists (existing decision).

## Resolved in the 2026-11-02 stabilization pass

| ID | Resolution |
|---|---|
| LL-B-003 | The open door leaf's colliders are disabled while the door is open, so the leaf is decoration and no longer narrows the passage. |
| LL-B-001 | Replaced the flat authored bundle sheet with a procedural cloth parcel in a translucent film bag (`LaundryItem.CreateParcelVisual`); it sits on the folding table as a package. |
| LL-T-002 | `makehuman_npc_run.ps1` now creates the 55-byte `settings.ini` (UTF-8, no BOM) and the plugin folder automatically. |
| LL-T-001 | `CharacterAppearanceFixTool` discovers the suit renderer from the prefab and derives the texture name, so a re-export cannot silently keep an old outfit. |
| LL-B-007 | The clawed hand was the body/pelvis tuck artefact; the masked MakeHuman re-export removed it. Captures show normal hands. |
| LL-A-001 | Decision: first-person hands stay off until a proper hand asset exists (placeholder ones were removed). Tracked here, not in the active set. |
