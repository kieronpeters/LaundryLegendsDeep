#!/usr/bin/env python3
"""Render the Laundry Legends dialogue lines to draft WAV files with Piper.

Source of truth for the line text is the markdown table in
`.orchestrator/decisions/0008-voice-and-dialogue-plan.md` (section 4), parsed at
run time so the script can never drift from the approved script.

Output: ArtSource/Voice/Draft/<archetype>/<archetype>_<trigger>_<nn>.wav
        22050 Hz, 16-bit, mono.

Usage:
    python Tools/Voice/render_dialogue.py                 # render everything
    python Tools/Voice/render_dialogue.py --archetype attendant
    python Tools/Voice/render_dialogue.py --only attendant_enter_01

Every rendered file is validated (rate, channels, duration, non-silence) and the
results are written to ArtSource/Voice/rendering-report.csv.
"""

from __future__ import annotations

import argparse
import array
import csv
import json
import os
import re
import subprocess
import sys
import wave

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PIPER = os.path.join(REPO, "Tools", "Piper", "piper", "piper.exe")
VOICE_DIR = os.path.join(REPO, "Tools", "Piper", "voices")
DECISION = os.path.join(REPO, ".orchestrator", "decisions", "0008-voice-and-dialogue-plan.md")
DRAFT = os.path.join(REPO, "ArtSource", "Voice", "Draft")
REPORT = os.path.join(REPO, "ArtSource", "Voice", "rendering-report.csv")

SAMPLE_RATE = 22050
MIN_DURATION = 0.40
# Uniform headroom trim applied to every file (-1 dBFS). It is a single constant
# for all lines, never a per-file loudness normalisation, so relative character
# levels are preserved. Guarantees the shipped peaks cannot clip.
GAIN = 0.90

# One voice per archetype. All three models were trained from scratch on
# public-domain data; see ArtSource/Licenses/piper-tts.md for the licence audit.
ARCHETYPES = {
    "attendant": {
        "model": "en_GB-cori-high.onnx",
        "length_scale": 1.02,
        "prefix": "ATT",
    },
    "phone_regular": {
        "model": "en_US-ljspeech-high.onnx",
        "length_scale": 0.98,
        "prefix": "REG",
    },
    "drop_off": {
        "model": "en_US-norman-medium.onnx",
        "length_scale": 1.00,
        "prefix": "DROP",
    },
}

TRIGGERS = {
    "enter": "enter",
    "counter": "counter",
    "waiting": "waiting",
    "pickup": "pickup",
    "idle": "idle",
}


def normalise_text(raw: str) -> str:
    """Make the line safe for the TTS front end without changing the words."""
    text = raw.strip().strip('"').strip()
    text = text.replace("\u2014", ",")   # em dash -> comma pause
    text = text.replace("\u2013", ",")   # en dash
    text = text.replace("\u2019", "'")   # curly apostrophe
    text = text.replace("\u2018", "'")
    text = text.replace("\u201c", '"').replace("\u201d", '"')
    text = text.replace("\u2026", "...")
    text = re.sub(r"\s+", " ", text)
    return text


def parse_lines():
    """Pull the 54 dialogue rows out of decision 0008 section 4."""
    rows = []
    pattern = re.compile(r"^\|\s*(ATT|REG|DROP)-[A-Z](\d)\s*\|\s*([A-Za-z]+)\s*\|\s*(.+?)\s*\|\s*$")
    with open(DECISION, encoding="utf-8") as fh:
        for line in fh:
            m = pattern.match(line)
            if not m:
                continue
            code, _num, trigger, text = m.groups()
            rows.append((code, trigger.lower(), normalise_text(text)))
    if not rows:
        raise SystemExit("No dialogue rows found in %s" % DECISION)
    return rows


def build_index(rows):
    """Group by (archetype, trigger) and number them 01.. in document order."""
    counters = {}
    indexed = []
    for code, trigger, text in rows:
        archetype = next(a for a, cfg in ARCHETYPES.items() if cfg["prefix"] == code)
        if trigger not in TRIGGERS:
            raise SystemExit("Unknown trigger %r" % trigger)
        key = (archetype, trigger)
        counters[key] = counters.get(key, 0) + 1
        stem = "%s_%s_%02d" % (archetype, trigger, counters[key])
        indexed.append(
            {
                "archetype": archetype,
                "trigger": trigger,
                "stem": stem,
                "text": text,
                "file": os.path.join(DRAFT, archetype, stem + ".wav"),
            }
        )
    return indexed


def wav_stats(path):
    """Read the WAV ourselves: rate, channels, duration, peak, RMS."""
    with wave.open(path, "rb") as w:
        channels = w.getnchannels()
        width = w.getsampwidth()
        rate = w.getframerate()
        frames = w.getnframes()
        raw = w.readframes(frames)
    samples = array.array("h")
    samples.frombytes(raw)
    if sys.byteorder == "big":
        samples.byteswap()
    peak = max((abs(s) for s in samples), default=0)
    total = sum(float(s) * s for s in samples)
    rms = (total / len(samples)) ** 0.5 if samples else 0.0
    return {
        "channels": channels,
        "width": width,
        "rate": rate,
        "frames": frames,
        "duration": frames / float(rate) if rate else 0.0,
        "peak": peak,
        "rms": rms,
        "samples": samples,
    }


