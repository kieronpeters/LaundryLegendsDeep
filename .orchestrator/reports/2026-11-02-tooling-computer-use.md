# Computer-use tooling for OpenCode V2 — evaluation and recommendation

**Date:** 2026-11-02
**Author:** research subagent (not applied to the repo)
**Status:** recommendation only — nothing in the repository or OpenCode config was modified
**Scope:** give an OpenCode V2 agent eyes (screenshot) and hands (mouse, keyboard, window focus) on this Windows 11 machine, including OCR / set-of-marks text targeting.

---

## 1. Decision

| Role | Package | Why |
|---|---|---|
| **Primary** | `desktop-mcp` 0.1.1 (PyPI, Python) | Works today with zero broken dependencies, input hard-gated off by default, structured UIPI errors, smallest schema, verified live against the actual DirectX game window. |
| **Fallback** | `win32-mcp-server` 2.5.1 (PyPI, pinned `mcp<2`) | Same 4 core capabilities **plus** structured OCR with bounding boxes, `click_text`, `wait_for_text`, and Windows UI Automation. Use when text targeting is a hard requirement. |
| **Keep as-is** | `tools/game-vision/game-vision.ps1` | Still the only deterministic way to resolve the game window (by process, not by title). It is the *window resolver* for both MCP options. |
| **Rejected** | `@harusame64/desktop-touch-mcp`, `RandyNorthrup/win32-mcp-server` (same as fallback — see note), `tdav/Mcp.ComputerUse`, `sbroenne/mcp-windows` | Reasons in §7. |

**The single most important finding:** both Python candidates resolve windows by *title substring* and take the **first match**. The OpenCode window title is currently `Laundry Legends playtest fixes · OpenCode`, which contains the game window's title `Laundry Legends`. A live test of `focus_window("Laundry Legends")` focused **the OpenCode window**, not the game. Any agent that calls focus-then-type would type into the editor. This is a cross-cutting trap, not a package defect — see §6.1 for the fix.

---

## 2. What was actually verified on this machine

