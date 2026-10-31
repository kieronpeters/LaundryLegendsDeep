# Laundromat Operations Research — Real-World Detail vs. Laundry Legends

- **Date:** 2026-11-01 · **Type:** research / design input, docs only. No code, scene, package, or Unity change.
- **Scope:** front of house, payment, cash & safe, back office, maintenance, Bali/Indonesia specifics.
- **Sources checked 2026-11-01:** AccessiaPass Bali laundry price guide (Oct 2026); CuciQu "Modal Usaha Laundry Kiloan 2026"; AlirPOS laundry SOP (Sep 2026); Tolliver CPA coin-laundry cash controls (2026); PLN Bali outage schedules (Oct 2026); Indonesian laundry SOP/nota templates. Values marked *(est.)* are estimates or tuning targets, not measured facts.

## 0. Two framing decisions before the table

1. **Currency.** The game says "Coins"; Bali says rupiah. Keep the internal `int` wallet but display **Rp** with thousands separators at **1 coin = Rp1,000**. Then the current 40 price reads **Rp40,000**, which is a credible 3–4 kg kiloan order at Rp10–13k/kg, and the 2-coin arcade play reads Rp2,000, also plausible.
2. **Building scale.** The 16×12 m room (192 m²) is a large coin-laundry, not a neighborhood kiloan kiosk (real: **30–60 m²**, *(est.)*). Keep the room as the game's landmark, but model the **service** as kiloan: weighed bags, next-day pickup, self-service machines as a secondary channel.

**Unit economics to anchor the sim (Indonesia, 2026, *(est.)*).** Small shop: 30–60 kg/day, 1–2 washers, average **Rp7,000–8,000/kg**, materials (detergent + water + power) **≈Rp1,200–1,600/kg**, rent Rp10–20M/year, 1–2 staff at Rp2.5–4M/month. So a 3 kg order at Rp12,000/kg bills Rp36,000 and costs roughly Rp4,000–5,000 in consumables before rent and labour — a 30–50% net margin when machines run near capacity. That is the shape the game's `DailyOverhead` should eventually take: mostly fixed, partly volume-driven.

## 1. Operation → current support → gap → game-friendly abstraction

