# Piper TTS — engine and voice licences

- **Recorded:** 2026-10-31
- **Workstream:** W6 (local open-weight TTS)
- **Owner approval:** installing open-weight TTS software on this machine was approved.
- **Scope:** everything below was used **locally, on this machine, to generate audio files**. No
  engine binary or model weights are part of the game build, and none of them were committed.

## 1. Engine

| Field | Value |
|---|---|
| Project | Piper (`rhasspy/piper`). Note: the project was later relicensed GPL-3.0 as `OHF-Voice/piper1-gpl`; this is the **MIT-era release**. |
| Version | `2023.11.14-2` (latest release of the MIT-era repo) |
| Release page | https://github.com/rhasspy/piper/releases/tag/2023.11.14-2 |
| Download | https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip |
| Size / SHA-256 | 22,477,236 bytes / `f3c58906402b24f3a96d92145f58acba6d86c9b5db896d207f78dc80811efcea` |
| Installed path | `Tools/Piper/piper/piper.exe` (portable, no admin, no PATH change, no installer) |
| Engine licence | **MIT** (repo `LICENSE.md` at that tag) |

Bundled runtime components inside the zip, with their own licences:

| Component | Licence | Note |
|---|---|---|
| `piper_phonemize.dll` | MIT | Piper's phonemizer wrapper |
| `espeak-ng.dll` + `espeak-ng-data/` | **GPL-3.0-or-later** | GPL engine code, used only as a build-time tool here |
| `onnxruntime.dll` | MIT | inference runtime |

**Why GPL espeak inside a MIT tool is not a problem for this game:** the WAV files are output of
the program, not a derivative work of it. GPL obligations would attach to *redistributing the
engine*, which we do not do — the engine stays in `Tools/Piper/` and is git-ignored. If the project
ever ships a runtime TTS engine inside a build, that decision needs a fresh licence review.

## 2. Voices used for the draft renders

All three were chosen because the model card states the voice was **trained from scratch** on
public-domain data. That matters: a large share of Piper's English voices are *fine-tuned from the
Lessac Blizzard 2013 base*, whose licence forbids commercial use (see §4).

| Archetype | Voice | Model card licence | Dataset | Hash / size |
|---|---|---|---|---|
| Attendant | `en_GB-cori-high` | **public domain** | LibriVox recordings, ~24 h, trained from scratch (500 epochs) | `470b4dd6…a5903`, 114,219,352 B |
| Phone regular | `en_US-ljspeech-high` | **public domain** | LJ Speech Dataset, trained from scratch (1000 epochs) | `5d4f08ba…c029a`, 114,199,011 B |
| Drop-off customer | `en_US-norman-medium` | **public domain** | LibriVox recordings, ~15.5 h, trained from scratch (1200 epochs) | `b9739443…1cb71`, 63,531,379 B |

Model card URLs (these are the authoritative licence statements; the `voices.json` index carries no
licence field):

- https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/cori/high/MODEL_CARD
- https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ljspeech/high/MODEL_CARD
- https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/norman/medium/MODEL_CARD

Download base: `https://huggingface.co/rhasspy/piper-voices/resolve/main/<path>.onnx` (+ `.onnx.json`).

Attribution: **not required** — the source audio is public domain and the voice datasets are not
CC-BY. LibriVox recordings are dedicated to the public domain; the readers are volunteers. No
attribution line is owed to a dataset author, but the provenance above is recorded anyway so the
claim is checkable.

### Note on the voices being synthesised, not cloned

These are trained model weights, not recordings of a named person we are imitating. Neither the
LJ Speech speaker nor LibriVox readers are credited as characters; the game ships synthetic audio,
and voice credits should list the model, not a person.

## 3. Indonesian voice — exists, but is **not** licence-clean

A `id_ID` voice does exist: `id_ID-news_tts-medium` (`https://huggingface.co/rhasspy/piper-voices/resolve/main/id/id_ID/news_tts/medium/MODEL_CARD`).
It was **downloaded for audition and deliberately not used for shippable output**:

| Issue | Detail |
|---|---|
| Dataset licence unstated | MODEL_CARD says `License: See URL` pointing at a Kaggle "indic-tts" corpus page. A commercial-use grant could not be verified. |
| Inherited research-only base | MODEL_CARD: "Finetuned from U.S. English lessac voice (medium quality)". The Lessac/Blizzard 2013 base is research-only (see §4), so the finetuned model inherits that restriction. |
| Text mismatch | The voice's espeak language is `id`, so English dialogue is phonemised with Indonesian G2P rules. That may be desirable for accent colour, but intelligibility of English words is unverified without a human listen. |

