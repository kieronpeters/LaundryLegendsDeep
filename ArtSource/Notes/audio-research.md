# Audio Research — Background Music for a Cozy Bali Laundromat

**Date:** 2026-10-31
**Author:** research subagent
**Scope:** Research only. No project file was modified, nothing was installed, and no audio was downloaded.
**Status:** Unverified by ear. All "calm / clap-free" assessments come from publisher catalogue metadata, not listening tests.

---

## 1. Local audio tool inventory

### 1.1 PATH check

Command (PowerShell):

```powershell
$tools = 'ffmpeg','ffprobe','sox','audacity','python','python3','py'
foreach ($t in $tools) { $c = Get-Command $t -ErrorAction SilentlyContinue
  if ($c) { "FOUND: $t -> $($c.Source)" } else { "NOT IN PATH: $t" } }
```

Result:

| Tool | Status |
|---|---|
| `ffmpeg` | **Not in PATH** |
| `ffprobe` | **Not in PATH** |
| `sox` | **Not in PATH** |
| `audacity` | **Not in PATH** |
| `python` | **Found** — `C:\Users\Asus\AppData\Local\Programs\Python\Python315\python.exe` |
| `python3` | Found — `C:\Users\Asus\AppData\Local\Microsoft\WindowsApps\python3.exe` (Store alias) |
| `py` | Found — `C:\WINDOWS\py.exe` |
| `curl.exe` | **Found** — `C:\WINDOWS\system32\curl.exe`, curl 8.22.0 |

### 1.2 Disk scan

Scanned `%ProgramFiles%`, `%ProgramFiles(x86)%`, `%LOCALAPPDATA%\Programs`, `C:\ProgramData\chocolatey\bin`,
`C:\Users\Asus\scoop\shims`, and the WinGet package cache for `ffmpeg`, `ffprobe`, `sox`, `audacity`, `lame`, `reaper`.

| Finding | Path |
|---|---|
| OBS Studio (bundles libav libraries, **no ffmpeg CLI**) | `C:\Program Files\obs-studio` |
| Python install tree | `C:\Users\Asus\AppData\Local\Programs\Python` |
| No ffmpeg/ffprobe/sox/audacity binaries anywhere on those roots | — |

### 1.3 Python audio libraries

Command: `python --version` → **Python 3.15.0**
Command: `python -m pip list | Select-String "mutagen|pydub|soundfile|numpy|scipy|librosa"`

Only `numpy 2.5.4`. **No mutagen, pydub, soundfile, librosa, or scipy.**

### 1.4 Practical consequence

There is **no audio-analysis or audio-editing CLI on this machine**. MP3 metadata in this report was obtained by
parsing MPEG frame headers directly in Python with an inline script (no files created, no packages installed).
If trimming, crossfading, loudness normalisation, or clap removal is ever required, one of these must be approved first:

- `ffmpeg` (best single tool: trim, fade, crossfade, loudnorm, format conversion), or
- `mutagen` / `pydub` + `ffmpeg` backend (Python route), or
- Audacity (GUI, manual).

---

## 2. Existing music in the project

Files inspected read-only:

- `Assets/LaundryLegends/Audio/Music/BalinesePrelude.mp3`
- `Assets/LaundryLegends/Audio/Music/TempleOfHangzhou.mp3`

### 2.1 Method

No ffprobe was available, so both files were parsed with an inline Python script that:

1. skips the ID3v2 tag (big-endian syncsafe size),
2. walks every MPEG frame, validating sync word, version, layer, bitrate index and sample-rate index,
3. accumulates `samples_per_frame / sample_rate` per frame for exact duration, and
4. derives average bitrate from parsed frame bits.

```powershell
python - "Assets\LaundryLegends\Audio\Music\BalinesePrelude.mp3" "Assets\LaundryLegends\Audio\Music\TempleOfHangzhou.mp3"
```

### 2.2 Results

| Property | BalinesePrelude.mp3 | TempleOfHangzhou.mp3 |
|---|---|---|
| Size | 4,804,506 bytes | 4,467,025 bytes |
| Container | MPEG-1 Layer III | MPEG-1 Layer III |
| ID3 | ID3v2.3 present | ID3v2.3 present |
| Sample rate | 44,100 Hz | 44,100 Hz |
| Channels | 2 (Joint Stereo) | 2 (Stereo) |
| Bitrate | 320 kbps CBR | 192 kbps CBR |
| Frames parsed | 3,223 | 4,835 |
| **Duration** | **84.19 s (1:24)** | **126.30 s (2:06)** |
| Samples per frame | 1,152 | 1,152 |

Both files decode cleanly (every frame header valid, no corrupt-frame gaps detected in the frame walk).

### 2.3 Observations relevant to the complaint

