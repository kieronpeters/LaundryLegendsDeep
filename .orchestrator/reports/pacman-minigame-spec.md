# Maze Chase Minigame — Design Spec (arcade cabinet, Phase 7/8)

- **Status:** proposal. Nothing in this document is implemented; no asset exists.
- **Written:** 2026-11-01 (ll-designer). Documentation only — no code, Unity, Blender, package, or git action.
- **Read for this spec:** `GAME_DESIGN.md` §6/§9 (arcade economics, `$350` machine, `$2`/play, 1–3 plays/visitor),
  `PROJECT_STATE.md`, `.orchestrator/reports/room-realism-brief.md`.
  `bali-street-brief.md` does **not** exist in the repo; the closest existing brief is `room-realism-brief.md`.
- **Verified by inspection:** `EconomyService.cs` (`TrySpend`/`TryEarn`/`TransactionRecorded`/`GetSummary`),
  `GameState.cs` (no public money setter), `PlayerInteractor.cs` (`E`, 3 m ray, `IInteractable`),
  `PauseController.cs` (`Esc` → `Time.timeScale = 0`), `CarryController` (`Q` drop).
- **Not verified:** anything visual, performance on the target machine, feel, or fun.
- **IP note:** *Pac-Man* is a Bandai Namco trademark and its layout, sprites, and audio are protected. This document
  uses "maze chase" internally. **Ship an original game**: original name, original sprites, original maze, original
  sounds. The maze below is authored for this project and is not a copy of the 1980 layout.

## 1. Scope for v1

**In scope:** one maze, one player, four ghosts, dots, four power pellets, score, 3 lives, ghost house and tunnel,
scatter/chase/frightened/eaten ghost states, level clear and level loop, a paid single play, and a static
"INSERT COIN"/title state on the cabinet screen.

**Out of scope (do not build in v1):** fruit/bonus items, intermission cutscenes, any audio, leaderboards or initials
entry, multiple mazes or maze rotation, cruise-Elroy speed boost, attract-mode demo playback, NPC-visible play
animation, multiplayer, high-score persistence to save (fields reserved, not written), gamepad input,
in-cabinet settings, prize tickets, and any reward for score beyond the extra life at 10,000.

## 2. Maze layout

**Decision: 21 columns × 21 rows, 21 px-as-tiles, 4:3-native.** Reasons: odd width gives a true mirror axis with no
seam work; 441 tiles is ~half the original’s 868, so one developer can author and debug it; it maps exactly to a
512×384 render target at 16 px/tile (336×336 maze + 88 px side bands + 24 px top/bottom bands) with no letterboxing;
and a 21-tile maze crossed in ~2.6 s at the speeds in §3 feels like the original without needing a larger maze.
The layout is generated on a 7×7 corridor lattice (lanes at cols/rows 1,4,7,10,13,16,19) with 2×2 pillar blocks,
then sculpted: merged pillars, a 3×3 ghost house, holes in the top/bottom bands, and a tunnel row.

```
#####################   0
#o.................o#   1
#.##.####.#.####.##.#   2
#.##.####.#.####.##.#   3
#...................#   4
#.##.#####.#####.##.#   5
#.##.#####.#####.##.#   6
#........   ........#   7
#.#####.##=##.#####.#   8
#.#####.#ggg#.#####.#   9
T.......#ggg#.......T  10
#.#####.#ggg#.#####.#  11
#.#####.#####.#####.#  12
#...................#  13
#.##.####.#.####.##.#  14
#.##.####.#.####.##.#  15
#.........P.........#  16
#.##.##.##.##.##.##.#  17
#.##.##.##.##.##.##.#  18
#o.................o#  19
#####################  20
```

Legend: `#` wall (blocks player and ghosts) · `.` dot, 10 pts · `o` power pellet, 50 pts · ` ` empty corridor (no
pellet) · `P` player spawn (col 10, row 16) · `=` ghost-house door (ghosts only, player blocked) · `g` ghost-house
floor (9 tiles, no pellets) · `T` tunnel mouth (col 0 and col 20 on row 10; walking off one edge wraps to the other).
Columns 0–20, rows 0–20, row 0 at the top. Every row is exactly 21 characters.

