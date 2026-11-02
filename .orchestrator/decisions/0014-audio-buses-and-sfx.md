# 0014 - Audio buses, procedural cues and settings

**Status:** Accepted 2026-11-04
**Phase:** 13

## Context

No dedicated audio-generation tool is verified. The project already ships CC0/CC-BY
music and Piper voice clips, but machine, interaction and UI cues are missing. An
AudioMixer asset would need a new authoring surface; the game only needs three
volume buses and a handful of short cues.

## Decision

- Use three logical buses: **Master**, **Music** (music + ambience), **Effects**.
- Persist each in `PlayerPrefs` under `ll.audio.master|music|sfx`; clamp 0..1.
- Apply Master to `AudioListener.volume`; apply Music/Effects to the sources owned
  by `GameAudio`.
- Generate short SFX and the two ambience beds in code (`ProceduralSfx`) so the
  build has audible feedback without a new asset dependency.
- One `GameAudio` director auto-creates on first use, survives scene loads, owns
  one-shot SFX plus laundry and arcade ambience loops, and crossfades the beds
  when `ArcadeMode` is active.
- The pause menu exposes Master/Music/Effects sliders and a reset.
- A native `AudioMixer` asset and authored SFX remain future polish; the bus
  contract above must not change without a new decision.

## Consequences

- No package or project-setting change.
- Cues are intentionally synthetic; a later audio pass can replace the clips
  behind the same `Sfx` enum.
- Volume settings survive restarts without the Phase 14 save file.