| # | Real-world operation | Current game support | Gap | Game-friendly abstraction |
|---|---|---|---|---|
| 1 | Weigh bag at counter, customer watches the scale; 2–3 kg minimum; round to 0.1 kg | None; price fixed at 40 | No weight, no pricing basis, no minimum | Digital scale on counter; price = kg × rate; min 2 kg; "round down" rule visible |
| 2 | Ticket written in triplicate (red to customer, white/yellow shop); "Lunas" stamp if prepaid | `OrderLedger` `ORD-0001`, IMGUI ticket text | Number never appears on a physical object; no prepaid state | Nota booklet prop; tear red copy; stamp LUNAS; number also on bag tag |
| 3 | Tag garments / bag with order number so nothing mixes between customers | Per-instance item identity only | No visible tag, no mixing risk | One waterproof tag per order (not per garment); rare mix-up event |
| 4 | **Kiloan** = leave it, collect in 1–2 days. **Express** = same day, 2–3× price | Every customer waits in-store | No service tiers; waiting is unrealistic for kiloan | Express (waits, pays more) vs Kiloan (leaves, returns) — same pipeline, different clock |
| 5 | Wash-dry-fold vs **cuci-setrika** (with ironing); ironing often extra | Wash → dry → fold → wrap | No ironing tier, no price difference | Service tier flag: Cuci-Kering (fold) vs Cuci-Setrika (+25–35%) |
| 6 | Promised pickup time on the slip; WhatsApp "ready" message | Patience timer 90 s, `_tipWindow` 60 s | No promise, no notification, no no-show | Ticket prints "ready 17:30"; counter phone buzzes when racked |
| 7 | Finished orders stored on numbered racks (`Rak B-03`), retrieved in <30 s | Folding station holds one item | No storage, no location, no retrieval | Rack slots A1–C4; ticket shows slot; wrong-slot = short search |
| 8 | Unclaimed laundry: 7–30 days, then donate/dispose; storage fee sometimes | `Abandoned` state only | No aging, no shelf pressure, no policy | 14-day unclaimed shelf with date tags; day 15 = donation event |
| 9 | Complaints: fading, shrinkage, lost socks; claim capped at 5–10× the service price | None | No refund, no claim, no reputation hit | Complaint dialog: refund / re-wash / voucher; cap 5× order price |
| 10 | Payment: cash, QRIS, bank transfer; cards rare | Auto-credit on collection | One invisible channel; nothing to reconcile | Choose method at payment; channel split feeds the cash-up |
| 11 | Change from a till float; small notes always short; counterfeit Rp100k checks | None | No float, no change tension, no fake-note risk | Float Rp250k *(est.)*; "no small change" moment; UV check on 100k |
| 12 | Price board out front: per-kg rate, express column, minimum, "tanpa pewangi" option | Fixed 40, no board | Prices invisible; no upsell; no local language | Handwritten board: Rp12.000/kg, min 2 kg, Express 2×, no-fragrance |
| 13 | Tips: not customary; rounding up / keep-the-change in tourist areas | Flat +10 tip when collected ≤60 s | Tips are near-automatic and too large | Occasional round-up (0–5%), tied to care and speed, not guaranteed |
| 14 | Cash drops during the day when the drawer gets heavy | None | All money is abstract | Drop envelope at >Rp750k; logged with time |
| 15 | Safe with code, drop slot, two compartments | None | No safe, no lock state, no overnight risk | Safe object: code set once, drop slot, locked/unlocked state |
| 16 | End-of-day count: expected vs counted, over/short, signed slip | `DailySummary` session totals only | No reconciliation, no variance, no ritual | Count till → compare → stamp COCOK / SELISIH → day card |
| 17 | Bank deposit next morning; QRIS settles T+1 with MDR ~0.3–0.7% *(est.)* | Wallet doubles as bank | No deposit lag, no channel fees | Deposit bag → bank; QRIS clears next day minus fee |
| 18 | Supplies: detergent, softener, fragrance, plastic wrap, hangers, tags, receipt paper | Detergent bottle prop only | No stock, no ordering, no running out | 6 consumables with par levels; order from back-office phone |
| 19 | Staff: 1–2 workers, ~Rp2.5–4M/month *(est.)*, meals, reliability, honesty | Staff NPC with no cost | No wages, no schedule, no trust | 1 helper at Rp120k/day *(est.)*; skill/honesty affect speed and shrink |
| 20 | Maintenance log, faults, out-of-order sign, 1–2 day parts wait | None | Machines never fail, never wait for parts | Fault chance per ~40 cycles; handwritten OOO sign; part arrives next day |
| 21 | Load limit ~80% of drum (8–11 kg machine → 6–8 kg usable); water 30/40/60 °C; detergent 30–60 ml per 10 kg (≈1 ml/kg concentrate); overload = poor wash, residue, worn bearings | No capacity, temperature, or detergent model | Any amount loads; no program choice; no dosing consequence | Machine accepts 2–8 kg orders; program Cold/Normal/Hot (hot = paid upgrade in Bali's ambient-water climate); detergent drawn from stock, under-dose = re-wash |
| 22 | Utility bills: PLN (min 4,400 VA), PDAM/well water, internet; water and power track volume | `DailyOverhead` 15/day, **not wired to any day loop** | Flat, invisible, unmotivated cost | Itemized bills on a board; consumption scales with loads |
| 23 | Permits: izin usaha, signage, environmental; annual renewal | None | No compliance layer | One renewal card per year with a due date and small fee |
| 24 | Power: scheduled PLN outages, voltage dips, occasional island-wide blackout | Always on | No interruption, no generator value | 1–2 short outages/week *(est.)*: cycle pauses, clock resets, generator upgrade |
| 25 | Rainy season (Nov–Mar): sun-drying delays; hard/iron-rich well water in places | Always dry | No weather, no water quality | Rain adds a day to kiloan orders; backup dryer or covered rack |
| 26 | Delivery/pickup by Gojek/Grab; WhatsApp orders; motorbikes at the door | None | No delivery, no street life | Optional pickup requests via phone; parked scooters as ambience |
| 27 | Security: CCTV, alarm, coin-box/coin theft, staff shrink | None | No theft, no reason for upgrades | Overnight cash + unlocked safe = theft roll; CCTV/safe reduce it |

## 2. Proposed daily ritual loop

**Clock proposal:** 1 real second = 1 in-game minute. Day runs **08:00–20:00** (12 real minutes if fully attended; typical 10–14 min). Machine times: washer **35 s** (=35 min), dryer **45 s**, fold **8 s**, wrap **5 s**. This replaces the current 20/25 s cycles and makes the wall clock read truthfully.

| Phase | Player actions | Real time | If skipped |
|---|---|---|---|
| **Open** (08:00) | Raise shutter, lights, flip sign, count float (3 stacks), check drums for left laundry, empty lint traps, check detergent/softener/plastic | 60–90 s | Float unverified (hidden variance), lint raises dry time, stock-out blocks service |
| **Serve** (08:30–17:30) | Greet, weigh, quote, print nota, tag bag, load, transfer, fold, wrap, rack, hand over, take payment | 6–10 min | Orders late, patience drops, tips and reputation fall |
| **Maintain** (interleaved) | Lint after every 2 dryer loads, mop spill, restock, OOO sign, water-tank check, empty coin/till overflow | 45–90 s total | Faults, slips, longer cycles, mid-day stock-out |
| **Cash up** (17:30) | Stop intake, count till by denomination, compare to expected, record variance, drop to safe, stamp the slip | 90–120 s | Day cannot close cleanly; next day starts "unreconciled" |
| **Close** (18:00) | Machines off, lights off, lock safe, lock door, arm alarm, read day card | 30–60 s | Overnight theft risk rises; no day card |

**Totals:** ritual ≈ **4 min**, service ≈ 6–10 min, total **10–14 min per day**. The first day is manual; later a "quick count" upgrade shows only counted/expected/variance and cuts cash-up to ~30 s. Fast-forward during empty stretches; the ritual never blocks customers who are mid-cycle.

**Day card (5 lines, one stamp):** sales by channel · expenses · float returned · variance · net. One sentence of flavour ("A slow Tuesday. Two orders still on the rack.").

## 3. Safe and back-office design

**3.1 Space.** A 2.5×2.0 m office behind the counter *(est.)*: desk, chair, safe under the desk, supply shelf, wall board, CCTV monitor, counter phone, receipt book and stamp. Reuse the existing NW utility alcove; **do not touch the east secret door**.

**3.2 What the player physically does** (each is one or two interactions, no menus-first):
1. **Float count** at open — lay out 3 note stacks, hold to count each (≈10 s).
2. **Cash drop** mid-day — fold notes into an envelope, write time, post through the safe slot (≈5 s).
3. **Till count** at close — 6 denomination stacks, hold-to-count, running total (≈20–30 s).
4. **Reconcile & stamp** — the book shows expected vs counted; stamp COCOK (matched) or SELISIH (variance) (≈5 s).
5. **Deposit** — carry the bag to the bank or use the phone; clears to wallet next morning (≈10 s walk or 1 click).
6. **Bills** — pull pinned bills (PLN, PDAM, internet, rent, wages), pay each; overdue adds a fee.
7. **Supplies** — tick items on a clipboard; delivery next morning; par-level warnings on the shelf.
8. **Maintenance / roster** — optional: log a fault, order a part, confirm tomorrow's helper.

**3.3 What it displays.**
- **Till panel:** denomination stacks, expected, counted, variance, channel split (cash / QRIS / transfer).
- **Ledger spread:** today and yesterday; income, expenses, float, drops, net; last 7 days as a strip.
- **Safe panel:** balance, last drop time, lock state, next deposit.
- **Wall board:** bills due, stock levels, staff tomorrow, permit expiry, open faults.
- **Day card:** the 5-line summary the player actually reads.

**3.4 Economy wiring (the important part).**
- **One definition:** `Wallet = bank + safe + till`. The wallet stays the only spendable number; till and safe are *locations* of part of it, never a second balance.
- **New `CashSession`** (per in-game day): opening float, ordered movement list, counted total, variance, deposit id. Every movement stores the `EconomyService` transaction id, so the summary and the wallet cannot disagree.
- **Cash sale:** `TryEarn` + till increment. **QRIS:** `TryEarn` minus ~0.7% *(est.)*, clearing next day. **Cash expense:** till decrement + `TrySpend`. **Drop:** till → safe (no wallet change). **Deposit:** safe → bank (no wallet change).
- **Reconciliation:** expected = float + cash sales − cash expenses − drops. Variance = counted − expected. Tolerance ±Rp2,000 *(est.)*; larger creates a `selisih` record and feeds a **shrink** stat.
- **Risk:** cash left in the till above threshold, or safe unlocked at close, rolls a theft chance; the loss is a real `TrySpend("Theft")` and appears on the day card. CCTV, a time-delay safe, and an honest helper reduce it.
- **Persistence:** one `CashDayRecord` DTO per day (date, float, sales by channel, expenses, drops, counted, variance, deposit, closed-by). Versioned, save-safe, and it gives the ledger a history wall.

**3.5 Anti-busywork rules.** One count per day; automatic stacking animation with a skip; no coin sorting (Indonesia is note-based); quick-count and auto-reconcile upgrades; variance is shown once, not nagged; a perfect day is one satisfying stamp, not a spreadsheet.

**3.6 Starting numbers *(est., tune later)*:** float Rp200–300k; drop threshold Rp750k; wage Rp120k/day; variance tolerance Rp2k; base overnight theft 2%; QRIS fee 0.7%; bank deposit next morning.

## 4. Authentic details ranked by "credibility per hour of work"

| Rank | Detail | Effort | Why it lands | Plugs into |
|---|---|---|---|---|
| 1 | **Triplicate coloured nota** (red customer / white + yellow shop), number also written on the bag | S | The single most recognisable kiloan artefact; instantly reads "Indonesia", not "US laundromat" | Intake, `OrderTicketView` |
| 2 | **Digital scale + per-kg price + 2 kg minimum**, customer watching the display | S | Explains the price and creates a fair, visible transaction | Pricing, `LaundryOrder.Price` |
| 3 | **Plastic-wrapped bundle** with the nota tucked inside and a punched hole against condensation | S | Already have a wrapped-bundle mesh; film + label finish the fantasy | Folding station, handover |
| 4 | **Rack code on the ticket** (`Rak B-03`) and a 10-second retrieval | S | Makes storage real and gives the back room a job | West shelving, order state |
| 5 | **QRIS sticker + cash/QRIS choice**, with the channel split visible at cash-up | M | Locals pay by QRIS constantly; it also motivates the safe ritual | Payment, `EconomyService` |
| 6 | **Price board in Rp** with per-kg, express column, minimum, "tanpa pewangi" | S | Sets the world before the first customer speaks | Counter props |
| 7 | **LUNAS stamp** and prepaid vs pay-on-collection | S | One stamp changes the whole cash-up (and who owes what) | Nota, till |
| 8 | **Counter phone buzzing "cucian sudah siap"** | M | Replaces guessed patience with a real notification; very 2026 Bali | Customer agent, rack |
| 9 | **Check pockets** at intake → occasional pen/ink disaster | S | A memorable, fair failure the player caused | Intake choice, wash event |
| 10 | **Lint trap + handwritten "RUSAK" (out of order) sign** + 1–2 day part wait | S | Maintenance becomes visible and spatial | Machine controller |
| 11 | **PLN flicker/outage** pauses a cycle and resets the clock; generator upgrade | M | Bali-specific and mechanically meaningful (Feb 2026 island-wide blackout; recurring scheduled maintenance) | Machine timer, upgrades |
| 12 | **Rainy-season delay** (Nov–Mar) and a covered drying rack / backup dryer | M | Explains why drying capacity is worth buying | Weather, dryer upgrades |

## 5. What not to simulate (too tedious)

- Full bookkeeping, VAT/PPh filing, depreciation, loan amortisation — collapse to a monthly bill and a day card.
- Per-garment counting for kiloan orders — the industry explicitly prices by weight and does not count.
- Real-time 40-minute cycles and real waiting — keep the compressed clock.
- Manual coin sorting/rolling — Indonesia is note-based; auto-count with a skip.
- Detailed detergent chemistry, water hardness, and stain-by-stain treatment — one supply resource, one "treat stain" choice.
- Multi-week staff rosters, shift swaps, and hiring interviews — hire, daily wage, reliability stat.
- Permit bureaucracy chains — one renewal card with a due date.
- Delivery route navigation — abstract Gojek pickups to a phone request and a timer.
- Machine repair minigames beyond hold-to-fix or order-a-part.
- CCTV footage review as a regular task — only as a one-off after a loss event.

## 6. What a Balinese player would notice is wrong today

- "Coins" as currency, and a design baseline of $8–15 orders — should be **Rp**, per kilo.
- Customers **waiting in the shop** for a wash-dry-fold order; kiloan customers leave and come back.
- No **scale**, no **plastic wrap**, no **nota**, no **rack** — the four things every kiloan shop has.
- No **QRIS**, no WhatsApp, no Gojek — the payment and messaging layer of modern Bali.
- No **motorbikes** at the door, no sandals by the entrance, no offering on the counter.
- Perfect power and water; no rainy-season delay.
- A 192 m² shop with 8 machines is a coin-laundry, not a neighborhood kiloan kiosk — either own that as "the big one on the street" or shrink the fiction.
- Staff speaking only English; prices in a foreign currency; no handwritten signage.

## 7. Suggested slice (docs-only recommendation)

Ship in this order, each independently playable: **(a)** intake table — scale, per-kg price, nota, bag tag; **(b)** rack + ready notification + unclaimed shelf; **(c)** `CashSession` + till count + day card; **(d)** back-office room with bills, stock, and safe. (a) and (b) buy the most credibility for the least work; (c) turns the existing economy into a ritual; (d) is the long-tail system.

**Open questions for the owner:** display currency (Rp vs Coins), day length (10–14 min target), whether self-service customers are a separate channel or cut, and whether theft should be a real loss event or only a stat.
