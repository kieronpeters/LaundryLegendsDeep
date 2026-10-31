"""Generate an original, royalty-free Balinese-inspired laundry loop.

Run:
  python ArtSource/Audio/generate_balinese_laundry.py

Outputs:
  Assets/LaundryLegends/Audio/Music/BaliLaundryLoop.wav

The piece uses a slendro-like five-note scale, metallic gamelan-style
partials, a soft pad, hand-drum thumps, and low room/rain noise. It is
synthesized from scratch, so the project owns the recording.
"""

import math
import os
import random
import struct
import wave


SAMPLE_RATE = 44100
DURATION = 64.0
OUTPUT = os.path.join(
    os.path.dirname(__file__),
    "..", "..", "Assets", "LaundryLegends", "Audio", "Music", "BaliLaundryLoop.wav",
)

# Slendro-like five-tone palette (approximate, not equal temperament).
SCALE = [220.0, 247.0, 294.0, 330.0, 392.0, 440.0, 494.0, 588.0]


def add_tone(buffer, start, duration, frequency, amplitude, decay, partials, phase=0.0):
    start_index = int(start * SAMPLE_RATE)
    count = int(duration * SAMPLE_RATE)
    if start_index >= len(buffer):
        return

    count = min(count, len(buffer) - start_index)
    for i in range(count):
        t = i / SAMPLE_RATE
        envelope = math.exp(-decay * t)
        sample = 0.0
        for ratio, weight in partials:
            sample += weight * math.sin(2.0 * math.pi * frequency * ratio * t + phase)
        buffer[start_index + i] += amplitude * envelope * sample


def add_thump(buffer, start, amplitude=0.35):
    start_index = int(start * SAMPLE_RATE)
    count = min(int(0.18 * SAMPLE_RATE), len(buffer) - start_index)
    for i in range(count):
        t = i / SAMPLE_RATE
        envelope = math.exp(-24.0 * t)
        sample = math.sin(2.0 * math.pi * 78.0 * t) * 0.7
        sample += math.sin(2.0 * math.pi * 120.0 * t) * 0.3
        buffer[start_index + i] += amplitude * envelope * sample


def add_click(buffer, start, amplitude=0.12):
    start_index = int(start * SAMPLE_RATE)
    count = min(int(0.05 * SAMPLE_RATE), len(buffer) - start_index)
    for i in range(count):
        t = i / SAMPLE_RATE
        buffer[start_index + i] += amplitude * math.exp(-90.0 * t) * math.sin(2.0 * math.pi * 1200.0 * t)


def build():
    random.seed(7)
    total = int(DURATION * SAMPLE_RATE)
    mix = [0.0] * total

    # Slow pad: root and fifth, with gentle movement.
    for start in (0.0, 16.0, 32.0, 48.0):
        add_tone(mix, start, 16.0, 110.0, 0.05, 0.12, [(1.0, 0.6), (2.0, 0.2), (3.0, 0.08)])
        add_tone(mix, start, 16.0, 165.0, 0.035, 0.14, [(1.0, 0.5), (2.01, 0.15)])

    # Gamelan-like interlocking melody, 16-step phrases.
    patterns = [
        [0, 2, 3, 2, 1, 2, 4, 3, 2, 1, 0, 1, 2, 3, 2, 1],
        [2, 3, 4, 3, 2, 4, 5, 4, 3, 2, 1, 2, 3, 4, 3, 2],
        [1, 2, 3, 4, 3, 2, 0, 1, 2, 3, 4, 5, 4, 3, 2, 1],
        [0, 1, 2, 3, 2, 1, 0, 2, 4, 3, 2, 1, 0, 2, 3, 4],
    ]
    step = 0.5
    for phrase in range(8):
        pattern = patterns[phrase % len(patterns)]
        base_time = phrase * 8.0
        for index, degree in enumerate(pattern):
            if index % 4 == 0 and phrase % 3 == 0:
                continue
            freq = SCALE[degree % len(SCALE)] * (1.0 if index < 8 else 0.5)
            add_tone(
                mix,
                base_time + index * step,
                1.4,
                freq,
                0.045 if index % 2 else 0.065,
                3.2,
                [(1.0, 0.65), (2.01, 0.25), (3.02, 0.12), (4.08, 0.06)],
            )

    # Low drum pulse and light off-beat clicks.
    for beat in range(0, int(DURATION / 2.0)):
        add_thump(mix, beat * 2.0, 0.22 if beat % 4 == 0 else 0.12)
        if beat % 2 == 1:
            add_click(mix, beat * 2.0 + 0.5, 0.07)

    # Soft room/rain bed with a simple low-pass.
    previous = 0.0
    for i in range(total):
        previous = previous * 0.985 + random.uniform(-1.0, 1.0) * 0.015
        mix[i] += previous * 0.02

    # Simple stereo-ish delay taps for depth.
    delay1 = int(0.17 * SAMPLE_RATE)
    delay2 = int(0.29 * SAMPLE_RATE)
    for i in range(total):
        value = mix[i]
        if i >= delay1:
            value += mix[i - delay1] * 0.18
        if i >= delay2:
            value += mix[i - delay2] * 0.10
        mix[i] = value

    # Fade edges and normalize to about -3 dBFS.
    fade = int(2.0 * SAMPLE_RATE)
    for i in range(fade):
        gain = i / fade
        mix[i] *= gain
        mix[-1 - i] *= gain

    peak = max(abs(sample) for sample in mix) or 1.0
    target = 0.70 / peak
    for i in range(total):
        mix[i] *= target

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with wave.open(OUTPUT, "w") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(SAMPLE_RATE)
        frames = bytearray()
        for sample in mix:
            value = max(-1.0, min(1.0, sample))
            frames += struct.pack("<h", int(value * 32767))
        handle.writeframes(frames)

    print(f"[Music] Wrote {OUTPUT} ({DURATION:.0f}s, {os.path.getsize(OUTPUT) / 1024 / 1024:.1f} MB)")


if __name__ == "__main__":
    build()
