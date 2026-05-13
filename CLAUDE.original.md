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

## Before non-trivial edits

For changes bigger than a one-file fix, or any re-attempt after a failed step, answer three checks before writing code:

1. **Measured?** Have I inspected the failing state (logged values, RT contents, aspect, configs) — or am I guessing?
2. **Fallback?** What proves this step worked, what proves it failed, and what's plan B if it fails?
3. **Surprise signal?** Am I papering over an asymmetric clue (e.g. one config works, another mysteriously doesn't)?

If any answer is "no" or "don't know," stop and resolve before editing.

## Architecture

See [`docs/INDEX.md`](docs/INDEX.md).

## Memory

See [`memory/MEMORY.md`](memory/MEMORY.md).
