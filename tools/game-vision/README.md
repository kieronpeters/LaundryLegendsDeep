# Game vision harness

Agent-facing see-and-click control for the built player. One PowerShell script,
no packages: window capture, mouse clicks, key presses, relative mouse moves.

## Why

Deterministic Unity captures (`GameCaptureTool`) are the source of truth for
asset QA. This harness answers the other question: "what does the **shipped
build** look like right now, and can it be driven?" It is what lets an agent
open the menu, walk to the counter, press E, and screenshot each step.

## Usage

```powershell
# What is running?
powershell -NoProfile -ExecutionPolicy Bypass -File tools\game-vision\game-vision.ps1 -Action windows

# Capture the client area to a PNG (returns JSON with the client origin and size)
powershell -NoProfile -ExecutionPolicy Bypass -File tools\game-vision\game-vision.ps1 `
  -Action capture -ProcessName LaundryLegends -Out TestResults\game-vision\shot.png

# Click at client-relative coordinates
powershell -NoProfile -ExecutionPolicy Bypass -File tools\game-vision\game-vision.ps1 `
  -Action click -ProcessName LaundryLegends -X 640 -Y 420

# Hold W for 700 ms, then look right with a relative mouse delta
powershell -NoProfile -ExecutionPolicy Bypass -File tools\game-vision\game-vision.ps1 `
  -Action keys -ProcessName LaundryLegends -Keys "w:700"
powershell -NoProfile -ExecutionPolicy Bypass -File tools\game-vision\game-vision.ps1 `
  -Action move -ProcessName LaundryLegends -DX 260 -DY 0
```

## Coordinate model

- `capture` returns `clientOrigin` (screen pixels) and `clientSize`.
- `click` takes **client-relative** pixels, i.e. the same pixels as the captured
  image when `cropX`/`cropY` are zero.
- The script sets per-monitor DPI awareness, so capture pixels and click pixels
  match on scaled displays.

## Capture modes

1. `printwindow` — `PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT)`. Works on the
   URP player window; keeps the game clickable in the background.
2. `desktop-copy` — fallback when PrintWindow returns black (some DirectX
   surfaces). It copies the window rectangle from the desktop, so the window
   must be visible and not minimised.

## Launch the player windowed

Fullscreen-exclusive games often return a black `PrintWindow` frame and can drop
injected input. Launch with:

```powershell
Start-Process Build\LaundryLegends.exe -ArgumentList "-screen-fullscreen 0 -screen-width 1280 -screen-height 720"
```

## Limits

- Input goes to the focused window; the script focuses it first. Do not type or
  click while a scripted sequence runs.
- This is turn-based control (capture -> decide -> act). It is not a 60 fps loop.
- Every capture is a PNG read for the model; sample checkpoints, not video.

## Verified

- Menu capture at 1280x720 (PrintWindow, 96 DPI).
- `START GAME` click at client `(640, 420)` loaded the Laundromat.
- `w:700` moved the player; `-DX 260` turned the camera.
- Screenshots: `TestResults/game-vision/01-menu.png` ... `04-looked.png`.
