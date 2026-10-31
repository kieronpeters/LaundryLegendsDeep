# Competitive Feature Analysis — Laundry Legends

- **Status:** proposal / reference. Nothing here is implemented. No asset, code, or Unity change was made.
- **Written:** 2026-11-01 (ll-designer). Documentation only.
- **Purpose:** compare the day-loop, HUD, cash handling, back-office, pacing, and failure conventions of
  comparable shop sims against the current build, then rank what we should add.
- **Read for this document:** `MASTER_CONTEXT.md`, `GAME_DESIGN.md`, `PROJECT_STATE.md`, `ROADMAP.md`
  (Phase 8–9), `.orchestrator/reports/2026-11-01-phase-5-closure.md`, `2026-11-01-phase-6-7-closure.md`,
  `pacman-minigame-spec.md`, and the runtime scripts named in §2.
- **Method:** web research (reviews, store pages, wikis, Steam guides) plus direct inspection of our scripts.
  Marketing copy was not treated as evidence of feel.

## 1. Verification legend

| Tag | Meaning |
|---|---|
| **V** | Corroborated by a review, store page, wiki, or guide found during this research |
| **U** | Not verified — plausible from genre convention only; do not plan against it |
| **O** | Verified by inspecting this repository |

Items about *feel* ("satisfying", "annoying") are always second-hand reviewer opinion, even when tagged **V**.

## 2. Where we actually stand (O)

- Order lifecycle `Arrive → Handoff → Waiting → Collect → Pay → Leave`, exactly-once payment, patience abandon.
  **One customer at a time**; no queue (Phase 5 closure).
- `EconomyService` owns `TryEarn` / `TrySpend` / `TryPurchase`, a transaction log, and `GetSummary()`
  (income, expenses, upgrade spend, net). `DailyOverhead.TryCharge` (15/day) exists and is tested but
  **nothing calls it on a day advance**.
- `GameState` holds `Day = 1` and `TimeOfDayHours = 8f` plus `AdvanceDay()`, but **no clock ticks and nothing
  ends a day**. The HUD label "Today:" therefore describes a day that never closes.
- Two upgrades: fast dryer (×0.7 dryer time), high-efficiency washer (×0.75 wash time). Bought with the **`U`
  key** through `CustomerSpawner.TryPurchaseNextUpgrade()`.
- Secret room: 3 clues + 3 completed orders → one-shot reveal → `Cleaned → Powered → Repaired → Usable`.
  Lights exist but are off until Powered; room content is placeholder.
- Maze cabinet is playable (`MazeGame`, `MazeGameView`, `ArcadeCabinet`, `MazeLayout`, `MazeTile`).
- HUD today: one IMGUI box at top-left (`OrderTicketView`) containing wallet, order stage, "Today" summary,
  the full upgrade list, next goal, and a 5-second receipt line — plus `InteractionPromptView` and
  `SubtitleView`. No clock, no day counter, no objectives, no machine status strip.
- No save system.

## 3. Comparison table

