# Decision 0008 — Voice and dialogue plan

- **Date:** 2026-10-31
- **Status:** Proposal for owner approval. No asset was generated, downloaded, or integrated by this decision.
- **Feeds:** Phase 5 (customers), Phase 10 (characters), Phase 12 (audio/presentation)
- **Reality check:** no image, 3D, or audio generation tool is available in this environment. Every clip named below is future work; prompts, plans, and briefs are not assets.

## 1. Current state and the actual gap

`NpcDialogue.Configure(string[])` cycles 5 text lines on a 6 s timer while the player is within 3.5 m, and draws an IMGUI box. `AmbientNpc` plays one Mixkit clip on spawn (`customer_hello.mp3`) and one on drop-off (`customer_thankyou.mp3`). The three archetypes already exist as CC0 100 Avatars prefabs: Attendant, PhoneRegular, StreetCustomer.

So the current speech is **two generic stock stings plus unrelated text**. There is no line data, no trigger mapping, and no way to attach a clip to a specific sentence. The gap is structural first, asset second.

## 2. License-clean voice options, ranked

| # | Option | License reality | Naturalness / topical fit | Account or cost | Verdict |
|---|---|---|---|---|---|
| 1 | **Paid cloud TTS, one-off batch render** (Azure `id-ID` neural, ElevenLabs multilingual, Google/Polly) | Commercial use only on a paid tier; free tiers are attribution/non-commercial. Voice use must be allowed for games. | Best. Can speak every line we write, with the right regional accent. | Account + roughly $5–25 for one month of a single batch. Verify list price. | **Best quality target if the owner approves one small spend.** |
| 2 | **Local open-weight TTS** (Kokoro-82M Apache-2.0; Piper MIT) | Permissive weights, offline, no account, no per-line cost. Verify each voice model's own license; Piper voices vary. | Good English, clearly synthetic on long lines. **No Indonesian voice in Kokoro.** | Free; requires a model download plus Python/ONNX runtime, which is an install approval gate. | **Recommended free path.** |
| 3 | **CC0 / CC-BY recorded human packs** (Mixkit, Freesound CC0, Mozilla Common Voice CC0, LibriSpeech CC BY 4.0, LJSpeech PD) | Clean but mixed: CC0 needs no attribution, CC-BY does, Mixkit forbids standalone redistribution. Per-file check required. | Real humans, but only generic utterances — greetings, thanks, laughs, single sentences read from books or Wikipedia. | Free or free-with-account (Freesound download). | **Good for stings; cannot carry topical dialogue.** |
| 4 | **Text-only subtitles** | No risk at all. | No voice, but the writing carries the personality. | None. | **Permanent fallback; never blocked.** |

**Honest limitation:** there is no free stock pack containing sentences about Bali's rainy season, school uniforms, or the end-of-year rush. Anyone claiming otherwise is describing TTS or a paid library. Speech corpora (Common Voice, LibriSpeech, LJSpeech) are recorded but their sentence inventory is unrelated text; word-splicing LibriVox or corpora into custom lines is technically public domain but audibly stitched and would not satisfy "not robotic".

**Excluded on license grounds:** Coqui XTTS v2 weights (CPML, non-commercial), any voice cloning of a real person without written consent, and any audio lifted from games, film, or YouTube.

## 3. Recommendation

1. **Make text the source of truth.** A line is a record: id, speaker, trigger, priority, text, optional clip. Subtitles always render; a clip is an optional field. This ships now and is never blocked.
2. **Voice target: batch cloud TTS on a paid commercial tier** (Azure `id-ID` for the two local adults, or ElevenLabs multilingual for a warmer read), rendered offline into `.ogg` and committed with its license note. Reason: only TTS covers 54 written lines with a plausible local accent, and a one-month batch is the cheapest way to get commercial rights.
3. **Fallback that blocks nothing:** keep the two existing Mixkit stings for enter/leave, keep every line subtitled, and let the `clip` field stay null. If no spend is approved, regenerate the same 54 lines locally with Kokoro (English, no accent) — same file names, same data, no code change.
4. **Do not gate Phase 4 or Phase 5 on voice.** Dialogue data and subtitles land with the customer phase; the voice pass is Phase 12.

## 4. Dialogue content (PROPOSED — 54 lines)

Rules: plain, natural English; local specifics (rain, school terms, ceremonies, humidity) instead of dialect spelling; no caricature, no tourist-guide exposition; every line ≤ 8 s spoken so subtitles stay readable; lines reference things the player can see or do.

### 4.1 Attendant / staff (works here; talks to the player as a colleague)

