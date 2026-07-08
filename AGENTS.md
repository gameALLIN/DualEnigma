# AGENTS.md

## Project Overview

《双生迷城》(DualEnigma) — 2-player cooperative online 2D puzzle-building game built in Unity 2022 LTS. Players control Aqua (水人) and Ignis (火人) to collect fragments, synthesize materials, and build defenses against 35 types of natural disasters across 36 levels (3 chapters × 4 sections × 3 rounds). GDD v6.0 is the current design baseline.

## Repository Layout

```
DualEnigma/
├── Client/                    # Unity 2022.3.62f3c1 project root
│   ├── Assets/
│   │   └── Scenes/           # Only Main.unity and Test.unity exist currently
│   ├── Packages/manifest.json
│   └── ProjectSettings/
├── GameDesign/                # All design documents (Markdown)
│   ├── DesignDocuments/      # GDD v6.0 (authoritative), disaster/skill/talent/shelter/map design
│   ├── NumericalDesign/      # Numerical tables v2.1 (pending update for v6.0)
│   ├── LevelDesign/          # Level design doc
│   ├── ArtRequirements/      # Art style + asset specs
│   ├── AudioRequirements/    # Audio + SFX specs
│   └── VersionHistory/       # Change log (v1.0 → v6.0)
└── .codely-cli/              # Codely CLI tool config (gitignored at root)
    ├── agents/               # 8 Agent TOML definitions (mesh topology)
    └── skills/               # Canvas, Input System, uGUI, MCP builder, game-doc-workflow
```

## Key Facts

- **Unity version**: 2022.3.62f3c1 — do NOT assume newer Unity features
- **Language**: C# (Unity), all game code goes in `Client/Assets/Scripts/`
- **Namespace convention**: `DualEnigma.{Module}` (e.g. `DualEnigma.Disaster`, `DualEnigma.Material`)
- **No C# files exist yet** — the project is in early prototyping; Scenes/ has only empty Main.unity and Test.unity
- **Design docs are the source of truth** — GDD v6.0 (`GameDesign/DesignDocuments/GDD_核心设计文档.md`) is the authoritative design document
- **All art is procedurally generated in code** — zero external asset dependency. Color palette: Aqua `#4FC3F7`, Ignis `#FF6F00`, Background `#263238`
- **2D project** — `com.unity.feature.2d` and `com.unity.modules.physics2d` are active; use 2D physics, not 3D

## Codely CLI Agent System

This project uses Codely CLI with a multi-agent setup (`.codely-cli/agents/`) in a **mesh topology**:

| Agent | Role | Scope |
|-------|------|-------|
| `00` | Main dispatcher | Initial routing + cross-domain coordination |
| `main_coder` | Programming | `Client/Assets/Scripts/` C# implementation, DualEnigma.{Module} |
| `main_art` | Art | Procedural sprites, Shaders, prefabs, UI, particles |
| `main_designer` | Design | `GameDesign/` documentation, GDD iteration, numerical balance |
| `main_network` | Network | Netcode/Mirror sync, room management, latency compensation |
| `main_qa` | Testing | Automated tests, bug reproduction, compile checks |
| `main_audio` | Audio | AudioSource, SFX system, BGM, procedural audio |
| `main_level` | Level | Level layout, disaster waves, difficulty curve, blueprints |

**Mesh topology**: Sub-agents can directly call each other via `task` tool without going through `00`. The `00` agent handles initial routing and complex cross-domain task decomposition.

**When working in this repo, respect the agent boundaries**: code changes go through `main_coder`, design doc edits through `main_designer`, art/visual work through `main_art`, network sync through `main_network`, testing through `main_qa`, audio through `main_audio`, level config through `main_level`.

## Codely Skills

Available skills (`.codely-cli/skills/`):
- **Input_System** — Use Unity's new Input System (`com.unity.inputsystem` is NOT in manifest; add if needed). Never use legacy `Input.` class.
- **uGUI** — Canvas-based UI. Use fully qualified types (`UnityEngine.UI.Image`). Always use `InputSystemUIInputModule`, not `StandaloneInputModule`.
- **canvas-design** — Procedural visual art creation (posters, static visuals)
- **mcp-builder** — MCP server development (TypeScript recommended)

## Design Document Hierarchy

1. **GDD** (`DesignDocuments/GDD_核心设计文档.md`) — master design v6.0, all systems reference this
2. **灾难系统设计** (`DesignDocuments/灾难系统设计.md`) — 35 disasters, 6 categories, environment mapping v2.0
3. **技能系统设计** (`DesignDocuments/技能系统设计.md`) — Card selection (3E+3Q), passive, combo v2.1
4. **天赋系统设计** (`DesignDocuments/天赋系统设计.md`) — 48 talents, 36 selections, stackable v2.0
5. **双生庇护系统设计** (`DesignDocuments/双生庇护系统设计.md`) — Energy system, 5 shelter types v2.1
6. **地图设计** (`DesignDocuments/地图设计.md`) — 40×20 grid map, 15×8 build zone v3.1
7. **章节策划文档** (`DesignDocuments/章节策划文档_v2.0.md`) — 3×4×3=36 levels, 7-phase 90s timeline, disaster allocation
8. **数值表** (`NumericalDesign/数值设计表_v2.1.md`) — pending update for v6.0
9. **关卡设计** (`LevelDesign/关卡设计文档_v1.0.md`) — pending update for v6.0
10. **美术/音频需求** — resource specs, style direction (pending)

**Version history matters** — `VersionHistory/版本记录.md` tracks all design pivots. v6.0 is current baseline. Some docs flagged as needing update (numerical, level design, art, audio) — check before trusting them.

## Git Ignore Rules

- `Client/` ignores Library, Temp, Logs, UserSettings, .csproj, .sln, build outputs
- `GameDesign/` ignores binary exports (.pdf, .psd, .xlsx) but keeps .md tracked
- `.codely-cli/` is gitignored at root level
- Do NOT commit Unity build artifacts (.apk, .aab, .ipa, .app)

## Open Items (from GDD v6.0)

- Build zone exact size pending prototype testing (temp 15×8 grid)
- Numerical balancing pending prototype testing
- 35 disaster visual details pending art confirmation
- E3 enhanced version numerical balance pending prototype testing
- Networking tech (Netcode vs Mirror) still TBD
- Art/audio requirement docs pending finalization