Two audition renders were produced with it anyway, in `ArtSource/Voice/Samples/id_ID/`, so the owner
can hear what the accent would sound like. **They must not be shipped** unless the dataset licence
is confirmed to permit commercial use.

## 4. Voices rejected on licence grounds (recorded so nobody re-adds them)

Piper's per-voice MODEL_CARDs are the only reliable licence statement; the top-level index has none.

| Voice | Why rejected |
|---|---|
| `en_US-ryan-high` / `-medium` / `-low` | Dataset licence **CC BY-NC-SA 4.0** — non-commercial, and share-alike. |
| `en_US-hfc_female-medium`, `en_US-hfc_male-medium` | Dataset licence **CC BY-NC-SA 4.0** (Hi-Fi-CAPTAIN). |
| `en_GB-semaine-medium` | Dataset licence **CC BY-NC-SA 4.0**. |
| `en_US-l2arctic-medium` | Dataset licence **CC BY-NC 4.0** — non-commercial. |
| `en_US-lessac-*` (low/medium/high) | Blizzard 2013 Lessac data is under a **research-only** licence that explicitly excludes "development … of voice synthesis products or services". |
| `en_GB-vctk`, `en_GB-alba`, `en_GB-aru`, `en_GB-alan`, `en_US-joe`, `en_US-kusal`, `en_US-mike`, `en_US-sam`, `en_US-reza_ibrahim`, `en_GB-jenny_dioco`, `en_US-libritts_r`, `id_ID-news_tts` | Dataset licences are CC-BY/CC0/Apache — but each MODEL_CARD says "Finetuned from U.S. English lessac voice", so the weights are derived from the research-only Lessac base. Treated as unusable for a commercial build. |
| `en_GB-northern_english_male`, `en_GB-southern_english_female` | Dataset licence **CC BY-SA 4.0** — share-alike (and also Lessac-derived). |

The Blizzard 2013 licence text checked: https://www.cstr.ed.ac.uk/projects/blizzard/2013/lessac_blizzard2013/license.html
— "the User wishes to make use of the Materials for Research Purposes only", where Research Purposes
"excludes … using the Materials for any commercial purpose, including the development, marketing,
commercialisation, sale or licencing of voice synthesis … products or services".

`en_US-libritts-high` (CC BY 4.0, trained from scratch, 904 speakers) is the cleanest remaining
option if we later need more distinct voices; it needs a `--speaker` id chosen by listening, so it
was not used for this first pass.

`en_US-bryce-medium` and `en_US-john-medium` are public-domain/unreleased-base derivatives and may
be usable, but their provenance is less clearly documented than the three above.

## 5. Reproducing the renders

See `ArtSource/Voice/README.md` for the command lines. In short:

```
python Tools\Voice\render_dialogue.py                 # all 54 lines
python Tools\Voice\render_dialogue.py --only attendant_enter_01
```

Raw engine invocation for a single line (Piper reads the text from **stdin**, not argv):

```
Tools\Piper\piper\piper.exe ^
  --model "Tools\Piper\voices\en_GB-cori-high.onnx" ^
  --output_file "ArtSource\Voice\Draft\attendant\attendant_enter_01.wav" ^
  --sentence_silence 0.35 --length_scale 1.02 < line.txt
```

## 6. Pitfalls hit while installing (for the next agent)

- The MIT-era Windows zip **only works if the zip is extracted completely**. An interrupted
  extraction left `espeak-ng-data/lang/gmw/` (the English voice definitions) missing; `piper.exe`
  then died with `0xC0000409` (`STATUS_STACK_BUFFER_OVERRUN`) inside phonemisation with no useful
  error. Extract with `tar -xf` and verify `Tools/Piper/piper/espeak-ng-data/lang/gmw/en` exists
  before concluding the engine is broken.
- The newer `piper1-gpl` 1.8.0 Windows wheel is `cp39-abi3` and would import on Python 3.15, but it
  requires `onnxruntime`, which has **no cp315 wheel** (latest 1.31.1 ships cp311–cp314 only). The
  Python route is blocked on this machine; the standalone binary is the working path.
- Piper 2023.11.14-2 takes text on **stdin**; a positional text argument is silently ignored and
  produces no output.
