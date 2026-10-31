# W6 — Local open-weight TTS: engine install and dialogue render

- **Task:** W6 (voice track for Phase 12 / decision 0008, governed by decision 0010)
- **Date:** 2026-10-31
- **Status:** engine installed, 54 draft clips rendered and mechanically validated. **No commit made** (subagent rule). No Unity work, no playback, no listening pass.
- **Branch:** `main` @ `c834cd6`, working tree untouched apart from the files listed below.

## Objective

Install open-weight TTS locally (owner-approved), pick licence-clean voices, render the 54 written
dialogue lines to importable WAV drafts, and prove — mechanically, not audibly — that the files are
well-formed.

## Result

| Item | Value |
|---|---|
| Engine | Piper **2023.11.14-2**, MIT-era release, portable zip |
| Engine path | `Tools/Piper/piper/piper.exe` (+ `espeak-ng.dll`, `onnxruntime.dll`, `espeak-ng-data/`) |
| Source | `https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip`, SHA-256 `f3c58906…efcea` |
| Voices | `en_GB-cori-high` (attendant), `en_US-ljspeech-high` (phone regular), `en_US-norman-medium` (drop-off) — all **public domain**, trained from scratch |
| Clips | **54 / 54 rendered** — 18 per archetype, 3/4/4/3/4 across enter/counter/waiting/pickup/idle |
| Format | 22,050 Hz, 16-bit, mono, 3.66 s – 6.08 s (mean 4.87 s), peaks −0.9 dBFS |
| Validation | **54 pass, 0 fail** (exists, parseable, 22 050 Hz, mono, 16-bit, > 0.4 s, non-silent, peak < full scale) |
| Report | `ArtSource/Voice/rendering-report.csv` |
| Licence record | `ArtSource/Licenses/piper-tts.md` |
| Operator notes | `ArtSource/Voice/README.md` |

## Files created / modified

Created:

- `Tools/Voice/render_dialogue.py` — renders + validates + writes the CSV. Parses the line text
  directly out of decision 0008 §4, so the script cannot drift from the approved script.
- `ArtSource/Voice/Draft/{attendant,phone_regular,drop_off}/*.wav` — 54 files, 11.1 MB.
- `ArtSource/Voice/Samples/id_ID/*.wav` — 2 audition clips (see finding; **not shippable**).
- `ArtSource/Voice/rendering-report.csv`
- `ArtSource/Voice/README.md`
- `ArtSource/Licenses/piper-tts.md`
- `Tools/Piper/**` — engine + 4 voice models, 376 MB, **git-ignored on purpose**.

Modified:

- `.gitignore` — added `Tools/Piper/piper/`, `Tools/Piper/voices/`, `ArtSource/Downloads/`.
  Without this a future `git add -A` would commit 376 MB of third-party binaries.

Not touched: `ASSET_INVENTORY.md`, `PROJECT_STATE.md`, `Assets/**` (another agent's work is in
flight there; the lead should record this in the inventory once clips are actually imported).

## Checks performed

| Check | Command | Result |
|---|---|---|
| Piper runs | `Tools\Piper\piper\piper.exe --help` | exit 0 |
| Single-line render | `piper.exe --model en_GB-cori-high.onnx --output_file … ` with text on stdin | exit 0, 5.59 s, 22 050 Hz mono |
| Full render | `python Tools\Voice\render_dialogue.py` (per archetype) | exit 0 each, 18/18/18 |
| Validation pass | `python Tools\Voice\render_dialogue.py --report-only` | exit 0, "54 line(s); 0 failed" |
| Independent re-read | `python -c` with `wave`+`array` on 3 files | rates/channels/peaks match CSV |
| Ignore rules | `git check-ignore -v` on models/binary/downloads | all ignored; drafts and CSV **not** ignored |
| Line count vs source | group-by archetype+trigger | matches decision doc exactly (3/4/4/3/4 ×3) |

## Checks NOT performed

- **No listening pass.** Naturalness, pronunciation ("Nyepi", "Bali"), pacing, and whether the three
  voices are distinguishable are **unverified**. This is the single biggest open risk and cannot be
  closed by an agent that cannot hear.
- No LUFS / true-peak measurement (no meter, no ffmpeg) — only sample peak from the WAV bytes.
- No silero/VAD silence trim, no de-essing/EQ/compression.
- No OGG conversion, no Unity import, no `.meta`, no `DialogueLineSet`, no AudioSource wiring.
- No lip sync, no subtitle binding, no 3D falloff setup.

