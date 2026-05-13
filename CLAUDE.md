# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repo.

## Project

Unity **2022.3 LTS** 2D pixel-art platformer. Game code under `Assets/MouseButton/`. Third-party under `Assets/External/` — don't modify.

## Session Start

Run `unity-session-start` skill before anything else every session.

## Editor Workflow

No CLI build/test commands. All editor ops via **Unity MCP** (`mcp__UnityMCP__*` tools):

- **Inspect scene state / runtime values** → `execute_code`
- **Check for compile errors after script changes** → `read_console`
- **Modify prefabs** → `manage_prefabs` (prefer over runtime code workarounds)
- **Run in editor** → `manage_editor` (play/pause/stop)

Always run `read_console` after creating or editing scripts before anything else.

## Before non-trivial edits

For changes bigger than one-file fix, or any re-attempt after failed step, answer three checks before writing code:

1. **Measured?** Inspected failing state (logged values, RT contents, aspect, configs) — or guessing?
2. **Fallback?** What proves step worked, what proves it failed, plan B if it fails?
3. **Surprise signal?** Papering over asymmetric clue (e.g. one config works, another doesn't)?

Any answer "no" or "don't know" — stop and resolve before editing.

## Architecture

See [`docs/INDEX.md`](docs/INDEX.md).

## Memory

See [`memory/MEMORY.md`](memory/MEMORY.md).