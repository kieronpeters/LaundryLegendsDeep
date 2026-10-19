# Laundry Legends — Game Design Bible

**Status:** Living document. Phase 0 baseline.
**Rule:** Sections marked **CONFIRMED** are commitments. Sections marked **PROPOSED** are design hypotheses that require implementation and playtesting before they become commitments.

## 1. Vision

**CONFIRMED.** Laundry Legends is a cozy, slightly mysterious 3D business simulation about running a neighborhood laundromat that hides something impossible in the back: an arcade that grows from a sealed room into a second, thriving business.

The game should feel satisfying in small doses—sorting, washing, drying, folding, and handing back clean laundry—and then surprise the player with a larger secret that recontextualizes the building and the business.

### Player fantasy

- “I turned a tired laundromat into something people love.”
- “I found a secret and built an entirely new business inside it.”
- “My two businesses feed each other in a way I planned.”

### Pillars

1. **Satisfying physical work.** Laundry handling should feel tangible, readable, and rhythmic, not like menu clicking.
2. **A secret worth keeping.** The arcade is foreshadowed, discovered, and expanded with deliberate reveals.
3. **Visible progress.** Money, equipment, cleanliness, lighting, customers, and space all visibly improve.
4. **Cozy tension.** Everyday chores and neighborhood warmth sit beside neon, hum, and mystery.
5. **Short-session readability.** A player can complete one order or one arcade upgrade in a few minutes.

## 2. Target experience and scope

**CONFIRMED.**

- Single-player, Windows 64-bit PC.
- First-person perspective, provisional until Phase 1 human review.
- Stylized 3D, small footprint (one building plus back room), 30–60 minute vertical slice.
- No combat, no online multiplayer, no open world, no horror gore.
- Mouse/keyboard primary; gamepad support is a later stretch goal.

**PROPOSED.**

- Session length: 15–45 minutes; orders take 3–6 minutes once understood.
- Rating target: everyone/teen, with mild mystery tension.
- Visual reference: warm neighborhood laundromat, worn tile, fluorescent light, sunset windows, handmade signage; secret arcade with magenta/cyan neon and dark carpet.
- Audio reference: washing-machine rhythms, dryer tumbling, door chimes, arcade attract modes, lo-fi music.

## 3. Core loop

**CONFIRMED.** The first playable loop is the laundry order loop.

| Step | Player action | System response | Feedback |
|---|---|---|---|
| 1 | Notice customer | Customer enters, approaches counter | Door chime, greeting, order ticket |
| 2 | Receive order | Basket/order is created | Ticket shows service, weight, payout |
| 3 | Collect laundry | Pick up basket or loose items | Carry pose, weight cue, prompt |
| 4 | Load washer | Interact with available washer | Door opens, items transfer, cycle starts |
| 5 | Wash | Wait or do another task | Timer, drum sound, progress light |
| 6 | Transfer to dryer | Interact with washer then dryer | Wet-laundry cue, dryer starts |
| 7 | Dry | Wait or do another task | Timer, heat/tumble audio, progress |
| 8 | Fold | Carry to folding table, interact | Fold progress, stack/bag visual |
| 9 | Return order | Give completed bag to customer | Customer takes bag, thanks |
| 10 | Get paid | Customer pays | Coins/cash sound, money HUD update |
| 11 | Reinvest | Buy/upgrade equipment | Visible improvement, new capability |

### Loop quality bar

- The player always knows what to do next without reading a manual.
- Interaction prompts name the object and the action.
- Machine state is readable from both UI and the world.
- An order cannot get permanently stuck through normal play.
- Failure or interruption produces a recoverable state.

## 4. Laundry operations

**CONFIRMED.** Needed for the first vertical slice:

- Baskets, loose laundry piles, and completed bags.
- At least one washer and one dryer; multiple machines later.
- Load, start, progress, complete, transfer, and unload interactions.
- Folding at a table as a short interaction, not a full physics simulation.
- An order ticket that tracks items and service state.
- A handoff/collection interaction with payment.

**PROPOSED.**

- Separate white, color, and delicate loads later.
- Detergent/softener as optional consumables after the core loop is stable.
- Machine capacity and load-weight constraints.
- Stains or special requests as later order modifiers.
- Folding minigame: hold and move the mouse along a fold path; quality affects tip, not core completion. Start with a simple timed hold.
- Lost-and-found items as small optional discoveries.
- Cleanliness/odor system only if it creates readable decisions; avoid stat clutter.

### Machine state model

`Empty → Loaded → Running → Complete → Unloaded`

A machine may also be `Broken` or `Reserved` later. The model must prevent invalid transitions and expose progress events for UI.

## 5. Customers

**CONFIRMED.** Basic customer lifecycle:

`Arrive → Queue/Wait → Handoff → Wait for order → Collect → Pay → Leave`

**PROPOSED.**