- `BalinesePrelude.mp3` is only **1:24**. A loop shorter than ~2 minutes repeats noticeably in a slow-paced shop
  sim, which makes any percussive figure in it far more obvious.
- The reported clapping/percussion artifact cannot be confirmed or located without a listening pass and without a
  spectral tool (no ffmpeg means no spectrogram, no click detection). **This remains an owner-reported observation.**
- `TempleOfHangzhou.mp3` is 2:06 at 192 kbps, already closer to a workable loop length.
- Both are CC BY 4.0 by the same artist, so the licence note pattern already exists
  (`ArtSource/Licenses/balinese-laundry-music.md`) and can be cloned for replacements.

---

## 3. Source research

### 3.1 Source-by-source findings

| Source | Verdict | Detail |
|---|---|---|
| **incompetech** (Kevin MacLeod) | **Best source** | ~1,443-track JSON catalogue with `title`, `filename`, `length`, `instruments`, `feel`, `bpm`, `description`. Direct MP3 URL pattern works with a plain GET, no account, no cookies. All tracks CC BY 4.0. |
| **Free Music Archive** | Usable, with a caveat | `/track/<slug>/download/` now **302s to `/login`** — FMA requires a free account for its official download button. However the public track/album HTML embeds `"fileUrl"` pointing at `files.freemusicarchive.org/storage-...`, which serves the file to an anonymous GET (HTTP 200). Track pages are JS-rendered, so no direct `<a href>` to the MP3 exists — the URL must be lifted from the embedded JSON. |
| **OpenGameArt** | Usable, but thin | Direct file hosting under `opengameart.org/sites/default/files/...`, no account. **Zero results for `gamelan` and `bali`** in the music category. CC0 ambient loops exist but are short. |
| **Pixabay** | **Unusable** | `pixabay.com/music/search/gamelan/` returns **HTTP 403** to a plain client, and `cdn.pixabay.com/download/audio/...` also returns **403**. Requires a browser session/account. Pixabay's Content License also restricts standalone redistribution of the audio itself. Excluded. |
| **FMA real gamelan field recordings** | **Excluded on licence** | Verified two: Gamelan Nyai Saraswati "Ladrang JURUDEMUNG" = **CC BY-NC 3.0 US**, Arrington de Dionyso / Gamelan Gandrun Lombok "Gamelan Mandalika" = **CC BY-NC-ND 3.0 US**. Both violate the "no NC, no ND" requirement. |
| **archive.org** | Lead only, not verified | Hosts many gamelan LPs and field recordings, but per-item rights vary wildly (PD, CC BY, "no known copyright", or unclear). **Not listed as a candidate** until a specific item's rights statement is read. |

### 3.2 Licence terms captured

- **incompetech** — CC BY 4.0, attribution required unless a paid no-attribution licence is bought.
  Credit format from the site's licence generator:
  `"<Track>" Kevin MacLeod (incompetech.com) — Licensed under Creative Commons: By Attribution 4.0 — https://creativecommons.org/licenses/by/4.0/`
- **Free Music Archive / Geoff Harvey – Purple Planet Music** — "Attribution 4.0 International License",
  `creativecommons.org/licenses/by/4.0`, confirmed in the album page markup (`by/4.0` present 3×).
  Same artist and licence as the two tracks already shipped, so credit wording can be reused.
- **OpenGameArt** — CC0 / Public Domain where noted; no attribution obligation.

---

## 4. Verified candidates

Verification commands used:

```powershell
# status + size + type (HEAD, follows redirects)
curl.exe -sSIL --max-time 35 -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" "<URL>"

# byte-range probe (confirms real audio bytes without downloading the whole file)
curl.exe -sS -o NUL -r 0-1023 --max-time 30 -A "Mozilla/5.0" "<URL>"
```

All rows below returned **HTTP 200 on HEAD** and **HTTP 206** on the byte-range probe, with a
`Content-Range` total matching the reported size. All files start with a valid MP3 signature
(`ID3` / `0xFF 0xFB`) or `OggS`; the FMA `.bin` URLs are real MP3s despite the extension.