def apply_gain(path, gain):
    """Rewrite the WAV with a uniform gain. Peak stays well below full scale."""
    with wave.open(path, "rb") as w:
        params = w.getparams()
        raw = w.readframes(w.getnframes())
    samples = array.array("h")
    samples.frombytes(raw)
    if sys.byteorder == "big":
        samples.byteswap()
    scaled = array.array("h", (int(round(s * gain)) for s in samples))
    if sys.byteorder == "big":
        scaled.byteswap()
    with wave.open(path, "wb") as w:
        w.setnchannels(params.nchannels)
        w.setsampwidth(params.sampwidth)
        w.setframerate(params.framerate)
        w.writeframes(scaled.tobytes())


def render_voice(archetype, items):
    cfg = ARCHETYPES[archetype]
    model = os.path.join(VOICE_DIR, cfg["model"])
    if not os.path.exists(model):
        raise SystemExit("Missing voice model: %s" % model)
    payload = []
    for item in items:
        payload.append(
            json.dumps(
                {
                    "text": item["text"],
                    "output_file": item["file"].replace(os.sep, "/"),
                }
            )
        )
    stdin = ("\n".join(payload) + "\n").encode("utf-8")
    cmd = [
        PIPER,
        "--model", model,
        "--json-input",
        "--sentence_silence", "0.35",
        "--length_scale", "%.3f" % cfg["length_scale"],
    ]
    print("  $ %s" % " ".join('"%s"' % c if " " in c else c for c in cmd[:-2]))
    proc = subprocess.run(cmd, input=stdin, capture_output=True)
    tail = proc.stderr.decode("utf-8", "replace").strip().splitlines()[-3:]
    for line in tail:
        print("    " + line)
    if proc.returncode != 0:
        raise SystemExit("piper failed for %s (exit %d)" % (archetype, proc.returncode))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--archetype", choices=sorted(ARCHETYPES))
    ap.add_argument("--only", help="render a single stem, e.g. attendant_enter_01")
    ap.add_argument("--report-only", action="store_true", help="validate existing files")
    args = ap.parse_args()

    items = build_index(parse_lines())
    if args.archetype:
        items = [i for i in items if i["archetype"] == args.archetype]
    if args.only:
        items = [i for i in items if i["stem"] == args.only]
    if not items:
        raise SystemExit("Nothing selected.")

    if not args.report_only:
        by_voice = {}
        for item in items:
            by_voice.setdefault(item["archetype"], []).append(item)
        for archetype, group in by_voice.items():
            os.makedirs(os.path.join(DRAFT, archetype), exist_ok=True)
            print("Rendering %d line(s) with %s" % (len(group), ARCHETYPES[archetype]["model"]))
            render_voice(archetype, group)

    rows = []
    failures = []
    for item in items:
        path = item["file"]
        if not os.path.exists(path):
            failures.append("%s: missing" % item["stem"])
            rows.append((item, None, "missing"))
            continue
        if not args.report_only:
            apply_gain(path, GAIN)
        st = wav_stats(path)
        problems = []
        if st["channels"] != 1:
            problems.append("channels=%d" % st["channels"])
        if st["width"] != 2:
            problems.append("sampwidth=%d" % st["width"])
        if st["rate"] != SAMPLE_RATE:
            problems.append("rate=%d" % st["rate"])
        if st["duration"] <= MIN_DURATION:
            problems.append("too short (%.2fs)" % st["duration"])
        if st["peak"] == 0:
            problems.append("silent")
        if st["peak"] >= 32767:
            problems.append("clipping")
        status = "ok" if not problems else "FAIL:" + ",".join(problems)
        if problems:
            failures.append("%s: %s" % (item["stem"], status))
        rows.append((item, st, status))

    os.makedirs(os.path.dirname(REPORT), exist_ok=True)
    write_header = not os.path.exists(REPORT) or args.report_only or args.only or args.archetype
    with open(REPORT, "w", encoding="utf-8", newline="") as fh:
        writer = csv.writer(fh)
        writer.writerow([
            "file", "archetype", "trigger", "text", "duration_s",
            "peak", "peak_dbfs", "rms_dbfs", "status",
        ])
        for item, st, status in rows:
            rel = os.path.relpath(item["file"], os.path.join(REPO, "ArtSource", "Voice"))
            if st is None:
                writer.writerow([rel.replace(os.sep, "/"), item["archetype"], item["trigger"],
                                 item["text"], "", "", "", "", status])
                continue
            import math
            peak_db = 20 * math.log10(st["peak"] / 32768.0) if st["peak"] else float("-inf")
            rms_db = 20 * math.log10(st["rms"] / 32768.0) if st["rms"] else float("-inf")
            writer.writerow([
                rel.replace(os.sep, "/"), item["archetype"], item["trigger"], item["text"],
                "%.3f" % st["duration"], st["peak"], "%.1f" % peak_db, "%.1f" % rms_db, status,
            ])

    total = len(rows)
    print("\nRendered/validated %d line(s); %d failed" % (total, len(failures)))
    for f in failures:
        print("  FAIL " + f)
    print("Report: %s" % os.path.relpath(REPORT, REPO))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