- 3–6 customer archetypes with different patience, order sizes, and tips.
- Queue capacity and waiting space.
- Patience decreases while waiting; very low patience reduces tip, never hard-locks the order.
- Regulars remember the player and unlock small perks later.
- Customer appearance and voice are placeholder until Phase 10/12.

### Interaction fantasy

The customer should feel like a neighbor, not a vending machine. Greetings, small reactions, and a visible order ticket create warmth without requiring expensive dialogue trees.

## 6. Economy

**PROPOSED BASELINE — none of these numbers are implemented or tuned.**

| Item | Starting value |
|---|---|
| Starting cash | $40 |
| Basic wash order payout | $8–$15 |
| Wash operating cost | $2 |
| Dry operating cost | $3 |
| Daily rent/operating cost | $10 (introduced after first loop) |
| Tip range | $0–$5, based on speed/care |
| Folding table upgrade | $60 |
| Second washer | $180 |
| Second dryer | $180 |
| Secret-room repair | $150 |
| First arcade machine | $350 |
| Arcade machine income | $2 per play, 1–3 plays per visitor |
| Arcade decoration/upgrade | $75–$500 |

### Economy rules

- Every transaction goes through one economy service.
- Income and costs must be visible in a simple daily summary.
- Upgrades should unlock capability or reduce friction, not only increase a number.
- Avoid exponential runaway; use soft caps, space limits, and rising costs.
- The player should be able to recover from a low-cash state without a hard fail.

## 7. Upgrades and business simulation

**CONFIRMED.** Progression must connect laundry success to arcade access.

**PROPOSED upgrade categories:**

- Capacity: more washers, dryers, baskets, folding space.
- Speed/efficiency: faster cycles, lower utility cost, larger loads.
- Comfort: seating, décor, lighting, signage, music.
- Service: express orders, delicate service, pickup/delivery later.
- Secret room: access, cleaning, power, security, arcade machines, carpet, lighting, prizes.
- Arcade: more cabinets, better games, marketing, prize counter, expansion.

A simple daily summary should show revenue, costs, orders completed, arcade plays, and reputation changes. Avoid tax/loan/stock-market complexity unless playtesting proves it adds fun.

## 8. Secret room discovery

**CONFIRMED.** The arcade must be discovered through play and remain central.

**PROPOSED discovery chain:**

1. **Foreshadow:** a locked door behind shelving, a draft, a faint electrical hum, an old arcade token, or a utility bill that does not match the building.
2. **Trigger:** complete 5 orders and earn $100 in laundry revenue (placeholder thresholds).
3. **Reveal:** move the shelf or open the service door; lights flicker on; a dusty room is visible.
4. **Prepare:** clear debris, restore power, repair the door, clean the floor. Each step is a small task with visible change.
5. **First machine:** purchase/install a used cabinet. Power it on. The attract mode is the payoff.
6. **First customer:** arcade traffic begins after a small marketing/opening action.
7. **Expansion:** add machines, improve ambiance, increase capacity, unlock new rooms or a second floor later.

### Reveal rules

- The reveal must be earned, not random.
- The player should be able to see progress toward discovery.
- The arcade should not invalidate the laundry. Laundry footfall, reputation, and cash feed it.
- The room should visibly transform from derelict to alive.

## 9. Arcade

**CONFIRMED.** The arcade is a business with machines, customers, income, and upgrades.

**PROPOSED.**

- Cabinets have genre, theme, condition, appeal, power draw, and income.
- Customers choose machines based on appeal, price, and crowding.
- Machines can break down and be repaired later.
- A prize counter and high-score board create long-term goals.
- Playable arcade microgames are a later stretch goal; the first version can simulate plays with lively visuals and sound.
- Arcade income is collected at a cash box or automatically summarized, depending on playtest preference.
- Laundry reputation increases foot traffic; arcade success funds better laundry equipment.

### Design guardrail

Do not let the arcade become a menu-only idle game. The player should walk into the room, see machines, hear them, and make spatial decisions about layout and expansion.

## 10. Progression map

**PROPOSED milestone sequence:**

1. **Open for business:** first customer, first completed order.
2. **Reliable service:** second washer/dryer, folding table.
3. **Neighborhood staple:** multiple customers, daily costs, visible upgrades.
4. **The locked door:** discovery clue and first reveal.
5. **Hidden potential:** room cleared, power restored.
6. **First credit:** first arcade machine installed and played.
7. **Word of mouth:** arcade traffic and combined income.
8. **Neon expansion:** more cabinets, décor, and a larger room.
9. **Two businesses, one legend:** endgame tier with a fully realized arcade and a thriving laundromat.

## 11. UI and UX

**CONFIRMED.**

- Diegetic interaction prompts plus a minimal HUD.
- Money, current order, machine progress, and interaction target must be readable.
- Pause/settings/save access from the game.
- No essential information only in audio.

**PROPOSED.**

