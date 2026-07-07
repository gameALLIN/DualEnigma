# AGENTS.md

## Project Overview

《双生迷城》(DualEnigma) — 2-player cooperative online 2D puzzle-building game built in Unity 2022 LTS. Players control Aqua (水人) and Ignis (火人) to build elemental defenses against escalating natural disasters. 3 rounds per match (~5 min), roguelite upgrades between rounds.

## Repository Layout

```
DualEnigma/
├── Client/                    # Unity 2022.3.62f3c1 project root
│   ├── Assets/
│   │   └── Scenes/           # Only Main.unity and Test.unity exist currently
│   ├── Packages/manifest.json
│   └── ProjectSettings/
├── GameDesign/                # All design documents (Markdown)
│   ├── DesignDocuments/      # GDD v3.0 (authoritative), chapter design
│   ├── NumericalDesign/      # Numerical tables v2.1
│   ├── LevelDesign/          # Level design doc
│   ├── ArtRequirements/      # Art style + asset specs
│   ├── AudioRequirements/    # Audio + SFX specs
│   └── VersionHistory/       # Change log (v1.0 → v2.1)
└── .codely-cli/              # Codely CLI tool config (gitignored at root)
    ├── agents/               # Agent TOML definitions
    └── skills/               # Canvas, Input System, uGUI, MCP builder
```

## Key Facts

- **Unity version**: 2022.3.62f3c1 — do NOT assume newer Unity features
- **Language**: C# (Unity), all game code goes in `Client/Assets/Scripts/`
- **Namespace convention**: `DualEnigma.{Module}` (e.g. `DualEnigma.Disaster`, `DualEnigma.Material`)
- **No C# files exist yet** — the project is in early prototyping; Scenes/ has only empty Main.unity and Test.unity
- **Design docs are the source of truth** — GDD v3.0 (`GameDesign/DesignDocuments/GDD_核心设计文档.md`) is the authoritative design document, 16 sections
- **All art is procedurally generated in code** — zero external asset dependency. Color palette: Aqua `#4FC3F7`, Ignis `#FF7043`, Background `#263238`
- **2D project** — `com.unity.feature.2d` and `com.unity.modules.physics2d` are active; use 2D physics, not 3D

## Codely CLI Agent System

This project uses Codely CLI with a multi-agent setup (`.codely-cli/agents/`):

| Agent | Role | Scope |
|-------|------|-------|
| `00` | Main dispatcher | Routes tasks to sub-agents |
| `main_coder` | Programming | `Client/Assets/Scripts/` C# implementation |
| `main_art` | Art | Procedural sprites, Shaders, prefabs, UI, particles |
| `main_designer` | Design | `GameDesign/` documentation and numerical balance |

**When working in this repo, respect the agent boundaries**: code changes go through `main_coder`, design doc edits through `main_designer`, art/visual work through `main_art`.

## Codely Skills

Available skills (`.codely-cli/skills/`):
- **Input_System** — Use Unity's new Input System (`com.unity.inputsystem` is NOT in manifest; add if needed). Never use legacy `Input.` class.
- **uGUI** — Canvas-based UI. Use fully qualified types (`UnityEngine.UI.Image`). Always use `InputSystemUIInputModule`, not `StandaloneInputModule`.
- **canvas-design** — Procedural visual art creation (posters, static visuals)
- **mcp-builder** — MCP server development (TypeScript recommended)

## Design Document Hierarchy

1. **GDD** (`DesignDocuments/GDD_核心设计文档.md`) — master design, all systems reference this
2. **章节策划** (`DesignDocuments/章节策划文档_v1.0.md`) — chapter/level structure (3 chapters × 12 levels)
3. **数值表** (`NumericalDesign/数值设计表_v2.1.md`) — all gameplay numbers, formulas, balance
4. **关卡设计** (`LevelDesign/关卡设计文档_v1.0.md`) — level layouts and flow
5. **美术/音频需求** — resource specs, style direction

**Version history matters** — `VersionHistory/版本记录.md` tracks all design pivots. v2.1 is current baseline. Some docs flagged as needing update (level design, art, audio) — check before trusting them.

## Git Ignore Rules

- `Client/` ignores Library, Temp, Logs, UserSettings, .csproj, .sln, build outputs
- `GameDesign/` ignores binary exports (.pdf, .psd, .xlsx) but keeps .md tracked
- `.codely-cli/` is gitignored at root level
- Do NOT commit Unity build artifacts (.apk, .aab, .ipa, .app)

## Open Items (from GDD §16)

- Art style not yet confirmed
- Engine choice verified as Unity, but networking tech still TBD
- Numerical balancing pending prototype testing
- Upgrade probability weights not finalized