## Notable failures and dead ends (recorded so they are not repeated)

1. **`piper.exe` crashed with `0xC0000409` on every input.** Root cause: my first `Expand-Archive`
   was cut off by a tool timeout, leaving `espeak-ng-data/lang/gmw/` (English voice definitions)
   missing; espeak then had no voice and fail-fasted inside phonemisation. Re-extracting the zip
   completely with `tar -xf` fixed it. The engine was never broken.
2. **Python TTS route is blocked on this machine.** `piper_tts-1.8.0-cp39-abi3-win_amd64.whl` would
   import on Python 3.15, but it requires `onnxruntime`, which has no cp315 wheel (1.31.1 ships
   cp311–cp314). Abandoned rather than fighting wheels, per instruction.
3. **Docker route abandoned:** `docker` CLI present but the daemon is not running.
4. **sherpa-onnx explored and dropped:** v2 removed espeak (needs a lexicon), v1.12.14 needs
   `sample_rate`/`n_speakers`/`comment` in the ONNX metadata that Piper files lack. Not needed once
   Piper worked.

## Findings

- **Indonesian voice exists but is not licence-clean.** `id_ID-news_tts-medium` is the only `id_ID`
  voice in the Piper catalogue. Its dataset licence is unverified ("See URL" → Kaggle indic-TTS) and
  it is fine-tuned from the research-only Lessac base. It was **not** used for shippable output;
  two English lines were rendered with it into `Samples/id_ID/` for audition only.
  Consequence: the shipped candidate set is **neutral English, not Balinese-accented**.
- **Licence minefield in Piper's English voices.** `en_US-ryan-*` and `en_US-hfc_*` are
  **CC BY-NC-SA** (non-commercial). The Lessac/Blizzard 2013 base is **research-only**, so every
  voice whose card says "Finetuned from … lessac" (vctk, alba, aru, joe, alan, libritts_r,
  id_ID-news_tts, …) was treated as unusable. Only *trained-from-scratch-on-public-domain* voices
  were used. Detail in `ArtSource/Licenses/piper-tts.md` §4.
- Decision 0008 proposed OGG at −16 LUFS for shipping; these are WAV drafts with no loudness
  normalisation. That gap is documented, not silently ignored.

## Reproducing one line

```powershell
python Tools\Voice\render_dialogue.py --only attendant_enter_01
```

which drives, via stdin JSON:

```powershell
Tools\Piper\piper\piper.exe --model "Tools\Piper\voices\en_GB-cori-high.onnx" `
  --json-input --sentence_silence 0.35 --length_scale 1.02
```

## Review outcome

Self-reviewed against the task brief. Two deliberate deviations, both recorded:

1. Files are named `<archetype>_<trigger>_<nn>.wav` inside the archetype folder (matches the three
   example names given in the brief, e.g. `phone_regular_counter_02.wav`).
2. A uniform −1 dB headroom trim is applied to all files. Justification: raw Piper peaks landed at
   full scale (32767) on at least one sample per line; a single constant gain removes any clipping
   risk without altering relative character levels or acting as per-file normalisation.

## Risks and limitations

- Quality is **unproven** until a human listens; mechanical validation cannot detect robotic
  prosody, mispronunciation, or two voices sounding like one person.
- `en_US-norman-medium` is the only medium-quality model in the set and is the most likely to sound
  weaker than the other two.
- `Tools/Piper/` (376 MB) lives only on this machine; a fresh clone must re-download before
  re-rendering.
- `DialogueBank.cs` did not exist at render time. If its text differs from decision 0008 §4, the
  clips will not match the on-screen subtitles and must be re-rendered.

## Approval required

None outstanding for what was done (TTS install was pre-approved). Required **before** the next
steps: human listen sign-off, and a decision on whether to chase a licensed Indonesian voice.

## Recommended next task

1. Owner listens to `attendant_enter_01`, `phone_regular_counter_02`, `drop_off_pickup_01` and the
   two `Samples/id_ID/` clips; accept/reject per archetype.
2. If accepted: copy approved clips to `Assets/LaundryLegends/Audio/Voice/{attendant,regular,dropoff}/`
   (decision 0010 — reviewed clips only), then attach `clip` fields in the dialogue data.
3. Record the clips in `ASSET_INVENTORY.md` and the credits screen at that point.
