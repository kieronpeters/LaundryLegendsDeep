# Voice — draft renders

Generated 2026-10-31 by workstream W6. **Drafts, not integrated.** Nothing here has been imported
into Unity, and no human has listened to it yet.

## What is here

```
ArtSource/Voice/
├── Draft/
│   ├── attendant/      18 clips  (attendant_<trigger>_nn.wav)
│   ├── phone_regular/  18 clips  (phone_regular_<trigger>_nn.wav)
│   └── drop_off/       18 clips  (drop_off_<trigger>_nn.wav)
├── Samples/id_ID/      2 audition clips — NOT for shipping, see below
├── rendering-report.csv
└── README.md
```

- **54 clips**, 11.1 MB total, all 22,050 Hz, 16-bit, mono (Unity's Pi/Little-endian PCM WAV).
- Filename contract: `<archetype>_<trigger>_<nn>.wav`, triggers `enter | counter | waiting | pickup | idle`,
  `nn` is 01-based within that archetype+trigger group. Lowercase, no spaces, stable across re-renders.
- Dialogue text source: the tables in `.orchestrator/decisions/0008-voice-and-dialogue-plan.md` §4.
  `Assets/LaundryLegends/Scripts/NPC/DialogueBank.cs` did not exist at render time, so the decision
  doc was used. **When `DialogueBank.cs` lands, diff it against `rendering-report.csv`** — the `text`
  column is the exact string that was spoken, and the KEEP-IN-SYNC rule from decision 0008 applies:
  if the text changes, re-render.
- Voice per archetype (one voice each, so characters stay distinguishable):

| Archetype | Voice | Character |
|---|---|---|
| attendant | `en_GB-cori-high` | female, British — the colleague behind the counter |
| phone_regular | `en_US-ljspeech-high` | female, American, brisk read — the regular on the phone |
| drop_off | `en_US-norman-medium` | male, American — the walk-in customer |

All three models are public-domain, trained from scratch. Full licence audit, including the voices
that were rejected as non-commercial: `ArtSource/Licenses/piper-tts.md`.

## How to re-render

The engine lives in `Tools/Piper/` (git-ignored, ~376 MB). If it is missing, re-install from the
URLs in `ArtSource/Licenses/piper-tts.md` §1–2 and extract with `tar -xf` (not an editor-embedded
unzip) so the full `espeak-ng-data/lang/` tree lands.

```powershell
# all 54 lines (rewrites Draft/ and rendering-report.csv)
python Tools\Voice\render_dialogue.py

# one archetype
python Tools\Voice\render_dialogue.py --archetype attendant

# one line — this is the command to re-render a single clip
python Tools\Voice\render_dialogue.py --only attendant_enter_01

# validate what is already on disk without re-rendering
python Tools\Voice\render_dialogue.py --report-only
```

The single line above resolves to:

```
Tools\Piper\piper\piper.exe --model "Tools\Piper\voices\en_GB-cori-high.onnx" ^
  --json-input --sentence_silence 0.35 --length_scale 1.02
```
with `{"text": "...", "output_file": "ArtSource/Voice/Draft/attendant/attendant_enter_01.wav"}` on stdin.
Piper takes its text on **stdin**; a positional text argument is silently ignored.

Per-archetype prosody knobs are in `ARCHETYPES` in `Tools/Voice/render_dialogue.py`
(`length_scale` 1.02 / 0.98 / 1.00). Only change them with a listen test, then re-render all lines of
that archetype so the character stays consistent.

## What the render step does and does not do

Applied:

- Text normalisation that does not change the words: em dash → comma, curly quotes → straight,
  whitespace collapse. (`normalise_text()` in the script.)
- Uniform **−1 dB headroom trim** (`GAIN = 0.90`) on every file. One constant for all 54 clips, so
  relative character levels are preserved — this is not per-file loudness normalisation, it is a
  guarantee that nothing clips.

Deliberately **not** done:

- **No listening test.** Nobody has heard these. Naturalness, pronunciation of Balinese names
  ("Nyepi"), pacing, and whether the three voices are *distinguishable enough* are **unverified**.
  Treat every clip as unproven until a human listens.
- **No normalisation to a target loudness** (e.g. −16 LUFS). Peaks sit at −0.9 dBFS and RMS around
  −16 dBFS, which is a sane range, but there is no LUFS measurement because no meter or ffmpeg
  exists on this machine.
- **No trimming of leading/trailing silence**, no de-essing, no EQ, no compression, no de-click.
  Piper emits a short natural tail; nothing was cut.
- **No conversion to Vorbis/`.ogg`.** Decision 0008 §6 proposes OGG for shipping; these are WAV
  drafts. Conversion needs a tool that is not installed.
- **No Unity import.** No `.meta` files, no `AudioClip` references, no `DialogueLineSet`
  ScriptableObject, no `AudioSource` wiring. Copying into
  `Assets/LaundryLegends/Audio/Voice/{attendant,regular,dropoff}/` is a separate, reviewed step
  (decision 0010: only reviewed clips move into `Assets/`).
- **No lip sync**, no subtitles binding, no mix bus / 3D AudioSource setup.
- **No Indonesian voice in the shipped set** — see the finding below.

## Validation performed

`rendering-report.csv` has one row per clip: `file, archetype, trigger, text, duration_s, peak,
peak_dbfs, rms_dbfs, status`. Every row was produced by reading the WAV bytes back with Python's
`wave` + `array` (no third-party audio library), checking:

- file exists and is a parseable WAV
- 1 channel, 16-bit, 22,050 Hz
- duration > 0.40 s
- peak > 0 (not silent)
- peak < 32767 (no clipping)

Result: **54 / 54 pass, 0 failures.** Durations 3.66 s – 6.08 s (mean 4.87 s), so every line is
inside the ≤ 8 s readability budget from decision 0008. That is a *mechanical* pass only; it says
nothing about whether any line sounds good.

## Finding: Indonesian voice

An `id_ID` Piper voice does exist (`id_ID-news_tts-medium`), which is the only Indonesian voice in
the catalogue. It is **not usable for shipping**: its dataset licence is unverified ("See URL"), and
it is fine-tuned from the research-only Lessac base. Two audition clips were rendered with it anyway
in `Samples/id_ID/` (same English text, Indonesian phonemisation) purely so a human can judge whether
the accent is worth chasing through a properly licensed route.

So the shipped candidate set is **neutral English, not Balinese-accented**. That is a limitation, not
a defect — recorded in `ArtSource/Licenses/piper-tts.md` §3.

## Next steps before this becomes an asset

1. Human listen: one clip per archetype, then the longest lines (`phone_regular_enter_03` 6.08 s).
2. Confirm the three voices read as three different people at gameplay distance.
3. Decide `en_US-norman-medium` (the only medium-quality voice here) is acceptable, or replace it
   with a `en_US-libritts-high` speaker.
4. Copy approved clips into `Assets/LaundryLegends/Audio/Voice/` and convert to OGG if decision 0008
   §6 stands.
5. Update `ASSET_INVENTORY.md` and the credits screen when clips are actually imported.
