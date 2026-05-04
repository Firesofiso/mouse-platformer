# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **2022.3 LTS** 2D pixel-art platformer. All game code lives under `Assets/MouseButton/`. Third-party code is under `Assets/External/` — don't modify it.

## Session Start

Run the `unity-session-start` skill before anything else in every session.

## Editor Workflow

There are no CLI build or test commands. All editor operations go through the **Unity MCP** (`mcp__UnityMCP__*` tools):

- **Inspect scene state / runtime values** → `execute_code`
- **Check for compile errors after script changes** → `read_console`
- **Modify prefabs** → `manage_prefabs` (prefer over runtime code workarounds)
- **Run in editor** → `manage_editor` (play/pause/stop)

Always run `read_console` after creating or editing scripts before doing anything else.

## Pixel Art Standards

- **16 pixels per unit** throughout
- Point-filter enforced on font textures via `Assets/Editor/FontTexturePointFilter.cs`
- Sprite sources are `.aseprite` files imported directly
- Snap positions to `1/16` unit grid where precision matters

## Architecture

See [`docs/INDEX.md`](docs/INDEX.md).

## Memory

See [`memory/MEMORY.md`](memory/MEMORY.md).
