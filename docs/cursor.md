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

`CursorController.Update` resolves position in this priority order each frame:

1. **Physics grab** (`IsGrabbing && CurrentHeldTransform != null`) — calls `PinToHeldItem`, which snaps `transform.position` to `CursorGrabber.GrabPoint` (held item position + grab offset). Skips all other logic.
2. **Normal update** — each mode (`Sidekick`/`TrueCursor`) computes a `freePos`, then calls `FinalizePosition(freePos)`:
   - Interaction target active → SmoothDamp toward `CurrentTarget.IconWorldPosition`
   - No target → set `transform.position = freePos`

`CarryTargetPosition` (static on `CursorController`, = `sidekickTarget.position + _carryOffset`) is what `CursorGrabber.BuildContext` reads as `GrabContext.HomePosition` — the spring/move target passed to held objects each frame.

**Direct grab note:** when `IsGrabbing` is true but `CurrentHeldTransform` is null (Direct mode), the cursor moves normally. `DirectGrabbable.WhileHeld` reads `GrabContext.CursorPosition` to track the cursor's position directly.

## Components

| Component | Role |
|---|---|
| `CursorController` | Movement, mode switching, click/release events, `PinToHeldItem`, `FinalizePosition` |
| `CursorGrabber` | Grab detection and lifecycle (`IGrabbable`); exposes `GrabPoint`, `HeldTransform`, `IsGrabbing` |
| `CursorAnimator` | Drives animator from controller + interaction events |
| `InteractionManager` | Selects nearest `InteractionTarget`, overrides cursor position; handles E key |

### InteractionManager — E key behavior

Lives on the Player. Each frame selects the nearest `InteractionTarget` within `_radius` on the interactable layer, suppressed while `CursorGrabber.IsGrabbing`.

E key press dispatches differently by context:
- **Cutscene playing** — forwards to `DialogueInput.Fire()` to advance dialogue.
- **Otherwise** — calls `InteractionTarget.Trigger()` on the current selection, which fires its `onInteract` UnityEvent.
