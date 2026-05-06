# Cursor

`_Scripts/Player/Cursor/`

## Behavior

The cursor always chases **home** — a position offset from the player, flipping side based on facing. Three overrides in priority order:

1. **Grabbing** — cursor pins to the held object at the grab offset; the object springs back toward home
2. **Interactable nearby** — cursor flies to the nearest `InteractionTarget`; suppressed while grabbing
3. **Default** — cursor chases home

## Modes

| Mode | When |
|---|---|
| `Sidekick` | Normal gameplay — cursor chases the player's home offset autonomously |
| `TrueCursor` | Direct pointer control via mouse delta |
| `FlyAway` | One-shot arc offscreen for cutscene transitions |

## Position pipeline

Each mode computes its desired position independently, then calls `FinalizePosition(freePos)`:

1. Writes `freePos` to `CursorGrabber.SidekickHomePosition` — the spring target for held objects.
2. If grabbing: sets `transform.position` to `CursorGrabber.HeldCursorPosition` (item position + grab offset).
3. Otherwise: sets `transform.position` to `freePos`.

This makes grab behavior identical across all modes — the grab system only reads `SidekickHomePosition` and `HeldCursorPosition`, never the mode.

## Components

| Component | Role |
|---|---|
| `CursorController` | Movement, mode switching, click/release events, `FinalizePosition` |
| `CursorGrabber` | Grab detection and lifecycle (`IGrabbable`); exposes `SidekickHomePosition` and `HeldCursorPosition` |
| `CursorAnimator` | Drives animator from controller + interaction events |
| `InteractionManager` | Selects nearest `InteractionTarget`, overrides cursor position; handles E key |

### InteractionManager — E key behavior

Lives on the Player. Each frame selects the nearest `InteractionTarget` within `_radius` on the interactable layer, suppressed while `ClickableElement.IsDragging` or `CursorGrabber.IsGrabbing`.

E key press dispatches differently by context:
- **Cutscene playing** — forwards to `DialogueInput.Fire()` to advance dialogue.
- **Otherwise** — calls `InteractionTarget.Trigger()` on the current selection, which fires its `onInteract` UnityEvent.
