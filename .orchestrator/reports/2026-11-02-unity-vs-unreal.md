# Decision memo: stay on Unity 6 or move to Unreal Engine 5?

- **Date:** 2026-11-02
- **Type:** Research and decision memo. No Unity run, no software installed, no project change. One file written (this report).
- **Question:** for Laundry Legends -- a small stylized 3D business sim (greybox laundromat, 8 machines, 3-4 NPCs, orders/economy, secret back-room arcade, Windows PC, one developer plus an AI coding agent) -- should the project stay on Unity 6 or port to Unreal Engine 5?
- **Author:** research subagent, read-only on the repository.
- **Evidence standard:** project facts are measured from this repository and its logs. Engine facts are cited to vendor documentation with URLs in section 10. Anything estimated is labelled **estimate** and the method is given. I did not install or run Unreal Engine, so all Unreal timings on this hardware are unmeasured.

---

## 0. Recommendation in one page

**Stay on Unity 6.0 and do not port.** Then move the editor pin to Unity 6.3 LTS, and spend the switching budget on characters, materials and lighting -- the three things that actually look bad today.

The decision is not close, and it is not mainly about engine capability. It is about three measured facts:

1. **The automation loop is the project's main asset, and it is fast.** In this repository, a full EditMode run of 147 tests takes 17.6 s wall clock, the 59-test PlayMode run takes 111 s, a Windows build takes 33 s, and a scene-capture pass takes 24 s. That is a roughly three-minute full verification cycle that the agent can drive unattended. Nothing in Unreal gives an equivalent cycle for free, and a port deletes the loop before it can be rebuilt.
2. **The pain is art, not engine.** The project's own realism audit lists the open defects: faceless tinted mannequin NPCs, an opaque "transparent" bundle, a carried item that floats, flat materials, dark ceiling, no reflection probes. Lumen does not fix a character rig; Nanite does not fix a material. The art direction is explicitly "stylized low-poly", "avoid photoreal skin, complex cloth simulation, and expensive volumetric effects" (`GAME_DESIGN.md`, section 12).
3. **A port is a rebuild, not a migration.** The repository holds 27,752 lines of C# (9,110 runtime, 13,383 editor tooling, 5,259 tests), 205 tests, 3 scenes, 61 editor scripts, and a working build. Almost none of that is portable: C# is not an Unreal language, editor tooling targets a different API, the 205 tests must be rewritten against a different framework, and scenes/prefabs/materials are Unity assets. Estimate: **20-32 person-weeks** to reach parity (section 4).

The counter-argument -- "Unreal looks better out of the box" -- is true for photoreal interiors with scanned assets on 8 GB+ GPUs. It is not true for a 756-triangle basket in a 16x12 m room on a 4 GB laptop GPU, which is the actual project.