**Layout metrics — machine-verified on 2026-11-01** (ad-hoc BFS script over the ASCII above, run in temp storage; no
repo file was added): width violations 0 · pixel-mirror violations 0 · dots 184 · power pellets 4 · total edible 188 ·
player-walkable tiles 194 · BFS from `P` reached 194 · unreachable 0 · dead ends (walkable tile with ≤1 walkable
neighbour) 0 · ghost-house tiles 9 · doors 1. The four power pellets sit at the four ring corners (1,1), (19,1),
(1,19), (19,19). The 3×3 house (cols 9–11, rows 9–11) with its door at (10,8) deliberately severs the centre lane,
which is why the tunnel row and the lane below the house are the two long escape routes.

Rejected alternative: the 28×31 original proportions read better on a portrait CRT but cost ~2× the authoring,
cannot be validated by a small team quickly, and force a 3:4 render target that the flat cabinet quad handles badly.

## 3. Rules

**Grid and movement.** Tile units are 1.0 world unit in maze space; +Y is up. Four directions only, no diagonals.
Positions are floats; an entity commits a direction at tile centres and interpolates between them. Reversing is
allowed for the player at any time; ghosts cannot reverse at a junction unless a mode change forces it.

| Entity state | Speed (tiles/s) | Note |
|---|---|---|
| Player | 8.0 | constant; no speed loss while eating |
| Ghost, normal | 7.2 | 90 % of player |
| Ghost, frightened | 4.8 | 60 % |
| Ghost, eaten (eyes) | 16.0 | 200 % while returning |
| Ghost, any, inside tunnel tiles (0,10)/(20,10) | 4.0 | 50 % |

Level scaling (per level cleared): ghost speed +5 %, capped at +25 %; frightened duration −0.5 s, floor 3.0 s.

**Cornering.** The player input buffer holds the last non-opposite direction for 0.40 s. A perpendicular turn is
committed when the entity is within 0.30 tiles of the next centre and the tile beyond is walkable; otherwise the turn
waits for the centre. If neither the buffered nor the current direction is walkable at a centre, the player stops on
that centre until input arrives. This one rule is what makes the cabinet feel responsive; do not simplify it to
"turn only at exact centres".

**Scoring.** Dot 10 · power pellet 50 · ghost 200, then 400, 800, 1600 within one power pellet · extra life at 10,000
(once per play) · level clear 200 · no other bonuses. Score is per play and resets on a new paid play.

**Power pellets.** Duration 6.0 s at level 1, flashing in the last 2.0 s. Eating one sets all non-eaten ghosts to
frightened, forces them to reverse, and resets the ghost chain to 200. Frightened ghosts choose a random legal
non-reverse direction at each junction (seeded `System.Random`, seed stored on the play for tests). Player/ghost
collision uses a 0.5-tile centre distance; frightened → ghost becomes eaten; otherwise the player dies.

**Ghost states.** `InHouse → Leaving → Scatter ⇄ Chase → Frightened → Eaten → Returning → InHouse`. The door tile is
walkable only for `Leaving` and `Returning`. Eaten ghosts target the house centre (10,10), descend, wait 1.0 s, then
leave. A mode change reverses every ghost except eaten/frightened ones.

**Modes.** Scatter 7 s → Chase 20 s → Scatter 7 s → Chase 20 s → Scatter 5 s → Chase 20 s → Chase forever. Timers run
per level and pause while a ghost is frightened or the player is dead.

**Junction choice (chase and scatter).** At each centre, score every legal direction except the one just travelled,
pick the minimum squared Euclidean distance from the *candidate tile* to the ghost's target tile, break ties in the
fixed order Up, Left, Down, Right. Targets:

| Ghost | Chase target | Scatter corner |
|---|---|---|
| Blinky (red) | player tile | (19,1) |
| Pinky (pink) | player tile + 4 × player facing | (1,1) |
| Inky (cyan) | 2 × (2-ahead-of-player) − Blinky tile | (19,19) |
| Clyde (orange) | player tile if distance > 8, else scatter corner | (1,19) |

**Ghost house release.** Blinky starts at (10,7) outside the house. Pinky, Inky, Clyde start at (10,10), (9,10),
(11,10) and leave at 1 s, 4 s, 8 s after the play starts and after each respawn.

**Lives.** Start 3, maximum 5. A death freezes 1.2 s, then respawns the player at (10,16) with 1.0 s invulnerability,
resets ghosts to the house and restarts ghost timers; dots already eaten stay eaten. At 0 lives the play ends.