| # | Title | Artist | Licence | Duration | Direct URL | HTTP | Size |
|---|---|---|---|---|---|---|---|
| 1 | That Zen Moment | Kevin MacLeod | CC BY 4.0 | 10:02 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/That%20Zen%20Moment.mp3` | 200 | 12,038,401 B (11.5 MB) |
| 2 | Kalimba Relaxation Music | Kevin MacLeod | CC BY 4.0 | 7:08 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/Kalimba%20Relaxation%20Music.mp3` | 200 | 17,257,632 B (16.5 MB) |
| 3 | Organic Meditations Two | Kevin MacLeod | CC BY 4.0 | 15:55 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/Organic%20Meditations%20Two.mp3` | 200 | 38,206,610 B (36.4 MB) |
| 4 | Ripples | Kevin MacLeod | CC BY 4.0 | 3:25 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/Ripples.mp3` | 200 | 8,220,237 B (7.8 MB) |
| 5 | Infinite Perspective | Kevin MacLeod | CC BY 4.0 | 5:45 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/Infinite%20Perspective.mp3` | 200 | 11,044,756 B (10.5 MB) |
| 6 | Wholesome | Kevin MacLeod | CC BY 4.0 | 6:04 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/Wholesome.mp3` | 200 | 14,556,390 B (13.9 MB) |
| 7 | Silver Flame | Kevin MacLeod | CC BY 4.0 | 6:36 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/Silver%20Flame.mp3` | 200 | 12,662,240 B (12.1 MB) |
| 8 | Cattails | Kevin MacLeod | CC BY 4.0 | 2:39 | `https://incompetech.com/music/royalty-free/mp3-royaltyfree/Cattails.mp3` | 200 | 6,365,435 B (6.1 MB) |
| 9 | Oriental Skies | Geoff Harvey – Purple Planet Music | CC BY 4.0 | ~3:35 (est.) | `https://files.freemusicarchive.org/storage-freemusicarchive-org/tracks/pOQehwiZHmC5LJ9uxzv5mTK94zwXJteQ2Y2i3s2L.bin` | 200 | 6,590,467 B (6.3 MB) |
| 10 | The Rice Fields | Geoff Harvey – Purple Planet Music | CC BY 4.0 | ~3:03 (est.) | `https://files.freemusicarchive.org/storage-freemusicarchive-org/tracks/Il8xeZrccXxw8HGWmuyDHQRFbh6GkIiSU6T0ZtuF.bin` | 200 | 5,819,332 B (5.5 MB) |
| 11 | Flowers of Osaka | Geoff Harvey – Purple Planet Music | CC BY 4.0 | ~2:44 (est.) | `https://files.freemusicarchive.org/storage-freemusicarchive-org/tracks/XZ1eLlWwxihxqlc1hA7HpSyUMymNGZuQzW5TDHlq.bin` | 200 | 5,368,308 B (5.1 MB) |
| 12 | Silk Road | Geoff Harvey – Purple Planet Music | CC BY 4.0 | ~2:35 (est.) | `https://files.freemusicarchive.org/storage-freemusicarchive-org/tracks/77YW2ocFbP2VIjXLzqxUcirE7SafioUaFoALB8o1.bin` | 200 | 5,156,031 B (4.9 MB) |
| 13 | Ambient Loop (isaiah658) | isaiah658 | CC0 | short loop, unmeasured | `https://opengameart.org/sites/default/files/Ambient-Loop-isaiah658_0.ogg` | 200 | 1,284,993 B (1.2 MB) |

Duration key: incompetech durations are publisher-declared exact lengths (`hh:mm:ss` in `pieces.json`) and match the
byte sizes at the detected bitrate. FMA durations are **estimated** from `(size − ID3 tag) × 8 / bitrate` on a CBR
stream (ID3 tags there are ~1.4 MB of artwork); treat as ±1 s.

Additional verified negative: OpenGameArt `Relaxing_0.mp3`
(`https://opengameart.org/sites/default/files/Relaxing_0.mp3`, CC0, HTTP 200, 314,222 B) responds but is far too
short for background use.

### 4.1 Technical detail for the strongest candidates

| Track | Bitrate | Sample rate | Channels | Percussion risk (from catalogue metadata) |
|---|---|---|---|---|
| That Zen Moment | 160 kbps | 44,100 Hz | Joint Stereo | **Low** — instruments: "Strings, Bowl Drum" (metal tongue drum); feel Calming/Mystical/Relaxed |
| Ripples | 320 kbps | 44,100 Hz | Stereo | **Low** — solo Koto, feel Calming/Mystical/Relaxed |
| Organic Meditations Two | 320 kbps | 44,100 Hz | Joint Stereo | **Low** — Tuba, French Horns, Marimba; "Very long, very calming" |
| Infinite Perspective | 256 kbps | 44,100 Hz | Stereo | **Low** — Synths, Voices, Piano, Harp, Horns, Flute |
| Wholesome | 320 kbps | 44,100 Hz | Stereo | **Medium** — includes Drums |
| Silver Flame | 256 kbps | 44,100 Hz | Stereo | **Medium** — includes Drums |
| Cattails | 320 kbps | 44,100 Hz | Joint Stereo | **Low–Medium** — acoustic guitar/banjo/erhu, no drum kit listed |

Note: "Asian Drums" (2:19, CC BY 4.0) was found and **deliberately rejected** — it is percussion-only and
described as "dark, eerie, suspenseful, unnerving". `Ujangong Mix` was also rejected (drum kit, "intense, unnerving").