**One thing does need to change:** the pinned editor, `6000.0.64f1`, is on Unity 6.0 LTS, whose support window closed in October 2026 (Unity's own support page: "Unity 6.0 LTS is supported through October 2026", while 6.3 LTS runs to December 2027). That is a cheap, separately-approvable maintenance move, not an engine question.

---

## 1. Project baseline (measured)

| Fact | Value | Source |
|---|---|---|
| Engine / editor | Unity `6000.0.64f1`, URP 17.0.4, C# | `PROJECT_STATE.md` |
| Phase | 8 of ~16 complete pending owner playtest | `PROJECT_STATE.md` |
| Runtime C# | 64 files, 9,110 lines | measured this session (`Assets/LaundryLegends/Scripts`) |
| Editor C# | 61 files, 13,383 lines | measured (`Assets/LaundryLegends/Editor`) |
| Test C# | 42 files, 5,259 lines | measured (`Assets/LaundryLegends/Tests`) |
| Automated tests | 147 EditMode, 58 PlayMode (57 pass, 1 intentionally skipped) | `editmode-final.xml`, `playmode-final2.xml` |
| EditMode run | 0.51 s test time, **17.6 s** wall clock | `TestResults/editmode-final.log` |
| PlayMode run | 90.8 s test time, **111.2 s** wall clock | `TestResults/playmode-final2.log` |
| Windows build | **33.2 s**, 196 MB output | `TestResults/build-final.log`, `Build/` |
| Deterministic capture pass | **23.8 s** | `TestResults/captures-final.log` |
| Machine | Ryzen 7 7445HS (6C/12T), **15.3 GB RAM**, RTX 3050 Laptop **4 GB VRAM**, 131 GB free | measured this session |
| Toolchain present | .NET 10, VS Build Tools 2026, Blender 4.5.15 | measured; no Epic Launcher, no UE install |

Mesh scale in use: basket 756 tris, loose laundry 324, bag 432, washer/dryer 808, wrapped bundle 1,140, NPC mannequin ~1.8k. Materials are flat URP Standard/unlit with a few CC0 PBR maps.

---

## 2. Comparison table

Weights reflect what decides this project: agent iteration speed first, art outcome second, cost/risk third.

| Criterion | Weight | Unity 6.0 (today) | Unreal Engine 5.8 | Edge |
|---|---|---|---|---|
| **Headless automation** | 30% | `-batchmode -executeMethod`, exit codes, `-runTests`, XML reports, scene captures. Proven in-repo: 3 min full loop | `UnrealEditor-Cmd.exe -run=pythonscript`, `-ExecCmds="Automation RunTest ...;Quit"`, `-ReportExportPath`, UAT/Gauntlet. Real but heavier; editor cold start and DDC dominate | **Unity**, strongly for this workflow |
| **Code the agent can write** | (in above) | C# text files, no build step to reason about, YAML scenes/prefabs the agent can read, diff, and edit | C++ with UHT macros/GC/reflection, or Blueprint, which is a **binary asset** the agent cannot author as text. C# via UnrealSharp is unofficial and its own FAQ says not production-ready | **Unity**, strongly |
| **Test authoring** | (in above) | NUnit + UnityTest coroutines in C#; 205 tests already exist | Automation Spec/Simple tests in C++, latent commands for multi-frame tests; Blueprint functional tests. Workable, more verbose | **Unity** (incumbency) |
| **Parallel agents / mergeability** | (in above) | Text scenes/prefabs; UnityYAMLMerge does semantic merges | Binary `.uasset`/`.umap`; Epic documents check-out as "lock it for editing". Merge is not available; conflicts are resolved by picking a file | **Unity** |
| **Visual ceiling for this art style** | 25% | URP handles stylized low-poly, neon emissive, baked/real-time mix well; ceiling is artist time | Lumen/Nanite are best-in-class for photoreal, dense, dynamic-lit scenes; irrelevant-to-harmful at 300-1500 tri props on 4 GB VRAM | **Tie** (UE only for a different game) |
| **Character/animation pipeline** | (in above) | Humanoid avatar abstraction previously failed on third-party rigs (documented); works with a matching rig | Skeleton asset + IK Retargeter is more forgiving of mixed rigs | **UE**, narrowly -- the one real technical gap |
| **Porting cost** | 20% | 0 | 20-32 person-weeks to parity; 205 tests rebuilt; all editor tools rewritten | **Unity**, decisively |
| **Learning / agent ramp** | (in above) | Zero; 27.7k lines of context and a tuned loop | New language, build system, asset model, test framework; no reusable code | **Unity** |
| **Licensing** | 8% | Personal free under $200k revenue/funding; Pro $2,310/seat/yr above, or for consoles; no runtime fee | Free under $1M lifetime gross per product; 5% royalty above (3.5% with EGS parity) | **Tie** at this scale (both effectively $0) |
| **Build size / distribution** | 5% | 196 MB shipped today | ~321 MB blank shipping build, ~692 MB blank development (third-party measurements); typical small project 1-3 GB | **Unity** |
| **Hardware fit** | 7% | Runs on 4 GB VRAM / 16 GB RAM today | Epic recommends 32 GB RAM, 8 GB+ VRAM, 12-16 cores for local C++ compile. Current machine is below all three | **Unity** |
| **Asset availability** | 3% | Asset Store 13,000+ top-rated; strong stylized low-poly; Kenney/Quaternius/ambientCG are engine-agnostic | Fab: excellent photoreal scans, but Megascans stopped being broadly free after 2024; UE-native content skews realistic | **Unity**, narrowly for stylized |
| **Strategic ceiling (console, publisher, big worlds)** | 2% | Consoles need Pro; smaller teams; less publisher prestige | Native console pipelines, World Partition, cinematics, Sequencer, large-team workflows | **UE** |
| **Weighted outcome** | 100% | | | **Stay on Unity** |

---

## 3. Iteration speed and automation (the decisive section)

### 3.1 What exists here, measured

The loop the agent actually uses, from this repository's logs:

| Step | Command shape | Measured wall clock |
|---|---|---|
| Compile + EditMode | `Unity.exe -batchmode -quit -projectPath ... -runTests -testPlatform EditMode -testResults ...` | 17.6 s (147 tests) |
| PlayMode | same with `-testPlatform PlayMode` | 111.2 s (59 tests) |
| Windows build | `-executeMethod LaundryLegends.Editor.BuildGameTool.BuildDirect` | 33.2 s |
| Deterministic captures | editor tool + `-executeMethod` | 23.8 s |

That is the whole quality gate in under three minutes, unattended, with exit codes and NUnit XML the agent can parse. It is the reason eight phases landed between 2026-10-19 and 2026-11-01. The loop is not a convenience; it is the production method.

Unity documents the primitives directly: `-batchmode`, `-executeMethod`, `-runTests`, `-logFile`, and the caveat that only one Editor instance can hold a project at a time (`EditorCommandLineArguments`). The single-instance limit is real but the project already works around it with worktrees and sequential scene ownership.

### 3.2 What Unreal actually offers

Unreal's automation is genuine and improving; it is not the weak point people claim:

- **Python editor scripting** with two paths: `UnrealEditor-Cmd.exe "...uproject" -ExecutePythonScript="..."` (full editor) and `-run=pythonscript -script="..."` (headless commandlet). The commandlet does not auto-load a level, so scripts must load one explicitly.
- **Automation tests** runnable from the command line: `-ExecCmds="Automation RunTest Group:MyGroup;Quit"` with `-ReportExportPath` writing JSON + HTML.
- **UAT / Gauntlet** for orchestrated runs, and **Unreal MCP** (new in 5.8, experimental) exposing an in-editor tool server to external agents.

So "Unreal can't be automated" is false. The real differences are cost and shape:

1. **Cold-start and shader cost.** Unreal's Derived Data Cache must be built and rebuilt; shader compilation is CPU-bound and parallel, and community reports of 20-30 minute first-run compiles on modest machines are common (Epic's own DDC docs describe the cache lifecycle). A 6-core/16 GB laptop will sit at the slow end. Unity's equivalent cost here is 17 seconds, because the project is small and already imported.
2. **Batch mode is headless; MCP is not.** Unreal MCP runs inside a full editor process, after map load and shader work, and community reports describe experimental connection instability. An autonomous loop wants `exit 0` + XML, not a socket to a GUI. Unity's batch mode is exactly that, today, verified in this repo.
3. **The asset model fights text-based agents.** Unreal's `.uasset`/`.umap` are binary. Epic's own source-control guide describes check-out as "lock it for editing" and warns that syncing with the editor open can conflict. A binary asset cannot be read, diffed, reviewed or patched by an agent; it can only be produced by driving the editor. Unity's scenes and prefabs are YAML, and `UnityYAMLMerge` performs semantic merges. For this project's review discipline ("inspect the staged diff before every commit"), that difference is structural: today the lead agent can read a scene diff; in Unreal it would be a binary blob.
4. **The language tax.** C++ means header/implementation splits, UHT reflection macros, manual GC semantics, and slow incremental builds; Live Coding helps only for `.cpp` body edits and explicitly struggles with header/constructor changes. Blueprints are fast for a human but are binary graphs. UnrealSharp exists but its FAQ states it is not production ready yet. An LLM writing 27k lines of Unreal C++ will make more compile-error round-trips per feature than the same model writing C#, and C# is far better represented in its training distribution.