All installs went into throwaway virtual environments / npm cache under
`C:\Users\Asus\AppData\Local\Temp\opencode\ll-cu-eval\`. Nothing was installed system-wide, no PATH was changed, no service was registered.

Environment confirmed:

| Item | Value |
|---|---|
| OS | Windows 11 build 10.0.26300, x64 |
| Session | user, **not** elevated |
| Python | 3.15.0 (also 3.14, 3.13 available) |
| Node / npm | 24.21.0 / 11.19.0 |
| Display | 1 monitor, 2560×1440, **DPI 96 (100 % scaling)** |
| Game window | `Laundry Legends`, pid 12680, 1296×759 at (-7, 0), windowed/borderless, DirectX |

### 2.1 desktop-mcp (primary)

| Check | Command (abbreviated) | Result |
|---|---|---|
| Install on Python 3.15 | `py -3.15 -m venv …; pip install desktop-mcp` | **OK** — 78 packages, venv 116.7 MB, `desktop-mcp 0.1.1` |
| stdio handshake | `initialize` + `tools/list` | **OK** — 18 tools |
| Schema size | sum of `tools/list` JSON | **7,801 chars ≈ 2.2k tokens** |
| Screenshot, full desktop | `screenshot {monitor:0}` | **OK** — real PNG 2560×1440, 195 KB, written to `%TEMP%\desktop-mcp-scratch\` |
| Screenshot, **game window** | `screenshot {window_title:"Laundry Legends"}` | **OK** — 1296×759 PNG, mean luminance 61.1, 1 318 distinct colours in a 64×36 sample, `allBlack=false`. HUD text (`ORDER TICKET`, `Coins: 53`, controls list) is legible. See evidence note below. |
| Mouse input | `mouse_move {x:300,y:220}` then read `[System.Windows.Forms.Cursor]::Position` | **OK** — cursor went 1930,1155 → 300,220 |
| Input gate (default) | `mouse_click` with no env | **OK** — `{"ok":false,"error":{"type":"policy_refusal",…"required_env":"DESKTOP_MCP_ENABLE_INPUT"}}`, no click performed |
| Window focus | `focus_window {title_substr:"Laundry Legends"}` | **Call succeeded but matched the wrong window** (OpenCode) — see §6.1 |
| Window read | `window_info {title_substr:"Laundry Legends"}` | Returned the OpenCode window on a later call, the game on an earlier one → **match order is not stable** |

Evidence image (captured through the MCP server during this evaluation):
`C:\Users\Asus\AppData\Local\Temp\desktop-mcp-scratch\screenshot-f2e1c6ee19.png`

**This is the key result:** the game is a DirectX surface, `PrintWindow` returns black for it, and the repo's existing script already falls back to a desktop copy. `mss` (used by both Python candidates) *is* a desktop copy, and it captures the game correctly. **No special DirectX handling is required for a windowed game.**

### 2.2 win32-mcp-server (fallback)

| Check | Command | Result |
|---|---|---|
| Install on Python 3.15 | `pip install win32-mcp-server` | **OK** — 43 packages, venv 152.9 MB, `win32-mcp-server 2.5.1` |
| Default `pip` resolution | — | **BROKEN**: pulls `mcp 2.3.0`, server dies with `AttributeError: 'Server' object has no attribute 'list_tools'` |
| With `mcp<2` pinned | `pip install "mcp>=1,<2"` | **OK** — 53 tools register, `mcp 1.30.0` |
| `health_check` | tool call | **OK** — reports Python 3.15.0, DPI 96, 1 monitor, 2560×1440, **Tesseract not installed** |
| `capture_screen` | tool call | **OK** — returns a text block **and a real `image/png` content block** (244 KB PNG) |
| `capture_window` | `{window_title:"Laundry Legends"}` | Captured the **OpenCode** window (fuzzy match), not the game — see §6.1 |
| Schema size | sum of `tools/list` JSON | **22,288 chars ≈ 6.2k tokens** (53 tools, always advertised) |
| Tool hiding via `WIN32_MCP_BLOCKED_TOOLS` | env + `tools/list` | **No effect on the catalog** — still 53 tools listed; the env only blocks *execution* |

### 2.3 @harusame64/desktop-touch-mcp (rejected)

| Check | Result |
|---|---|
| `npx -y @harusame64/desktop-touch-mcp` | **Runs** — 32 tools, `desktop-touch` 2.4.0, native Rust engine messages (`UIA`, `vision-gpu`, `L1 capture`) |
| Schema size | **177,371 chars ≈ 49k tokens** — by far the most expensive |
| Footprint | 2.9 MB in npm cache **plus a 183.8 MB first-run payload in `C:\Users\Asus\.desktop-touch-mcp`** |
| Semantics | Genuinely good (`desktop_discover` / `desktop_act` / leases / perception guards), but built for business apps, not a rendered game |

### 2.4 Not run (documented only)

| Package | Why not run |
|---|---|
| `sbroenne/mcp-windows` v1.3.28 | Ships as a 54 MB self-contained .NET binary zip (with `SHA256SUMS.txt`), not pip/npx. Downloading and executing a prebuilt binary is outside the "pip/npx into its own environment" allowance. Assessment is from README + release metadata only. |
| `tdav/Mcp.ComputerUse` | No releases; must be built with .NET 10 SDK **and** MSVC Build Tools (`dotnet publish -r win-x64`). No license file. Assessment from README only. |

---

## 3. Comparison

| | **desktop-mcp 0.1.1** | **win32-mcp-server 2.5.1** | **desktop-touch-mcp 2.4.0** | **sbroenne/mcp-windows 1.3.28** | **tdav/Mcp.ComputerUse** |
|---|---|---|---|---|---|
| Install | `pip install desktop-mcp` (in own venv) | `pip install "win32-mcp-server==2.5.1" "mcp<2"` | `npx -y @harusame64/desktop-touch-mcp` | download 54 MB zip, run `.exe` | build from source (.NET 10 + MSVC) |
| Deps | fastmcp, mss, pyautogui, PyGetWindow, pywin32 | 43 pkgs incl. numpy, Pillow, uiautomation, rapidfuzz, psutil | node ≥ 20 + 184 MB native payload | none (.NET self-contained) | .NET 10 SDK + MSVC |
| **Verified working here** | **yes, fully** | yes, **with pin** | yes | no (not run) | no (not run) |
| Python 3.15 | **yes** | **yes** | n/a | n/a | n/a |
| Tools | 18 | 53 | 32 | ~40 | 16 |
| Schema cost | **≈2.2k tok** | ≈6.2k tok | **≈49k tok** | not measured | not measured |
| Screenshot | PNG **file path** (agent reads it) | PNG **image block** + path tag | image block (detail=full) | image block | image block (downscaled) |
| Window-relative capture | `window_title`, `region`, `monitor` | `window_title`, `monitor`, region via `capture_region` | `screenshot` + window meta | window/monitor | monitor only |
| Mouse | move/click/drag/scroll | move/click/double/triple/drag/scroll | click/drag/scroll | full | full |
| Keyboard | press / hotkey / type | press / hotkey / type (+ clipboard) | press/type + clipboard + terminal | press / type | press / hotkey / type |
| **Window focus** | `focus_window` (env-gated) | `focus_window` | `focus_window` | window mgmt | not implemented |
| **OCR / set-of-marks** | **none** | **yes** — `ocr_*_structured` (word + bbox + confidence), `click_text`, `wait_for_text`, `assert_text_visible` | OCR via UIA names, not pixels | OCR + UIA controls | none |
| Input safety | **strong**: input group OFF by default, rate limit 60/min, structured `policy_refusal` | moderate: `read_only` / `interactive` / `unrestricted` profiles, `dry_run`, allow/block lists, confirmation tokens | auto-guard + leases + perception guards | tool toggles, not a sandbox | **none** (explicitly by design) |
| Extra surface | screen recording (ffmpeg), no clipboard | process kill/start, clipboard, file dialogs, PowerShell | terminal, Excel, browser CDP, credential autofill | file dialogs, batch steps | shell, file read/write |
| Game suitability | **good** — mss captures the windowed DX surface; no activation glued to capture | **good** — same mss path; `capture_window` activates the window by default | neutral — heavy, coordinate-hostile | good for UI, unproven for DX | **poor** — BitBlt only, README documents black frames for HW-accelerated windows |
| License | code in repo, no PyPI license field | MIT | MIT | MIT | none |

---

## 4. Why desktop-mcp is the primary

1. **It is the only candidate that is fully working with no caveats on a clean install.** `pip install desktop-mcp` → 18 tools → screenshot of the live game → cursor actually moved. No pin, no build, no extra runtime.
2. **The safety model is the right shape for an agent that can move the user's mouse.** Input, window manipulation, and recording are each behind their own env var and are **off** unless explicitly enabled. A disabled group returns a structured error and does nothing — verified. Rate limiting (60 actions/min) is a real blast-radius limiter.
3. **Cheapest to keep loaded.** ~2.2k tokens of tool schema, versus 6.2k and 49k. At 18 tools it is also small enough for a model to choose correctly.
4. **The missing piece (OCR) is purchaseable later, not required today.** The game's clickable targets are 3D objects and a small fixed HUD; the existing script already does coordinate clicks well. If text targeting is later needed, upgrade to the fallback rather than starting there.
5. **It complements `game-vision.ps1` instead of fighting it.** `screenshot(region=…)` accepts the client rect that `game-vision.ps1 -Action capture` already computes, so the deterministic window resolution stays in the script and the MCP only does input + pixels.

**The one real cost:** screenshots come back as a **file path**, so the agent must `read` the PNG as a second call. That is deliberate (no base64 in the transcript) and works with OpenCode's image-capable `read` tool.

---

## 5. Config to apply (do not apply automatically)

Recommended durable install location, outside the repo, outside Temp:

```powershell
py -3.15 -m venv "$env:LOCALAPPDATA\ll-mcp\desktop-mcp"
& "$env:LOCALAPPDATA\ll-mcp\desktop-mcp\Scripts\python.exe" -m pip install --upgrade pip
& "$env:LOCALAPPDATA\ll-mcp\desktop-mcp\Scripts\python.exe" -m pip install desktop-mcp==0.1.1
```

`opencode.json` (or `opencode.jsonc`) — merge into the existing `mcp.servers` object:

```jsonc
{
  "mcp": {
    "servers": {
      "desktop": {
        "type": "local",
        "command": [
          "C:\\Users\\Asus\\AppData\\Local\\ll-mcp\\desktop-mcp\\Scripts\\desktop-mcp.exe"
        ],
        "cwd": "C:\\Users\\Asus\\Desktop\\AI Coding Test\\LaundryLegends",
        "codemode": false,
        "environment": {
          "DESKTOP_MCP_ENABLE_INPUT": "1",
          "DESKTOP_MCP_ENABLE_WINDOW": "1",
          "DESKTOP_MCP_RATE_LIMIT_PER_MIN": "120",
          "DESKTOP_MCP_SCRATCH_DIR": "C:\\Users\\Asus\\AppData\\Local\\Temp\\desktop-mcp-scratch"
        }
      }
    }
  }
}
```

Notes:

* **`cwd` matters little** for this server (it is not repo-aware), but it is set as requested so relative scratch paths and any future tooling resolve inside the repo-like context. The scratch dir is pinned to Temp so **screenshots never land in the repo** and can never be committed.
* **`codemode: false`** is deliberate. Code Mode is on by default; a computer-use server wants its screenshot results and tool list on the native path rather than funneled through the `execute` runtime. Verify in step 4 of §6 that a screenshot arrives as an image, not as base64 text.
* **Leave `DESKTOP_MCP_ENABLE_RECORD` unset.** Recording is not needed and is 30 s of ffmpeg process per call.
* The verified run used `…\Temp\opencode\ll-cu-eval\venv-desktop-mcp\Scripts\desktop-mcp.exe`; the only difference in the snippet above is the install path. If you prefer to keep the verified path, substitute it verbatim.
* **Do not** add `mcp` and `win32-mcp-server` at the same time — together they are ~8.4k tokens of schema and two competing input paths.

---

## 6. Verification procedure

### 6.1 Pre-flight — fix the window-title collision (do this first)

Observed live: `focus_window("Laundry Legends")` focused `Laundry Legends playtest fixes · OpenCode`.
Both Python servers do `pygetwindow.getWindowsWithTitle(substr)[0]`; `win32-mcp` additionally fuzzy-matches and picked the *same* wrong window. The game title is a strict prefix of the OpenCode window title, so **no substring can disambiguate them**.

Pick one, in order of preference:

1. **Rename the OpenCode session/window** so it no longer contains `Laundry Legends` (e.g. `Playtest fixes`). Cheapest, permanent fix; then title matching is safe for everything.
2. **Never focus or capture by title.** Resolve the window once with `game-vision.ps1 -Action capture` (process → `MainWindowHandle`), then use its `clientOrigin` / `clientSize` for MCP `screenshot(region=…)` and `mouse_click(x,y)`.
3. Verify every focus/capture by reading the returned `title` field before sending any keystrokes.

### 6.2 Confirm the server is connected

```powershell
opencode mcp list
```

Expect a `desktop` row in the connected state (`✓ desktop connected`). If it is not listed, the `opencode.json` merge is wrong; if it fails to start, run the executable directly — a `ModuleNotFoundError` means the venv was installed into a different interpreter.

### 6.3 Confirm the tools appear

```powershell
# from the repo root, using the same venv
& "$env:LOCALAPPDATA\ll-mcp\desktop-mcp\Scripts\python.exe" -c "import desktop_mcp, importlib.metadata as m; print(m.version('desktop-mcp'))"
```

Expect `0.1.1`. Then in a **new** OpenCode session, ask for the tool list, or run the raw stdio probe below. Expect exactly **18** tools:
`screenshot, list_windows, get_active_window, window_info, focus_window, move_resize_window, minimize_window, restore_window, mouse_move, mouse_click, mouse_drag, mouse_scroll, key_press, hotkey, type_text, record_start, record_status, record_stop`.

```powershell
# raw handshake without OpenCode (proves the process speaks MCP)
$exe = "$env:LOCALAPPDATA\ll-mcp\desktop-mcp\Scripts\desktop-mcp.exe"
'{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"probe","version":"0"}}}',
'{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}',
'{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' | & $exe 2>$null
```

### 6.4 Confirm a screenshot works (eyes)

In OpenCode, with the game window visible (not minimised, not fully covered):

```text
Use desktop_list_windows, find the entry whose title is exactly "Laundry Legends",
then call desktop_screenshot with {"window_title":"Laundry Legends"}.
Report the returned path, width and height, then read that PNG and describe the HUD.
```

Pass: path under `…\Temp\desktop-mcp-scratch\`, `w=1296 h=759`, and the image shows the order ticket / coins. A black or empty image means the window was occluded or minimised — desktop-copy capture only records what is actually on screen.

### 6.5 Confirm input works (hands) — non-destructive

```text
Call desktop_mouse_move with {"x":300,"y":220}. Then, in a shell, print the cursor position.
```

```powershell
Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.Cursor]::Position
```

Pass: `X / Y` equals `300 / 220`. This proves the whole pyautogui → Win32 input path without clicking anything.

Then a real click test on a harmless target:

```powershell
# 1. note the cursor
Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.Cursor]::Position
# 2. desktop_click at {x: 300, y: 220} via the agent
# 3. confirm the cursor is still at 300,220 and that nothing unexpected opened
```

### 6.6 Confirm the safety gate actually gates

```powershell
$env:DESKTOP_MCP_ENABLE_INPUT = $null   # in a fresh shell / a second config without the env block
```

Then `mouse_click` must return `policy_refusal`, **not** move the mouse. This was verified during the evaluation and is the test that tells you the gate is real rather than decorative.

### 6.7 Confirm a game key press

```text
Focus the game via game-vision.ps1 -Action focus -ProcessName LaundryLegends,
then call desktop_key_press {"key":"u"} and check that the HUD "Next goal" line changes.
```

Pass: the purchase line updates. Fail-and-stop if the keystroke lands in another window — that is the §6.1 collision.

---

## 7. Rejected / deferred, with reasons

| Candidate | Verdict |
|---|---|
| `@harusame64/desktop-touch-mcp` | Technically the most capable (semantic `desktop_discover`/`desktop_act`, perception guards, leases). **But:** ~49k tokens of tool schema (≈22× the primary) and 184 MB of native payload on first run. That budget buys correctness on business apps, not on a rendered 3D game where the targets are not in the UIA tree. Revisit only if the project later automates Edge/Chrome or Office. |
| `RandyNorthrup/win32-mcp-server` | Not rejected — it **is** the fallback. Note two sharp edges: PyPI's latest is 2.5.1 while the README advertises 2.6.1, and 2.5.1 is **broken** unless `mcp<2` is pinned. It also advertises all 53 tools even when `WIN32_MCP_BLOCKED_TOOLS` hides their execution, so the 6.2k-token cost is unavoidable. |
| `tdav/Mcp.ComputerUse` | Clean design (Native AOT, per-monitor-v2 DPI, `SendInput`, downscale-to-WXGA coordinate mapping — the model-friendly approach Anthropic recommends). **But** it needs a full .NET build toolchain including MSVC, ships **no release**, has **no license**, no window focus at all, and its own README says `BitBlt` returns black for HW-accelerated windows — precisely the game case. Only 12 tests. Not suitable. |
| `sbroenne/mcp-windows` | The strongest non-Python option: MIT, actively maintained (v1.3.28, Nov 2026), 127 stars, UIA-first with screenshot fallback, published token-savings benchmarks. **But** it is a 54 MB prebuilt binary rather than a pip/npx package, its centre of gravity is UI Automation (which a DirectX game does not expose), and it was not executed here. Worth re-evaluating if the project starts automating the Unity Editor or Office instead of the game. |

---

## 8. Risks and failure modes

### 8.1 Wrong-window input (highest severity)

`focus_window("Laundry Legends")` focused the OpenCode window during this evaluation. Consequence: keystrokes into an editor, or a click on a random UI element. **Mitigations:** §6.1 (rename the session / resolve by pid / read back the focused title before typing). Treat every `focus_window` result as a claim to verify, not a fact.

### 8.2 Occlusion and minimised windows

Both Python servers capture with `mss` (desktop copy). A window behind another window yields a screenshot of the *occluder*. A minimised window yields a black/stale image. `PrintWindow` — which `game-vision.ps1` tries first — returns black for this DX surface, so there is no non-visible capture path. **Rule:** raise the window, wait ~150–300 ms for composition, then capture. Never capture only because a previous call succeeded.

### 8.3 Fullscreen / exclusive fullscreen

The game is currently **windowed 1296×759**, and that is the supported configuration. True exclusive fullscreen can bypass DWM composition; desktop-copy may then capture a black or stretched frame, and synthetic clicks can land on the wrong surface. Keep the player build windowed/borderless-windowed for automation.

### 8.4 UIPI and the secure desktop

Not elevated. Input to a higher-integrity window is refused by Windows; `desktop-mcp` surfaces this as a structured `window_action_failed` naming UIPI (good). The UAC secure desktop, Ctrl+Alt+Del and the lock screen are invisible and untouchable to any process on the user desktop. If the screen locks, every automation run silently fails. **Do not** "fix" this by elevating the agent.

### 8.5 Focus stealing and racing the human

Synthetic input goes to whatever has focus *at that instant*. The owner using the keyboard mid-run, a notification toast, or an IME window can all redirect a click. **Rule:** one agent owns the mouse; announce it; keep a kill path (`pyautogui` FAILSAFE in win32-mcp is a corner-of-screen abort — acceptable, but it will interrupt a drag).

### 8.6 Token cost

Measured schema cost: **2.2k** (desktop-mcp), **6.2k** (win32-mcp, unavoidable), **49k** (desktop-touch). Screenshot cost scales with pixels: a 2560×1440 full-screen grab is ~4× the pixel budget of the 1296×759 game window (roughly 1.3k vs 5k tokens per frame). **Rule:** capture the window or an explicit region, never the full desktop, and never re-capture when the previous frame already answers the question. A 20-step playtest at 1 screenshot per step is 25–100k image tokens — budget for it.

### 8.7 DPI and multi-monitor

This machine is 100 % scaling, 1 monitor — the benign case. On a 150 % display, `mss` pixels and `pyautogui` points diverge unless per-monitor-v2 awareness is set. desktop-mcp sets it at startup but the README admits coordinate agreement is **unit-tested only, never validated on real multi-DPI hardware**. If the display setup changes, re-run §6.5 before trusting a click.

### 8.8 Data hygiene

Screenshots land in `%TEMP%\desktop-mcp-scratch\` and are never cleaned. They can contain anything on screen — including credentials, private messages, or the OpenCode transcript itself. The configured `DESKTOP_MCP_SCRATCH_DIR` keeps them out of the repo, but nothing deletes them. Add a periodic cleanup, and never point the scratch dir inside `LaundryLegends`.

### 8.9 Supply chain and drift

Both recommended packages are single-maintainer, ~4-month-old PyPI projects with no audited release process (`desktop-mcp` was uploaded via plain twine, not Trusted Publishing). Pin exact versions, install into a dedicated venv, and re-run §6 after any upgrade. `win32-mcp-server` in particular will break again the moment its `mcp` dependency resolves to 2.x.

---

## 9. Install ledger — exactly what was installed

Nothing system-wide, nothing on PATH, nothing in the repo. All under `C:\Users\Asus\AppData\Local\Temp\opencode\ll-cu-eval\` plus one npm cache entry.

| What | Where | Size |
|---|---|---|
| Python 3.15 venv + `desktop-mcp==0.1.1` (+77 transitive, incl. `fastmcp 3.4.2`, `mss 10.2.0`, `pyautogui 0.9.54`, `pywin32 312`) | `…\ll-cu-eval\venv-desktop-mcp` | 116.7 MB |
| Python 3.15 venv + `win32-mcp-server==2.5.1` (+42 transitive, incl. `numpy 2.5.4`, `pillow 13.0.0`, `uiautomation 2.0.29`), then downgraded `mcp` 2.3.0 → 1.30.0 | `…\ll-cu-eval\venv-win32` | 152.9 MB |
| `@harusame64/desktop-touch-mcp@2.4.0` via `npx` | npm cache `_npx\c67e74de0542c87c` | 2.9 MB |
| its first-run native payload | `C:\Users\Asus\.desktop-touch-mcp` | **183.8 MB** |
| screenshots produced by verification | `%TEMP%\desktop-mcp-scratch` | 2 files, 1.1 MB |
| probe scripts written for verification | `…\ll-cu-eval\probe_mcp.py`, `probe_schema.py`, `probe_capture.py`, `args*.json` | < 20 KB |

**Rollback:** delete `…\ll-cu-eval`, `%TEMP%\desktop-mcp-scratch`, `C:\Users\Asus\.desktop-touch-mcp`, and run `npm cache clean --force` if the npx entry matters. No registry keys, services, PATH edits, or global installs were made.

---

## 10. Recommended next step (not done here)

1. Ask the owner to approve the two config edits in §5 (`opencode.json` server entry) and the durable venv install.
2. Rename the OpenCode session to drop `Laundry Legends` from its title (§6.1) — this removes the wrong-window class of failure entirely for both servers.
3. Run §6.2 → §6.7 and record the evidence under `.orchestrator/reports/`.
4. Only if text-targeting is then still needed: install Tesseract (a **system-wide app install — approval gate**), install `win32-mcp-server` with the `mcp<2` pin, and switch the primary to it.

Do not add a computer-use server to a task that does not need the game window; the schema and screenshot tokens are charged to every session that has it connected.