**Level clear.** Last edible tile eaten → 2.0 s maze flash, +200, level++, dots and power pellets reset, ghosts return
to the house, player returns to (10,16). Win condition for a play is "clear as many levels as you survive"; the play
ends only on Game Over or quit.

## 4. Controls

| Action | Input | Behaviour |
|---|---|---|
| Start / insert coin | `E` (the `PlayerInteractor` key, not hard-coded) | Charges `$2` and starts a play (§6) |
| Move | `WASD` or arrow keys | Buffered, see cornering |
| Pause the minigame | `Esc` | Freezes the minigame, shows "PAUSED — ESC resume / Q quit" on the cabinet screen |
| Quit to the room | `Q` | Ends the play, no refund, control returns to the player |

While the cabinet is active: `FirstPersonController`, `PlayerInteractor` and `CarryController` are disabled (otherwise
`Q` would also drop laundry), the player transform is parked at the cabinet's `ViewAnchor` (0.55 m from the glass,
1.35 m eye height) and the cursor stays locked. On quit, the player is restored to the pre-play position/rotation and
all three components are re-enabled.

**Does the laundry sim keep running?** **Decision to confirm A — recommended: yes, it keeps running.** Washers,
dryers, order timers and customer patience use real time. Pausing would be free wash/dry time and would break the
economy. The consequence — you can lose a customer while playing — is intentional and is why `Q` exists.

**Decision to confirm B — `Esc` conflict.** `PauseController` currently consumes `Esc` unconditionally and sets
`Time.timeScale = 0`. Proposal: `PauseController` gains a suppressed flag that the cabinet sets while a play is
active, so `Esc` pauses the minigame instead. This is a code change to an existing, tested component and needs the
Unity owner’s sign-off.

## 5. In-world presentation

**Render target.** `RT_ArcadeMaze` = 512×384, `ARGB32`, depth 16, `antiAliasing = 1`, `filterMode = Point`,
`useMipMap = false`, no compression. 16 px per maze tile → 336×336 maze centred at pixel (88, 24), 88 px side bands
for score/lives/level, 24 px bands top and bottom. The RT is authored once as an asset and referenced by a serialized
field; never created per play.

**Camera.** `ArcadeMazeCamera`: orthographic, `orthographicSize = 12` (24 tiles tall × 32 tiles wide at 4:3), solid
near-black clear colour, no lights, no post-processing. It lives under a root parked at `(0, −500, 0)`, renders only
the `ArcadeScreen` layer, and the main camera’s culling mask excludes `ArcadeScreen`. Nothing in the room can leak
into the shot and the maze can never be seen from the room.

**Cabinet geometry (proposed).** Upright cabinet 0.68 W × 0.78 D × 1.75 H m, control panel at 1.00 m, screen recess
0.42 × 0.32 m with the screen quad **0.34 × 0.255 m (4:3)** inset 0.02 m, tilted 8° back from vertical, centre at
1.32 m. A 12 mm black bezel lip overlaps the screen on all four edges, so any aspect mismatch is hidden by plastic
rather than letterboxed. Marquee 0.42 × 0.10 m, emissive, above the screen; coin slot and joystick are static props.
The bezel and marquee are what make the screen read as a screen instead of a glowing rectangle.

**Readability at 1.5 m.** One maze tile is 10.6 mm on the glass, subtending ~24 arcmin at 1.5 m; a 6 px dot is ~4 mm,
~9 arcmin. Both clear the ~1 arcmin acuity limit by a wide margin, so 512×384 is sufficient and a higher RT gains
nothing but fill cost. Constraints: no texture filtering, no bloom, no screen grime over the play area — keep glass
transmittance ≤ 0.06 and put reflections on the bezel, not the aperture. Score text ≥ 8 px cap height, white on
near-black.