**Net:** Unity's loop is roughly 3 minutes end-to-end and already tuned. A credible Unreal equivalent would need a Python tooling layer, a test harness, cooked-build captures, and a warm DDC, and would still be minutes-to-tens-of-minutes per cycle on this hardware. That is the single largest cost of switching, and it recurs on every task, not once.

### 3.3 Test infrastructure comparison

| | Unity | Unreal |
|---|---|---|
| Framework | Unity Test Framework (NUnit + `UnityTest` coroutines) | Automation Framework (Simple/Complex), Automation Spec, Functional Tests, Low-Level Tests (Catch2), Gauntlet |
| Where tests live | C# assemblies, isolated EditMode/PlayMode asmdefs | C++ modules, plus Blueprint functional test maps |
| Command line | `-runTests -testResults x.xml` | `-ExecCmds="Automation RunTest ...;Quit" -ReportExportPath=...` |
| Multi-frame gameplay tests | `yield return null` in coroutines | latent command chains (`ADD_LATENT_AUTOMATION_COMMAND`), verbose |
| Existing assets | 205 tests covering orders, economy, machines, arcade, HUD | 0 |

A 2025 survey of Unreal test tooling concludes the same thing practitioners report: the Automation Framework exists, but multi-frame tests are verbose and teams end up spreading across Spec, Functional and Gauntlet depending on the test. Nothing there is disqualifying; it is simply a rebuild with a worse ergonomic profile for this specific codebase.

