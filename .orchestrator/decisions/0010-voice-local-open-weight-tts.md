# Decision 0010 — Voice rendering: local open-weight TTS

- **Date:** 2026-11-01
- **Status:** accepted (owner decision)
- **Related:** decision `0008-voice-and-dialogue-plan.md`, workstream W6

## Decision

The owner chose **local open-weight TTS** over paid cloud TTS and subtitle-only
dialogue. Voice is generated on this machine, committed as audio assets, and
attached per line; subtitles remain the source of truth and render even when a
clip is missing.

## Consequences

- No recurring cost and no external service dependency.
- Licence cleanliness depends on the **voice model**, not just the engine. Each
  downloaded voice model must permit commercial use (CC0, CC-BY, MIT, public
  domain). Non-commercial voices are excluded.
- Quality needs a human listening pass; the agent cannot judge naturalness.
- Indonesian (`id_ID`) is preferred for setting authenticity. If the chosen
  engine has no Indonesian voice, that is recorded as a limitation rather than
  silently substituted.
- Machine constraint: Python 3.15 with no PyTorch wheels, so PyTorch-based
  stacks (Kokoro, MeloTTS) are fallbacks only. A standalone binary (Piper) is
  the primary route.

## Rules

- Draft renders live in `ArtSource/Voice/Draft/` until reviewed; only reviewed
  clips are copied into `Assets/LaundryLegends/Audio/Voice/`.
- Every engine and voice model gets a licence record in `ArtSource/Licenses/`.
- Generation must be reproducible: the exact command line is documented.