| Asset | Spec | Poly/tri budget | Path / name | Licensing |
|---|---|---|---|---|
| Cabinet body | 0.68×0.78×1.75 m, bevelled box, coin door, T-moulding | ≤ 1,200 | `Art/Models/Arcade/ArcadeCabinet_Maze01.fbx`, `Prefabs/Arcade/ArcadeCabinet_Maze01.prefab` | Original, made in Blender (approved pipeline) |
| Screen quad + bezel | 0.34×0.255 m quad; bezel 0.40×0.34 m | 2 + 120 | `MAT_Arcade_Screen`, `MAT_Arcade_Bezel` | Original |
| Marquee | 0.42×0.10 m, emission 1.8, 256×64 texture | 2 | `Art/Textures/Arcade/marquee_maze01.png` | Original lettering; no franchise marks |
| Maze tiles | 1×1 unit quads, flat colour + 1 px inner highlight | ~250 quads / 500 tri | generated mesh at runtime | Original |
| Actors | Player disc (mouth as a 2-part mesh), 4 ghosts (domed body, 3-tone eyes) | ≤ 40 tri each | code-generated placeholder → later `Art/Sprites/Arcade/atlas.png` 256×256, 16×16 tiles | Original; if sourced, CC0 only |
| Dots/power pellets | instanced 2-tri quads, 6 px and 12 px | 2 each | procedural material | Original |

Placeholder rule: v1 uses runtime-generated meshes/materials and a code-written 16×16 atlas via `Texture2D`. No image
tool exists in this environment, so **no generated image or "prompt" may be recorded as an asset.** Any later atlas
needs a source, author, and licence row in `ASSET_INVENTORY.md` before import.

## 6. Integration contract