---

## 4. Porting cost

### 4.1 What survives, what dies

| Asset | Count / size | Survives a port? | Why |
|---|---|---|---|
| Design docs, roadmap, economy numbers, quest text | ~14 markdown docs | **Yes, 100%** | Engine-neutral |
| `ArtSource/` (.blend, FBX, PNG, WAV, MP3) | Blender sources + exports | **Yes, 100%** | FBX/textures/audio import into UE |
| CC0 PBR materials (ambientCG) | 340 MB maps | **Yes** | Standard PBR |
| Custom meshes (basket, bag, laundry, bundle, washers) | 300-1,140 tris each | **Yes**, re-import + re-material | Materials/mesh setup must be redone |
| Runtime C# gameplay | 9,110 lines | **No** | C# is not an Unreal language; rewrite in C++/Blueprint |
| Editor tooling / validators | 13,383 lines, 61 scripts | **No** | Unity Editor API only |
| Tests | 5,259 lines, 205 tests | **No** | Different framework and APIs |
| Scenes, prefabs, ScriptableObjects, materials, animator controllers | 3 scenes + prefabs | **No** | Unity serialization |
| URP shaders / volume profile | small | **No** | Rebuild in UE material system |
| Voice WAVs, music, dialogue tables | 54 clips + 3 tracks | **Yes** | Audio is portable |
| Automation loop, capture harness, build tool | proven | **No** | Must be rebuilt |
| Learned context (agent prompts, guardrails, test recipes) | significant | **Partially** | Process survives; commands do not |

### 4.2 Effort estimate

Method: bottom-up workstream sizing against the measured code and content inventory, assuming one developer plus the same AI agent, a competent-but-new Unreal user, and the existing review/verification discipline (which must be re-earned, not assumed). **These are estimates, not measurements.**

| # | Workstream | Basis | Person-weeks |
|---|---|---|---|
| 1 | Toolchain, project scaffold, settings, packaging | Epic launcher + VS + 5.8, Win64 shipping, .gitignore/LFS | 1.5-2.5 |
| 2 | Player controller, input, interaction raycast, carry/attach | 1st-person + physics attach semantics differ | 1.5-2.5 |
| 3 | Domain logic: GameState, Economy, OrderLedger/state machine, machines, arcade, upgrades, save | ~4k lines of the 9.1k are near-pure logic | 3-4.5 |
| 4 | Environment rebuild: room, storefront, 8 machines, secret door, props, signage, trim | Currently generated by C# tools (Greybox/Phase2/Phase3 setup) | 3-5 |
| 5 | Customers, NPC AI, animation, dialogue, voice | Retargeting is easier; behavior code is not | 2-3.5 |
| 6 | UI: HUD, tickets, prompts, pause, menu, shop | UMG vs IMGUI/UGUI | 2-3 |
| 7 | Audio, music, subtitle wiring | Port existing clips | 0.5-1 |
| 8 | Test + validator rebuild (205 tests, 8 validators) | Rebuild from intent, not code | 3.5-5 |
| 9 | Deterministic captures + CI harness | UE screenshot/MRQ + report parsing | 1-2 |
| 10 | Stabilization to parity + regression budget (~30%) | Unknown-unknowns | 3-5 |
| | **Total** | | **21.5-33.5** |

**Reading:** 21-33 focused person-weeks, central estimate ~26 (about 6 months at 20-30 h/week, or ~5 months full-time). Deduct 25% if the agent's throughput transfers cleanly; add 40% if it does not. **Optimistic floor 14 weeks; pessimistic 12 months; probability of hitting parity without shipping slip: low.**

For comparison: the entire project from bootstrap to Phase 8 took about two weeks of calendar time *because* every phase reused an existing loop. A port rebuilds the loop first and produces zero shippable progress until it is rebuilt.

### 4.3 Hidden porting costs

- **Hardware.** Current machine is 15.3 GB RAM, 4 GB VRAM, 6 cores. Epic recommends 32 GB, 8 GB+ VRAM, and 12-16 cores without distributed builds. Realistically a new workstation (**estimate** $1,200-2,000) or an unacceptable daily experience.
- **Disk.** 131 GB free today. UE 5.8 install plus project plus DDC plus packaged builds and shader caches plausibly eats 100-150 GB, leaving no headroom.
- **Re-approval.** New engine, new package ecosystem, new licensing terms, new asset licences: all of it re-crosses the project's approval gates.
- **Regression risk.** The 205 existing tests are the safety net for every current change. They do not port. During the port, there is no net.
- **Opportunity cost.** Every week of the port is a week not spent on characters, clothing, laundry props and lighting -- the actual blockers.