---

## 5. What is downloadable without an account

| Source | No account needed? | Notes |
|---|---|---|
| incompetech direct MP3 URLs | **Yes** | Clean GET, returns `application/octet-stream`, honours byte ranges, no cookies or referer required. |
| FMA `files.freemusicarchive.org` storage URLs | **Yes, technically** | Anonymous GET returns 200. But FMA's official `/download/` route redirects to `/login`, and these storage URLs are undocumented internal links that could change without notice. Treat as fragile. |
| OpenGameArt `sites/default/files` | **Yes** | Plain GET, correct `audio/ogg` / `audio/mpeg` types. |
| Pixabay | **No** | 403 without a browser session/subscription flow. |
| FMA real gamelan recordings | n/a | Correctly downloadable, but **licences are BY-NC / BY-NC-ND**, so unusable regardless of access. |

Mirroring note: because incompetech files are served as `application/octet-stream` and FMA storage URLs are
fragile, any chosen track should be downloaded (once approved) and committed as a local asset, with the licence
note recorded under `ArtSource/Licenses/` — the same pattern already used for `BalinesePrelude.mp3`.

---

## 6. Shortlist — three best options

Ranked for the actual problem: longer than the current 1:24 loop, no clap-like percussion, calm, gamelan-adjacent
or warm/acoustic, licence-clean, no account.

### 1. That Zen Moment — Kevin MacLeod — CC BY 4.0 — 10:02
`https://incompetech.com/music/royalty-free/mp3-royaltyfree/That%20Zen%20Moment.mp3`

The single best fit. A ten-minute meditation piece built on a round metal tongue drum and strings — metallic,
bell-like, gamelan-adjacent in timbre, and described by the composer as "calming, mystical, relaxed". No drum kit,
no hand claps, no shakers. At 10:02 it can loop twice an hour with no audible seam and is the direct answer to the
"clap artifact" complaint. 160 kbps, 44.1 kHz stereo, 11.5 MB — smallest file of the three, which matters for a
Unity repo.

### 2. Ripples — Kevin MacLeod — CC BY 4.0 — 3:25
`https://incompetech.com/music/royalty-free/mp3-royaltyfree/Ripples.mp3`

Solo Chinese guzheng/koto, explicitly written as "serene… for relaxation and meditation… Asian-themed content".
Percussion-free by definition (single plucked instrument), so it cannot introduce a clap artifact. Shorter at 3:25
and more melodic than ambient — best used as a second rotation track rather than a sole loop. 320 kbps, 7.8 MB.

### 3. Oriental Skies — Geoff Harvey (Purple Planet Music) — CC BY 4.0 — ~3:35
`https://files.freemusicarchive.org/storage-freemusicarchive-org/tracks/pOQehwiZHmC5LJ9uxzv5mTK94zwXJteQ2Y2i3s2L.bin`

Stays in the exact same album, artist and licence as the two tracks already shipped, so the existing credit line
and licence note extend naturally and the soundtrack keeps one coherent voice. Longest of the verified Eastern
Horizons tracks (most of that album's 26 tracks are only 68–117 s, so this is the outlier). Caveat: instruments are
not published, the percussion content is unknown, and the URL is an internal storage link — it needs a listening
check before adoption.

**Runner-up if a genuinely gamelan-flavoured bed is required:** `Organic Meditations Two` (15:55, CC BY 4.0,
marimba/horns/tuba) — mallet-led and very long, but Western instrumentation rather than Balinese.

---

## 7. What was not done

- No audio was downloaded to the project. Only HTTP HEAD requests and ≤512 KB byte-range probes were made.
- No track was listened to. Every "calm", "no claps" and "gamelan-adjacent" claim is catalogue metadata, not audition.
- No spectral or waveform analysis was possible (no ffmpeg/sox/mutagen installed), so the original clap artifact in
  `BalinesePrelude.mp3` was neither reproduced nor located.
- Durations for OpenGameArt items and FMA est. rows were not frame-verified.
- archive.org gamelan holdings were left as an unverified lead.
- Nothing was installed and no project file outside this document was touched.

## 8. Suggested next step (requires owner approval)

1. Download `That Zen Moment` and `Ripples` with a plain `curl.exe` GET into a scratch folder, listen to both
   end-to-end, and confirm no clap-like percussion.
2. Only if a gamelan-specific bed is still wanted, audition `Oriental Skies` from the same album.
3. On approval: add the chosen file(s) under `Assets/LaundryLegends/Audio/Music/`, write
   `ArtSource/Licenses/<track>.md` with the CC BY 4.0 credit, update the in-scene music source and its
   `AudioClip` reference, and add the credit line to the future credits screen.
4. If loop trimming or a crossfade is needed, request approval for `ffmpeg` first — it is not installed.