Proposed ownership (design-level names; the Unity owner confirms files before implementation):
`Scripts/Arcade/MazeLayout.cs` (ASCII constants + parser), `ArcadeMazeState.cs` (pure C#, no `UnityEngine` types),
`ArcadeMazeView.cs` (renders state into the RT scene), `ArcadeCabinet.cs` (`IInteractable` + economy + events),
`ArcadeScreenCamera.cs`, plus EditMode `MazeLayoutTests`/`ArcadeMazeStateTests` and PlayMode `ArcadeCabinetTests`.

```csharp
public enum ArcadeEndReason { GameOver, Quit, SceneExit }
public readonly struct ArcadePlayResult {
    public string PlayId { get; }  public string MachineId { get; }
    public int Score { get; } public int Level { get; }
    public int DotsEaten { get; } public int GhostsEaten { get; }
    public float DurationSeconds { get; } public ArcadeEndReason Reason { get; }
}

public sealed class ArcadeCabinet : MonoBehaviour, IInteractable {
    public string MachineId { get; }            // stable id, e.g. "arcade.maze01"
    public int PlayCost => 2;
    public bool IsPlaying { get; }
    public string InteractionPrompt { get; }    // "Play maze game ($2)" | "Maze game — need $2"
    public bool CanInteract(GameObject interactor); // true when not playing (so the prompt can explain a shortfall)
    public void Interact(GameObject interactor);    // = TryStartPlay()
    public bool TryStartPlay();
    public void QuitPlay();
    public event Action<ArcadePlayResult> PlayStarted;
    public event Action<ArcadePlayResult> PlayConsumed; // exactly once per paid play
    public event Action<int> ScoreChanged;
}
```

**Charge path (exactly once).** `TryStartPlay()` returns false if `IsPlaying`, if `_economy` is null (fail closed — no
free play), or if `_economy.Balance < PlayCost`. Otherwise it calls
`_economy.TrySpend(PlayCost, $"Arcade play: {MachineId}", Time.time)` **before** any state change; only on `true`
does it create a `PlayId` (`Guid`), raise `PlayStarted`, and begin the play. Guarantees: the balance check and the
spend are the same call; a failed spend leaves zero state; a second start while playing is rejected by `IsPlaying`;
`QuitPlay()` and scene unload both route through one `EndPlay(reason)` that raises `PlayConsumed` once and clears
`PlayId`; a HashSet of consumed `PlayId`s makes duplicate counting impossible. Retrying after Game Over is a new paid
play by design — there is no free replay.

**Daily summary.** Player plays are expenses (`reason` prefix `Arcade play: `), NPC plays are income
(`TryEarn(2, "Arcade play: " + MachineId, t)`), both already visible in `EconomyService.GetSummary()` by kind. Play
*count* is not in `DailySummary`, so a small `ArcadeLedger` subscribes to `PlayConsumed` and exposes `PlaysToday`,
`PlayerPlays`, `HighestScore`, which the day-end report renders next to `GetSummary()`.
**Decision to confirm C:** whether owner-play should be charged at all (an owner playing their own machine is arguably
free) and **Decision to confirm D:** side ledger vs. extending `DailySummary` (recommended: side ledger, it avoids
touching a tested type). **Decision to confirm E:** whether a paid session needs a time cap (`MaxSessionSeconds`,
0 = unlimited; recommended 0 for v1).

## 7. Testable acceptance criteria

Pure logic (EditMode, no scene, no frame timing):
1. ASCII parse: 21 rows, every row 21 chars, single `P`, single `=`, exactly 9 `g`, both `T` on row 10.
2. Mirror symmetry: for every (x,y), tile at (20−x, y) has the same walkability class and the same pellet type.
3. Connectivity: BFS from `P` over walkable tiles reaches all 194 walkable tiles and all 188 edible tiles;
   no pellet is unreachable, with and without the row-10 tunnel wrap.
4. Dead ends: every walkable tile that is not in the house and not a tunnel mouth has ≥2 walkable neighbours.
5. Counts: exactly 184 dots and 4 power pellets at the four coordinates in §2.
6. Movement: a buffered perpendicular input within 0.30 tiles of a centre turns; a buffered input aimed at a wall is
   consumed without moving; the player stops at a centre with no walkable exit and resumes on valid input.
7. Ghost AI: at a junction a ghost never reverses; the chosen direction minimises squared distance to its target;
   ties resolve Up, Left, Down, Right; each of the four target formulas returns the documented tile for a fixed
   player tile/facing.
8. Ghost states: `Scatter→Chase` at 7 s, `Chase→Scatter` at 27 s; power pellet sets frightened and reverses ghosts;
   frightened ends at 6.0 s (flashes at 4.0 s); frightened collision → `Eaten`; `Eaten` ghost reaches the house,
   waits 1.0 s, leaves; the door is impassable for the player and for non-leaving/eaten ghosts.
9. Scoring: dot 10, power 50, ghosts 200/400/800/1600 within one power pellet, chain resets on a new power pellet,
   extra life once at 10,000, level clear +200, score never decreases within a play, new play starts at 0.
10. Lives: 3 at start, one lost per collision, dots persist across deaths, 0 lives → `GameOver` and `PlayConsumed`
    fires once.
11. Charge: balance 1 → `TryStartPlay` false, balance unchanged, no transaction, no event; balance 2 → true, balance
    0, exactly one `Expense` transaction whose reason starts `Arcade play:`, one `PlayStarted`; a second call while
    playing → false and no second transaction; `QuitPlay` twice → one `PlayConsumed`.
12. Determinism: same seed + same input tape → identical final score and ghost positions.

Smoke test (PlayMode, one test): spawn the room and cabinet, place the player in front, assert the prompt reads
`Play maze game ($2)`, call `TryInteract`, assert `IsPlaying`, RT is assigned and `width == 512`, player controller and
carry are disabled, step pellets until `Score` increases by 10, press quit, assert `IsPlaying == false`, controllers
re-enabled, one transaction recorded, and `PlayConsumed` fired exactly once.

Also required before calling this done: a human check that the screen is legible at 1.5 m in the Game view and that
`Q` never drops carried laundry.

## 8. Open decisions and risks

- **To confirm:** A (sim keeps running), B (`Esc` suppression in `PauseController`), C (charge owner play), D
  (side ledger), E (session cap). Unconfirmed numbers: all speeds, timers, and scores above are untuned proposals.
- **No `EconomyService` instance exists in `Laundromat.unity`** (verified: only tests construct one). Phase 8 must
  give the cabinet a serialized `_economy` reference; until then `TryStartPlay` fails closed.
- **IP risk is real, not theoretical:** shipping recognisable Pac-Man ghosts, mazes, or "waka" audio invites a claim.
  Original art, original name, original layout, no sound in v1.
- **Scope risk:** `ArcadeMazeState` must stay pure C# so the 12 logic criteria run in EditMode. If it touches
  `MonoBehaviour` timing, half the acceptance list becomes untestable.
- **No audio, no animation, no juice in v1.** The minigame will feel flat; that is accepted for a first playable.

## 9. Change record

Created `.orchestrator/reports/pacman-minigame-spec.md`. No other file created, modified, or deleted. No asset was
generated, imported, or integrated. No Unity, Blender, package, or git action was taken.