---

## 5. Visual ceiling: what Lumen and Nanite would and would not buy

**What the game needs** (`GAME_DESIGN.md` section 12): clean stylized silhouettes, flat-ish materials with warm contrast, practical fluorescents, neon bounce in the arcade, readable signage. Explicitly *avoid* photoreal skin, complex cloth, expensive volumetrics.

| Feature | What it does | Does it help Laundry Legends? |
|---|---|---|
| **Lumen GI/reflections** | Dynamic indirect light and reflections without baking | **Some.** Neon bounce in the arcade and the secret-room reveal would look good. But URP already does emissive + baked/point-light bounce, and the current defect is *dark ceiling and flat materials*, which is a lighting-composition and material problem, not a GI-algorithm problem. Cost: SM6 + RTX-2000-class for hardware RT, 8 GB VRAM recommended; on 4 GB the editor and the game both suffer |
| **Nanite** | Virtualized geometry for film-density meshes; removes LOD work | **Effectively nothing.** Epic's own doc says Nanite is for meshes with "many triangles" or "triangles that will be very small on screen". Here the largest mesh is ~1.8k triangles. Nanite also drops MSAA, forward rendering and VR, restricts materials to Opaque/Masked, and does not support morph targets -- which matters if faces get blendshapes later |
| **Virtual Shadow Maps** | High-quality dynamic shadows at scale | Marginal at one room, 4 lights |
| **TSR / temporal upscaling** | Needed to afford Lumen | Mildly **negative** for crisp low-poly edges and stylized flat shading |
| **Megascans / photoreal assets** | Scanned PBR | Wrong art direction, and no longer broadly free |
| **UE post stack (bloom, DoF, color)** | Strong defaults | Genuinely better out-of-box look, and reproducible in URP with a volume profile and grading in a day |

**Where Unreal is genuinely better for this game: characters.** The project's most expensive art failure was rigs: 100 Avatars characters whose hips sat at y=4629, Unity's humanoid avatar builder rejecting a re-export, and Quaternius models importing as non-humanoid -- ending with a faceless mannequin. UE's Skeleton + IK Retargeter model is more tolerant of mixed rigs. That is worth taking seriously -- but it is fixable in Unity for far less than a port (source a rig that maps, or author the animations for the actual rig, as the project already did once with the mannequin).

**Verdict:** switching engines to fix "props, clothing, laundry" is treating a content-production problem as a rendering problem. The visual ceiling here is set by character rigs, material craft, lighting composition and animation, all of which are engine-neutral and all of which are cheaper to buy in Unity.

---

## 6. Total cost of ownership

| Item | Unity 6 | Unreal 5.8 | Notes |
|---|---|---|---|
| Licence at this scale | **$0** (Personal, under $200k revenue/funding) | **$0** (under $1M lifetime gross per product) | Both free today. Licensing is a non-issue and should not drive the decision |
| Licence if it succeeds | $2,310/seat/yr above $200k; required for consoles | 5% royalty above $1M per product (3.5% with EGS parity) | UE is cheaper at $500k revenue, more expensive at $5M. Irrelevant pre-launch |
| Runtime fee | None (cancelled 2024) | None besides royalty | |
| Engine/source access | Source only on Enterprise | Full source from day one | UE advantage for deep customisation; not needed here |
| Install footprint | Editor ~10 GB + project | Engine ~60 GB + project + DDC + caches | This machine: 131 GB free |
| Shipped build | **196 MB measured** | ~321 MB blank shipping, 692 MB blank dev; 1-3 GB typical | Matters for demo downloads, not gameplay |
| Dev hardware | Runs on 4 GB VRAM / 16 GB RAM | 32 GB RAM, 8 GB+ VRAM, 12-16 cores recommended | Real capex if switching |
| Marketplace | Asset Store 13,000+ top-rated; best stylized low-poly coverage | Fab: best photoreal/scan coverage; Megascans largely paid since 2025 | The project's actual sources (Kenney, Quaternius, ambientCG) are engine-agnostic |
| Hiring / contractors | Large pool, cheaper generalists; C# | Smaller, pricier; C++/Blueprint | Solo project now; relevant only if it grows |
| Learning | Already fluent; agent fluent | New language, build system, asset model, test framework | The dominant hidden cost |
| Bus factor | One dev + one agent, both productive | One dev + one agent, both novices again | |