| ID | Trigger | Line |
|---|---|---|
| ATT-E1 | Enter | "Morning. Rain came in sideways about six, so the floor by the door is slick." |
| ATT-E2 | Enter | "You're just in time. The seven o'clock crowd already dropped off four bags." |
| ATT-E3 | Enter | "Machine three is making that noise again. I gave it one more load to prove a point." |
| ATT-C1 | Counter | "Two loads, one white, one mixed. I put the delicates on the cold cycle." |
| ATT-C2 | Counter | "This one's a school uniform. They want it back before noon." |
| ATT-C3 | Counter | "Customer said the stain is cooking oil. I'll pre-treat it and hope." |
| ATT-C4 | Counter | "End of the year is starting early. Everyone's washing everything at once." |
| ATT-W1 | Waiting | "Give the dryer another ten minutes. The air's too wet today for a short cycle." |
| ATT-W2 | Waiting | "Rainy season is good for business and terrible for drying. Trade-off." |
| ATT-W3 | Waiting | "If the power dips again, that old washer resets. Keep an eye on the panel." |
| ATT-W4 | Waiting | "I folded the towels already. They're on the shelf behind you." |
| ATT-P1 | Pickup | "That's the one for the phone lady. She'll be here after school pickup." |
| ATT-P2 | Pickup | "Checked it twice. No spots, and it still smells like soap, not the machine." |
| ATT-P3 | Pickup | "Bag's tied. If she asks, tell her we used the gentle spin." |
| ATT-I1 | Idle | "Nyepi next month. Everything shuts, so everyone wants their laundry before." |
| ATT-I2 | Idle | "My kid's class is doing the end-of-year performance. Three costume changes." |
| ATT-I3 | Idle | "This humidity. Nothing dries unless you want it to." |
| ATT-I4 | Idle | "We should get a second folding table before the ceremony rush." |

### 4.2 Phone regular (waits in the seating area, half on a call; knows the shop)

| ID | Trigger | Line |
|---|---|---|
| REG-E1 | Enter | "Sorry — one second. Yes, I'm at the laundry. No, the one by the corner." |
| REG-E2 | Enter | "Hi. Just dropping these off. I'll be quick, I'm on my way to work." |
| REG-E3 | Enter | "Hold on. Sorry. Two bags today, and the blue shirt needs a cold wash." |
| REG-C1 | Counter | "The school asked for the uniform back by Friday. Can we do Thursday?" |
| REG-C2 | Counter | "This is my husband's work shirt. He says it's fine. It is not fine." |
| REG-C3 | Counter | "Whatever you did last time worked. That collar came out white again." |
| REG-C4 | Counter | "Rainy season, so everything smells like a wet towel by day two." |
| REG-W1 | Waiting | "Sorry, say that again? The signal in here is terrible when it rains." |
| REG-W2 | Waiting | "Yes, I'm still waiting. No, I can't leave it until tomorrow." |
| REG-W3 | Waiting | "My daughter's school is flooded, so she's home. Which means more laundry." |
| REG-W4 | Waiting | "I'll sit by the window. Call me when it's done, I'll hear you." |
| REG-P1 | Pickup | "That's mine. Thank you — the kids will have clean uniforms tomorrow." |
| REG-P2 | Pickup | "Let me check the bag. Good. Last place lost a sock every single time." |
| REG-P3 | Pickup | "You're a lifesaver. I'll tell the others at the school gate." |
| REG-I1 | Idle | "End of the year, everyone's got something to wash. Weddings, ceremonies, school." |
| REG-I2 | Idle | "It's so humid the towels never finish. I've been wearing the same jacket all week." |
| REG-I3 | Idle | "The road by the market flooded this morning. Took me twenty minutes to get here." |
| REG-I4 | Idle | "I'll bring my mother's things next week. She has opinions about soap." |

### 4.3 Drop-off customer (walk-in; first visit or occasional)

| ID | Trigger | Line |
|---|---|---|
| DROP-E1 | Enter | "Hi, is this where I leave the laundry? Someone said you do it by the kilo." |
| DROP-E2 | Enter | "Good morning. Rain caught me at the door. Is there somewhere I can put this?" |
| DROP-E3 | Enter | "Hello — my neighbour sent me. She said you're careful with the white shirts." |
| DROP-C1 | Counter | "Just these two. One is bed sheets, the other is everything else." |
| DROP-C2 | Counter | "There's a spot on the sleeve. Cooking oil, I think. Can you have a look?" |
| DROP-C3 | Counter | "How long does it usually take? I don't mind waiting if it's not too long." |
| DROP-C4 | Counter | "I need it by Saturday. There's a ceremony at the temple and everyone's coming." |
| DROP-W1 | Waiting | "It smells good in here. Like clean cotton. Ours just smells like rain." |
| DROP-W2 | Waiting | "I'll wait outside, it's crowded. Or under the awning, at least." |
| DROP-W3 | Waiting | "The kids are off school this week, so the house is loud and the baskets are full." |
| DROP-W4 | Waiting | "Do you take card, or should I find an ATM before you finish?" |
| DROP-P1 | Pickup | "That's faster than I expected. Thank you, really." |
| DROP-P2 | Pickup | "The shirt looks new. I'll bring the rest of the family's next time." |
| DROP-P3 | Pickup | "Can I pay now? I have exact change, I think." |
| DROP-I1 | Idle | "It's been raining since Tuesday. My whole line is still damp." |
| DROP-I2 | Idle | "End of the year is expensive. New uniforms, new shoes, and now laundry." |
| DROP-I3 | Idle | "My son spilled juice on his good shirt. Of course it was the good shirt." |
| DROP-I4 | Idle | "Everyone says the dry season is easier. I'm still waiting for it." |