| Game | Day shape / how a day ends | Permanent HUD | Cash handling | Back office / safe | Failure handling |
|---|---|---|---|---|---|
| **Arcade Paradise** (closest reference) | 9am–11pm, accelerated (~1 in-game min per real sec; ~14 real min/day) **V**. Trash spawns at day start **V**. Day ends by leaving/going home; staying causes a pass-out **V**. | Deliberately minimal: wristwatch raised to read time and task reminders **V**; PDA carries cabinet stats, pricing, difficulty, to-do list **V**; machine timers sit on the machines, not the HUD **V**. | **Physical and deliberate**: empty coin hoppers and collect laundry payouts, then **deposit into the office safe (3-dial combo) before money is spendable** **V**. | **Yes, and load-bearing**: office safe + PC ordering + PDA. A second currency (£) from a daily to-do list buys personal upgrades **V**. | **No lose state**; poor play only reduces daily income **V**. |
| **Laundromat Simulator** (2024, Mammalian Machine) | Store-by-store operation; "collecting coins from machines, tracking daily earnings" **V**. Day boundary mechanics **U**. | Store rating / customer happiness **V**; specific HUD layout **U**. | Collect coins from machines physically **V**; no documented banking step **U**. | No documented safe/office; expansion is the money sink **U**. | Not documented; described as passive-income growth, no fail state **U**. Reception: Mixed, 55% **V**. |
| **Gas Station Simulator** | Continuous day with clock; left-edge notification tab reports waiting customers/deliveries **V**. Sleep-to-advance **U**. | Left-side event/objective tab **V**; employee status panel added post-launch **V**. | Cash register minigame at the till **V**; loans and purchases through the register computer **V**. No safe-deposit step documented **U**. | Computer at the register is the business surface: order stock, pay loans, buy upgrades, hire/call staff **V**. | "Not really any penalty for getting things wrong"; unserved customers drive away **V**. |
| **Supermarket Simulator** | ~7 real minutes per day; **time does not start until you open** **V**. Ending the day with an unfinished checkout loses that sale **V**. | Money, day, store level, objectives **U** (broad shape only). | Player runs the till: scan, take cash/card; credited at checkout **V**. Bank exists for loans **V**. No safe-deposit step **U**. | Store PC for ordering/pricing/hiring; bank for loans **V**. No safe. | Loans and slow days; **no bankruptcy**; theft events **V**. |
| **Internet Cafe Simulator 2** | Sleep to advance, energy/sleepiness stats, one skill point per in-game day **U**. | Money, stats, skill tree **U**. | Collect payment from customers at machines **V** (exact interaction **U**). | Build/buy mode, guards, tech tree **V**. | Debt-to-brother framing plus thugs/bombs **V**; famously buggy saves **V**. Cautionary example, not a model. |
| **Stardew Valley** | 6:00–2:00, ~14 real min; **sleep ends the day** and runs the overnight summary **V**. Passing out outside costs 10% gold, cap 1,000g **V**. | Clock + date, gold, energy bar, quest log **V**. | **Automatic at day end**: shipping bin sells overnight and pays in the morning summary **V**. | No office; shops (Pierre, blacksmith) are the upgrade surfaces; farmhouse owns sleep/save **V**. | Soft-fail only: time, energy, money loss. Never unwinnable. |
| **Animal Crossing: New Horizons** | Real-world clock; day rolls over in the early morning; shop hours constrain play **V**. No sleep action. | Minimal; money lives in the inventory/wallet, time in a corner clock/phone **V**. | Wallet plus an ATM (Nook Stop) for savings; drop box pays **next day at 80%** **V**. | Resident Services / Nook Stop is the office analogue: ATM, upgrades, town settings **V**. | No fail state; open-ended interest-free debt. |

### 3a. Pacing anchors

| Game | First meaningful purchase | Second-business / expansion gate |
|---|---|---|
| Arcade Paradise | Starting cabinets already sit in the back room; new cabinets from **$500** **V**; cheap quality-of-life upgrades from the £ to-do currency **V** | Not gated by a story beat — the player simply reinvests. Expansion is continuous, not a door **V** |
| Supermarket Simulator | Storage room **$800** **V**; licenses and shelves earlier **U** | Store level gates loans and stock tiers **V** |
| Stardew Valley | Backpack **2,000g**, copper tool **2,000g + 5 bars** — reachable in the first week **V** | Not applicable; upgrades are strictly sequential tiers **V** |
| Animal Crossing | Cheap tools/furniture immediately **V** | Nook's Cranny upgrade: **30 days + 200,000 Bells of transactions** **V** — deliberately slow, explicitly not a grind shortcut |
| **Laundry Legends (ours, O)** | Two upgrades; cost values in `GAME_DESIGN.md` are untuned proposals | Reveal already gated on **3 clues + 3 orders**; first machine is Phase 8 and not implemented |

## 4. We already have / we are missing / we should deliberately not copy

### Already have (O)

- Exactly-once payment with a rejection path — stronger than most comparables, which are loose about this.
- One auditable economy service with a derived daily summary; unaffordable purchases rejected atomically.
- Patience abandonment that cleans up the pipeline instead of soft-locking (Phase 5 fix).
- A gated, one-shot secret-room reveal with staged preparation states.
- A playable cabinet — most laundry sims have no second business at all.
- Interaction prompts that name object + verb + key.

### Missing (ranked by how much it hurts the current build)

1. **No day.** No clock, no close, no summary moment. Every comparable has a day boundary; ours has a
   `Day` field and a "Today" label with nothing behind them.
2. **Operating costs never fire.** `DailyOverhead` is dead code from the player's point of view, so there is no
   pressure and no reason for the summary.