Licensing is a wash, build size is a minor Unity win, hardware is a real Unreal cost, and learning is the largest Unreal cost. Nothing in TCO favours Unreal at this scale.

---

## 7. Recommendation

**Do not port. Stay on Unity 6, complete Phase 9+, and ship.**

Rationale in priority order:

1. **The engine is not the constraint.** The measured blockers are character rigs, material craft, lighting, clothing and prop detail. All are engine-neutral and all are cheaper in the engine where the team is already fluent.
2. **The port destroys the only durable competitive advantage this project has:** a three-minute, unattended, deterministic verification loop with 205 tests behind it. Rebuilding that is the largest single line in the port estimate and it produces no player-visible value.
3. **The port's cost exceeds the remaining project.** 21-33 person-weeks against a project that has reached Phase 8 in two weeks of agent-assisted work is a multi-month detour with a real chance of never reaching parity.
4. **Unreal's advantages do not apply to this game.** Lumen and Nanite target photoreal, dense, large, dynamically-lit scenes on 8 GB+ GPUs. This is 300-1,500 triangle props, one room, four lights, 4 GB VRAM, and a design document that says "avoid photoreal".
5. **The one real Unreal advantage -- skeletal retargeting -- is a two-week Unity fix, not a six-month rebuild.**

### 7.1 Actions instead of a port (in order, each independently approvable)

1. **Move the editor pin to Unity 6.3 LTS.** 6.0 LTS support ended October 2026; 6.3 LTS is supported to December 2027. This is a `ProjectSettings/**` + `Packages/**` change, so it needs approval, a Git checkpoint, and a full 147/58 test run plus a rebuild. Expected cost: under a day. The existing suite is precisely what makes this cheap.
2. **Fix the character pipeline.** Highest visual return per hour. Either source a rigged character whose skeleton matches the animation clips, or author clips for the rig in use. Retire the tinted mannequin before Phase 10.
3. **Do a materials-and-lighting pass.** Reflection probes, a proper volume profile, three-point practical lighting, and 3-5 hero materials with real grime/wear. This closes most of the "flat materials, dark ceiling" gap without touching Lumen.
4. **Fix the two known render defects** (opaque bundle, floating carried item) before adding new content.
5. **Keep the repository engine-agnostic where it is free:** continue storing editable sources in `ArtSource/` and design intent in markdown. This is already the practice; it preserves option value at zero cost.
6. **Do not test-drive Unreal "just to see".** An exploratory spike costs a week, produces nothing reusable, and cannot answer the only question that matters (would the loop be faster?) without first rebuilding the loop.

### 7.2 Conditions that would change this recommendation

Revisit the decision if **any** of these becomes true. Each is a trigger, not a preference.

| Trigger | Threshold | Why it changes the answer |
|---|---|---|
| **Art target goes photoreal** | Brief requires scanned/PBR interiors, realistic skin, cloth sim, or cinematic lighting as a core selling point | Lumen + Megascans + UE's post stack become the cheap path; URP/HDRP becomes the awkward one |
| **Minimum spec rises to 8 GB+ VRAM** | Target audience hardware shifts (e.g. current-gen console parity) | Removes the hardware objection that dominates today |
| **Console certification becomes required** | Signed PS5/Xbox/Switch 2 deal | Unity needs Pro ($2,310/seat) plus platform work; UE's console pipeline is stronger |
| **Publisher or funding mandates Unreal** | Contractual | Decision is made for you; budget 21-33 person-weeks and a hardware upgrade |
| **Environments become large/open** | Multi-street neighbourhood, 10+ buildings, crowds, streaming | World Partition/Nanite start paying for themselves |
| **Team grows past ~3 with artist-heavy workflow** | Sequencer, cinematics, live-link, virtual production needs | UE's content pipeline and artist tooling win at team scale |
| **Unity becomes a hard blocker** | An unresolved editor/engine defect blocks a phase for >2 weeks, or licensing terms become unacceptable | Switching becomes cheaper than staying; still document the specific blocker first |
| **The project's own tests stop paying off** | Suite becomes slow/flaky enough that the loop exceeds ~30 min | The incumbency argument weakens; investigate before switching |

**Explicit non-triggers:** "Unreal looks better in demos", "props look bad", "we could use Nanite", "UE is what real studios use", "Lumen would be nice for the arcade". None of these survives contact with the measured bottleneck.

### 7.3 If a trigger does fire