## 5. Subtitle / voice-trigger contract (PROPOSED)

**Line record:** `id, speakerId, archetype, trigger, priority, text, clip (nullable), clipLengthSeconds`.

| Trigger | Fires from | Priority | Cooldown (per NPC) | Cap |
|---|---|---|---|---|
| `PaymentComplete` | economy/payment | 0 (never interrupted) | once per order | 1 line, then silence |
| `OrderAccepted` / `OrderHandedOff` | order handoff | 1 | once per order | — |
| `OrderReady` | machine/order complete | 1 | once per order | — |
| `CounterApproach` | player within 2 m of counter | 2 | 45 s | 1 queued |
| `NpcEnteredRange` | player within 3.5 m | 3 | 60 s | 1 queued |
| `WaitingBark` | player idle in range ≥ 20 s | 3 | 30 s | — |
| `IdleBark` | no event for 45 s in range | 4 | 60 s | dropped first |

- **One speaker at a time** (global): a playing line is interrupted only by a strictly higher priority. Equal/lower requests are dropped, not stacked; at most one pending line exists.
- **Global cooldown** 8 s between any two lines; **per-archetype** no-repeat of the same id for 10 minutes or until that trigger pool is exhausted.
- **Sync:** subtitle appears at request time; audio starts via `PlayScheduled(AudioSettings.dspTime + 0.15f)`. Subtitle hides at `max(clipLength + 0.4 s, 2.5 s)`. A missing clip = subtitle only, same timings from a text estimate, one editor-only warning per id. Text and audio must be identical; never paraphrase one to fit the other.
- **Pause / mute:** `Time.timeScale == 0` freezes timers and pauses voice. Muting voice keeps subtitles. Subtitles are always on; settings may later allow "voice only when subtitles are on", never the reverse.
- **Distance:** 3D one-shot at the NPC's head, max distance 10 m; subtitle renders only within 8 m, or at any distance for the active order's customer.
- **Implementation note (not this task):** today's distance+timer cycle in `NpcDialogue` is replaced by a `DialogueDirector` reading a `DialogueLineSet` ScriptableObject; `AmbientNpc.Speak(clip)` stays for enter/exit stings. No code changes were made here.

## 6. Asset and licensing requirements

- **Naming:** `voice_{archetype}_{trigger}_{nn}.ogg` (e.g. `voice_attendant_counter_02.ogg`); Vorbis, 44.1 kHz, mono, normalized to about -16 LUFS, ≤ 8 s.
- **Layout:** `Assets/LaundryLegends/Audio/Voice/{attendant,regular,dropoff}/`; the two existing Mixkit files stay at the folder root for now.
- **CC-BY:** attribution text is mandatory, e.g. `"<file>" by <author>, <source URL>, CC BY 4.0; edited: trimmed and normalized.` Record it in `ArtSource/Licenses/voice-credits.md`, `ASSET_INVENTORY.md`, and the in-game credits screen. Avoid CC-BY-SA (share-alike).
- **CC0 / public domain:** record source, author, URL, and date anyway; no attribution required.
- **Mixkit:** no attribution, but no standalone redistribution of the raw clip.
- **Cloud TTS:** only on a plan whose terms grant commercial use; keep a dated copy of those terms; confirm the chosen voice is licensed for game use; no cloning a real person without written consent, including the owner's own contacts.
- **Local weights:** ship the model license (Apache-2.0 NOTICE / MIT) and verify each downloaded voice's own terms.
- **Never** ship a clip without its transcribed text, and never ship a line whose license cannot be shown on request.

## 7. Open items and next steps

1. Owner decision: approve a one-off paid TTS batch (recommended) or the local Kokoro path (install approval + English-only accent).
2. Next task proposal: dialogue data + subtitle-first wiring in the customer phase, using the 54 lines above; voice files attach later with no contract change.
3. Numbers here (priorities, cooldowns, distances, 54 lines) are proposals; they need playtest tuning before they become commitments.