- HUD: cash, day/time or shift, active order ticket, machine status strip.
- Interaction prompt: object name + verb + key, centered near the reticle.
- Order ticket: customer, service, state, payout, patience.
- Machine panel: state, time remaining, load contents.
- Secret-room progress: a subtle clue journal or visual checklist, not a quest marker that spoils the discovery.
- Pause menu: resume, settings, save, quit.
- Settings: resolution, window mode, master/music/SFX volume, sensitivity, invert Y, FOV, subtitles, brightness.
- Accessibility: text size, subtitle background, color-independent state cues, hold/toggle interaction option, reduced camera motion.

## 12. Visual direction

**PROPOSED.**

- Stylized low-poly with clean silhouettes and warm material contrast.
- Laundromat palette: cream tile, faded teal, warm wood, laundry whites, sunset orange, practical fluorescents.
- Arcade palette: deep carpet, black/indigo walls, magenta/cyan neon, warm cabinet marquees, reflective surfaces used sparingly.
- Lighting tells the story: flat, slightly tired fluorescent laundry → warm evening lamps → neon glow and colored bounce in the arcade.
- Signage and props carry personality: handwritten prices, neighborhood notices, worn chairs, coin slots, prize shelves.
- Avoid photoreal skin, complex cloth simulation, and expensive volumetric effects for the first playable.

## 13. Audio direction

**PROPOSED.**

- Laundry ambience: distant street, door chime, washer fills, drum rotation, dryer tumble, folding cloth, coins, receipt printer.
- Arcade ambience: cabinet hum, attract-mode bleeps, joystick clicks, prize dispenser, muffled music through a wall.
- Music: sparse lo-fi/neighborhood groove for laundry; synthwave/chiptune layer for arcade; stems crossfade as the player moves between spaces.
- Voice: short greetings and reactions, optional; subtitles always available.
- All audio must have documented licensing. Placeholder synthesized tones are acceptable until licensed assets exist.

## 14. Tutorial and onboarding

**PROPOSED.**

- First order is guided by contextual prompts, not a modal wall of text.
- Teach one verb at a time: pick up → load → start → transfer → fold → return.
- The first customer waits generously.
- The secret room is not tutorialized until the laundry loop is understood.
- A compact help screen repeats controls and machine states.

## 15. Save and progression reliability

**CONFIRMED.**

- Save money, progression flags, machine/order state, arcade state, and settings.
- Version the save format.
- Recover gracefully from missing/corrupt data without crashing.
- Autosave at safe points; manual save in pause menu.

**PROPOSED.**

- Single save slot initially; multiple slots later if needed.
- Autosave after payment, upgrade purchase, and room transition.
- Corrupt-save fallback creates a new profile and preserves the bad file for diagnosis.

## 16. Vertical slice content target

**CONFIRMED for the first playable milestone:**

- One laundromat room, counter, waiting area, at least one washer, one dryer, folding table, and a hinted secret door.
- One customer archetype with a complete order lifecycle.
- One full order loop with payment and a reason to repeat.
- A minimal HUD, pause, and save.
- Placeholder art/audio that is clearly functional.

**Not required for the first slice:** full arcade gameplay, multiple customer types, final character models, final music, decorations, release build, or Steam integration.

## 17. Release goals

**PROPOSED.**

- Windows 64-bit build that starts reliably and can be played with mouse/keyboard.
- 30–60 minute curated demo or a longer sandbox depending on playtest results.
- All essential features verified, no game-breaking blockers, and a documented known-issues list.
- Store/publishing decisions require explicit human approval; no external release is implied by this document.
- Potential development-series footage: first greybox, first completed order, secret-room reveal, first arcade machine, before/after lighting.

## 18. Confirmed versus proposed decisions

### Confirmed

- Unity 6.0 LTS / Built-In RP / Windows PC.
- Laundry loop first; secret arcade as central long-term progression.
- First-person provisional.
- Placeholder-first asset strategy.
- Simple component architecture.
- Human approval gates for destructive, dependency, settings, and publishing actions.

### Proposed and awaiting playtest or human confirmation

- First-person vs third-person final choice.
- Exact order timings, payouts, costs, and thresholds.
- Folding interaction design.
- Customer patience/tip model.
- Arcade microgames vs simulated play.
- Art style specifics and final audio direction.
- Save-slot count and autosave cadence.
- Tutorial length and presentation.

## 19. Open design questions

1. Should the player character be visible (hands/body) or first-person with minimal body?
2. Is the tone cozy-mysterious, comedic, or slightly eerie?
3. Should arcade cabinets be playable minigames in the first release, or simulated?
4. Does the player set prices, or are prices fixed by service tier?
5. Should the secret room be behind the laundromat, below it, or accessed through a service corridor?
6. How much dialogue and voice acting is affordable?
7. What is the target session length and replay structure?
8. Does the game end, or continue as an endless business sandbox?

Record decisions in `.orchestrator/decisions/` when they are made.