Do not port incrementally -- dual-engine maintenance is the worst outcome. Instead: freeze features, write a one-page parity checklist from the existing 205 tests, port in that order, keep the Unity build shippable until Unreal reaches parity, and set an explicit abandon criterion (e.g. "if parity is not reached in 14 weeks, ship the Unity build"). The existing tests are the port's specification, which is the best thing about this codebase's discipline.

---

## 8. Confidence and unknowns

- **High confidence:** project inventory and timings (measured); Unity support windows and pricing (vendor pages); UE hardware recommendations, Nanite scope, binary-asset workflow, automation entry points (Epic docs); licensing shape for both.
- **Medium confidence:** the 20-32 person-week estimate. It is bottom-up and anchored to measured line counts, but no comparable port exists in this repository to calibrate against. Treat the range as +-40%.
- **Unmeasured:** Unreal editor startup, shader compile, C++ incremental build and packaged-build times on *this* machine. I did not install Unreal (out of scope, and installation is an approval gate). Community-reported figures (20-30 min first-run shader compiles) are consistent with Epic's DDC documentation but are not a controlled measurement.
- **Not evaluated:** Unreal 6 / Verse roadmap (no public build); console devkit availability; publisher preferences (none exists yet).
- **Known gap in this memo:** I did not benchmark a Unity 6.3 LTS migration, so its cost is an inference from the existing suite, not a measurement.

---

## 9. Summary table of the decision

| Question | Answer | Basis |
|---|---|---|
| Does Unreal render this game better? | Somewhat, for lighting; not for the current defects | Nanite scope, Lumen requirements, art direction |
| Is Unreal faster for this agent to build in? | No, materially slower on this hardware | Binary assets, C++/UHT, editor-bound MCP vs headless batch |
| Would 205 tests survive? | No -- rebuilt from intent | Framework and API mismatch |
| Would the code survive? | No -- 27.7k lines of C# is not portable | Language and API |
| Would the art survive? | Yes -- FBX, textures, audio, .blend | Format portability |
| Cost to switch | 21-33 person-weeks + hardware | Bottom-up estimate |
| Cost to stay | <1 week (LTS move) + art work | Existing loop and tests |
| Decision | **Stay on Unity 6** | |

---

## 10. Sources

Vendor and primary documentation (fetched or verified this session unless marked):

1. Unity 6 release support -- "Unity 6.0 LTS is supported through October 2026", "Unity 6.3 LTS is supported until December 2027". https://unity.com/releases/unity-6/support
2. Unity subscription changes -- Personal free to $200k revenue/funding; Pro $2,310/yr per seat; Pro required for console publishing; Pro/Enterprise +5% from 2026-01-12; Runtime Fee cancelled. https://unity.com/products/pricing-updates
3. Unity 6.0 Editor command line reference -- `-batchmode`, `-executeMethod`, `-runTests`, `-logFile`, `-nographics`; single-instance restriction. https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html
4. Unity Smart Merge -- `UnityYAMLMerge` performs semantic merges of `.unity`, `.prefab`, `.meta`. https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html
5. Unity Test Framework -- NUnit-based EditMode/PlayMode tests, command line and Test Runner. https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction
6. Unity Asset Store -- "Over 13,000 top-rated assets". https://assetstore.unity.com
7. Epic hardware and software specifications -- recommended 32 GB RAM, 8 GB+ graphics RAM, Windows 11, DX12; 12-16 cores as practical local-compile baseline; Lumen requires SM6 and RTX-2000/RX-6000/Arc or newer; Nanite requires SM6.6 atomics. https://dev.epicgames.com/documentation/en-us/unreal-engine/hardware-and-software-specifications-for-unreal-engine
8. Nanite overview -- purpose is high triangle density/pixel-scale detail; material restrictions (Opaque/Masked); no MSAA, forward rendering or VR; morph targets unsupported. https://dev.epicgames.com/documentation/en-us/unreal-engine/nanite-virtualized-geometry-in-unreal-engine
9. Unreal automation tests from the command line -- `-ExecCmds="Automation RunTest ...;Quit"`, `-ReportExportPath`, `-ResumeRunTest`. https://dev.epicgames.com/documentation/en-us/unreal-engine/run-automation-tests-in-unreal-engine
10. Scripting the Unreal Editor using Python -- `-ExecutePythonScript`, `-run=pythonscript`, level must be loaded explicitly in commandlet mode. https://dev.epicgames.com/documentation/en-us/unreal-engine/scripting-the-unreal-editor-using-python
11. Source control inside Unreal Editor -- check-out described as "lock it for editing"; config files must be checked in externally. https://dev.epicgames.com/documentation/en-us/unreal-engine/using-source-control-in-the-unreal-editor
12. Live Coding -- recompiles `.cpp` at runtime; header/constructor changes still require editor restart. https://dev.epicgames.com/documentation/en-us/unreal-engine/using-live-coding-to-recompile-unreal-engine-applications-at-runtime
13. Unreal MCP in Unreal Editor (5.8, experimental) -- in-editor MCP server, auto-start, tool registry. https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-mcp-in-unreal-editor
14. Unreal Engine 5.8 release notes (build toolchain: VS 2022/2026, .NET 10, Windows SDK). https://dev.epicgames.com/documentation/unreal-engine/unreal-engine-5-8-release-notes
15. Derived Data Cache -- why derived data exists, is large, and must be regenerated. https://dev.epicgames.com/documentation/unreal-engine/using-derived-data-cache-in-unreal-engine
16. Unreal licensing -- free under $1M gross per product, 5% royalty above (3.5% with EGS parity). https://www.unrealengine.com/license (the licence page redirects automated clients to login; figures cross-checked against Epic's published royalty FAQ and multiple 2026 summaries, e.g. https://300mind.studio/blog/unreal-engine-licensing-royalties)
17. Fab licences and pricing -- Personal tier under $100k gross revenue. https://dev.epicgames.com/documentation/fab/licenses-and-pricing-in-fab
18. Fab launch / Megascans pricing change -- Megascans free only until end of 2024, then largely paid. https://www.unrealengine.com/blog/fab-content-marketplace-launches-in-october-publishing-portal-opens-today