3. **Upgrades are a keypress, not a place.** `U` buys the cheapest unowned upgrade with no confirmation, no
   comparison, and no in-world surface. Every comparable puts purchases at a counter, PC, or board.
4. **No objective/todo layer.** Nothing tells the player what a good day looks like beyond "serve the order".
5. **No machine status at a glance.** State lives in the world only; there is no strip or pip showing which
   machine is running and for how long.
6. **No cash ritual.** Money appears in a counter. There is no collection or banking beat, so income has no
   physical payoff and the arcade has no reason to have a cash box.
7. **No records.** The summary resets conceptually but never archives; there is no "last 7 days" view.
8. **No visible link between laundry reputation and arcade traffic** (Phase 9 scope).

### Deliberately do not copy

- **Arcade Paradise's mandatory chore rotation** (trash, gum, toilet) as a daily tax. It is the most-cited
  friction in that game's reviews **V** and it fights our "satisfying physical work" pillar by adding
  busywork that is not laundry.
- **Employee hiring, skills, shifts, wages** (Gas Station, Supermarket, Internet Cafe). Large system, new UI,
  and it automates away the verb the game is about.
- **Punitive failure**: debt collectors, theft, bombs, guards, 10%-gold pass-out penalties. `GAME_DESIGN.md`
  already commits to "recover from a low-cash state without a hard fail."
- **A second currency** (£, tokens, skill points). One wallet keeps the summary readable.
- **A 35-cabinet collection.** One cabinet that is actually playable beats five that are menus.
- **Real-time or real-calendar gating** (Animal Crossing's 30 days). Wrong for a 30–60 minute slice.

## 5. Prioritized features for us (10)

Weights are rough engineering size including tests and validation, not schedule promises.
**S** ≈ one task · **M** ≈ two to three tasks · **L** ≈ a phase.

| # | Feature | Player-facing value | Weight | Notes |
|---|---|---|---|---|
| 1 | **Day clock + close-out ritual + summary screen.** Clock visible in-world or corner; a "close up" interaction (counter bell / sign flip) ends the day, charges overhead, shows summary, advances `Day`. | Turns a sandbox into a rhythm; makes the existing summary and overhead real; gives sessions a stopping point. Highest structural value. | **M** | Do not require walking to a bed. Keep the close-out at the counter. |
| 2 | **Back-office upgrade board.** A ledger/board in the utility alcove listing upgrades with cost, owned state, and effect; buy with a confirm. Retire the `U` key. | Discoverability, spatial ownership, and a reason to walk to the back of the shop. Fits the "back room" identity. | **M** | Doubles as the records surface for #7. No new economy logic. |
| 3 | **Machine status strip + in-world timers.** One compact row of machine pips (state + remaining time), plus a readable timer on the machine face. | Removes the "which machine is done?" scan; the single most repeated question in the loop. | **S** | Read-only view over existing machine events. |
| 4 | **Daily to-do list (3 optional chores, small bonus).** e.g. serve 2 orders, fold without a mistake, tidy 3 items. Pays into the same wallet. | Direction for a short session without a quest marker; rewards play, never punishes skipping. | **M** | Must be optional. Bonus small enough that ignoring it is viable. |
| 5 | **Arcade coin box → one safe deposit.** Empty the cabinet coin box, carry the takings, deposit once in the safe. No combination lock. | Physical cash payoff; makes the arcade feel like a business you own; creates a reason to cross the building. | **S** | Money stays spendable only after deposit — say so in the prompt. Do not copy the dial minigame. |
| 6 | **Counter-side patience display.** Ticket on the counter or a small patience arc on the customer, not a HUD bar. | Fair warning before abandonment; keeps tension cozy and diegetic. | **S** | Pairs with the eventual multi-customer queue. |
| 7 | **Seven-day ledger page.** Last 7 days: income, costs, orders, arcade plays, net. | Visible progress over time; makes a bad day legible instead of mysterious. | **S** | Pure read of the transaction log. |
| 8 | **Reputation → arcade walk-ins, shown as one number.** Laundry completions set expected arcade visitors; the summary reports actual vs expected. | Delivers the promised "two businesses feed each other" fantasy with one number and no new sim. | **M** | Phase 9 scope. Do not build a hidden reputation stat with many inputs. |
| 9 | **First-machine power-on ceremony.** Lights, attract mode, a music sting, a customer peeking in. | The payoff moment the whole secret arc proves. Cheap to make memorable. | **S** | Presentation and sequencing only; no new mechanics. |
| 10 | **Per-cabinet price dial with visible effect.** One price-per-play setting; the daily summary shows plays and revenue before/after. | Gives the arcade one real decision without a spreadsheet. | **S/M** | Cap it at one knob. No difficulty, appeal, condition, or popularity stats. |

If only three ship: **1, 2, 3.** They convert the current build from "systems that exist" into "a day you play".

## 6. HUD/UI lessons

**Do**

- Reserve exactly one permanent corner for **money + day + time**; everything else is contextual.
- Put machine state **in the world** (timer on the face) and mirror it in one thin strip — never require a
  menu to answer "is my laundry done?"
- Give transient messages (receipt, tip, "machine done") one fixed slot and an auto-dismiss timer.
- Let the pause menu own purchases, records, settings, and save; keep purchase surfaces in the world.
- Report only what the simulation actually applied. A "Today" line with no day boundary is misinformation.
- Keep state cues color-independent (shape/icon/text), per `GAME_DESIGN.md` §11.

**Don't**

- Don't ship a permanent debug panel. Our top-left box (wallet + order + summary + full upgrade list + next
  goal + receipt) is six surfaces in one rectangle — the single weakest UI element in the build.
