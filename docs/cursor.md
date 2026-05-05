# Cursor

`_Scripts/Player/Cursor/`

## Behavior

The cursor always chases **home** — a position offset from the player, flipping side based on facing. Three overrides in priority order:

1. **Grabbing** — cursor pins to the held object; the object springs back toward home
2. **Interactable nearby** — cursor flies to the nearest `InteractionTarget`; suppressed while grabbing
3. **Default** — cursor chases home

## Modes

| Mode | When |
|---|---|
| `Sidekick` | Normal gameplay — cursor chases the player's home offset |
| `TrueCursor` | Direct pointer control via mouse delta |
| `FlyAway` | One-shot arc offscreen for cutscene transitions |

## Components

| Component | Role |
|---|---|
| `CursorController` | Movement, mode switching, click/release events |
| `CursorGrabber` | Grab detection and lifecycle (`IGrabbable`) |
| `CursorAnimator` | Drives animator from controller + interaction events |
| `InteractionManager` | Selects nearest `InteractionTarget`, overrides cursor position |