Secondary sources (reported, not vendor-authoritative):

19. Binary assets and version control in Unreal -- ".uasset is binary, cannot merge"; Epic staff note on visual diff/merge work. https://forums.unrealengine.com/t/pain-of-unreal-engine-binary-assets/24394
20. Unreal packaging size measurements -- blank UE5 shipping ~321 MB, blank development ~692 MB. https://www.strayspark.studio/blog/ue5-packaging-shipping-build-size-optimization
21. Shader compilation startup reports (20-30 min on first launch, recurring after updates). https://forums.unrealengine.com/t/ue5-compiling-shaders-on-every-startup/520971
22. Unreal test automation tooling survey (Automation/Spec/Functional/Gauntlet; latent commands "quite verbose"). https://andrewfray.wordpress.com/2025/04/09/the-topography-of-unreal-test-automation-in-2025/
23. UnrealSharp -- unofficial C# for UE5; FAQ states it is "not production ready yet". https://www.unrealsharp.com/faq
24. Unreal MCP connection instability report (5.8 experimental). https://forums.unrealengine.com/t/5-8-experimental-modelcontextprotocol-mcp-server-instantly-drops-connections/2729488
25. Unreal Engine 5.8 release (June 2026) and MCP announcement coverage. https://www.youtube.com/watch?v=F2cWJFcTft4

Project sources:

26. `PROJECT_STATE.md`, `MASTER_CONTEXT.md`, `GAME_DESIGN.md` (section 12 visual direction), `DEVELOPMENT_LOG.md`.
27. `.orchestrator/reports/2026-11-01-realism-pass.md` (rig failures, known visual defects).
28. `.orchestrator/reports/2026-11-01-tools-and-assets-research.md` (hardware, GPU, asset-source audit).
29. `TestResults/editmode-final.xml`, `playmode-final2.xml`, `build-final.log`, `captures-final.log` (timings, test counts, build size).

---

## 11. Checks performed and not performed

**Performed:** read mandatory context (`MASTER_CONTEXT.md`, `PROJECT_STATE.md`, `GUARDRAILS.md`, `GAME_DESIGN.md` section 12); inventoried source/test/editor line counts; parsed NUnit XML for test counts and durations; measured batch-run wall clock from log file timestamps; measured build output size; verified local hardware (RAM, CPU, VRAM, free disk), VS Build Tools, and absence of any Unreal install; fetched/verified vendor documentation listed in section 10.

**Not performed:** Unity was not launched. No test, build, or capture was re-run. Unreal Engine was not installed or run, so all Unreal timings on this hardware are unmeasured. No files were modified except this report. No packages, settings, or Git state were changed.

**Status:** recommendation issued; no approval gate crossed. The two follow-up actions (6.3 LTS move; character pipeline work) each require their own task and, for the LTS move, explicit approval.