- Don't hide money, but don't put a second currency next to it.
- Don't use a quest marker for the secret room; it spoils the discovery the game is built on.
- Don't turn banking into a minigame. Arcade Paradise's combo safe is memorable and also the first thing
  players look for a workaround for **V**.
- Don't give "next goal" and "today's summary" equal visual weight — they answer different questions.

## 7. Where our HUD is weakest versus these games

1. **No clock and no day** — every comparable shows or diegetically implies time remaining. We show nothing,
   so the player cannot plan and the summary has no meaning.
2. **No objective layer** — Arcade Paradise (to-do list), Gas Station (objectives tab), and Supermarket
   (store level goals) all answer "what should I do now?". Ours answers only "what is this order doing?".
3. **Wrong shape** — one always-on IMGUI rectangle holds permanent, contextual, and transient information at
   equal weight. The comparables separate these into corner / world / toast.
4. **Purchases are keyboard-only** (`U`), which is undiscoverable and has no confirmation or comparison.
5. **Machine state is world-only**, with no progress readout, while reviews of this genre consistently praise
   visible timers as the thing that makes waiting tolerable **V**.

## 8. Scope and tone warnings

- Feature 1 needs a decision on **what happens to in-flight orders at close**. Options: block closing until the
  order resolves, or let it carry to the next day with patience paused. Carry-over is cozier; blocking is
  clearer. Decide before implementation.
- Features 1 + 4 + 8 together create daily pressure. Keep the to-do list optional and the reputation link
  explanatory, or the game stops being cozy.
- Feature 5 (physical deposit) is the highest tone risk of the recommended set: if the player can be paid but
  cannot afford an upgrade because cash is "on the floor", that is friction, not texture. Keep deposit one
  action, from anywhere the takings are held.
- Anything requiring new art (office furniture, ledger prop, coin box, to-do board) is **brief-only** right
  now: no image or 3D generation tool is verified. Build these as primitives or defer.
- Do not let features 3, 8, and 10 accumulate into a stat dashboard. Our scope is one room, one cabinet, one
  business link.

## 9. Open questions for the owner

1. **Day end:** player action only (proposal), or also a clock timeout with a "we close in an hour" nudge?
2. **Cash:** spendable before deposit (proposal: automatic for laundry, physical for the arcade), or gated
   behind deposit (Arcade Paradise), or fully automatic (Stardew)?
3. **Records:** physical board for upgrades and a pause-menu tab for history, or one surface for both?

## 10. Unverified / not researched

- Exact day lengths, sale prices, and upgrade costs for every comparable except the specific figures cited.
- Any hands-on play of these games during this task — all impressions are second-hand.
- Laundromat Simulator's day-end mechanics and HUD, and Internet Cafe Simulator 2's precise cash flow.
- Whether Supermarket Simulator or Gas Station Simulator has any safe/deposit step at all.
- Performance, feel, and fun of any feature proposed here. None of it has been built or tested.
