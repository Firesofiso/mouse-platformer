# Grab System

`_Scripts/ClickInteractions/`, `_Scripts/Player/Cursor/`

## Mechanic

The cursor grabs a world object and spring-pulls it back toward the player's home position. The cursor pins to the object; the object chases the cursor's natural home near the player.

## Architecture

| Class | Role |
|---|---|
| `CursorGrabber` | On the cursor. Detects `IGrabbable` via `OverlapCollider`, calls `OnGrabbed/WhileHeld/OnReleased`. Exposes `TargetPosition` — home position, set by `CursorController`. |
| `IGrabbable` | Interface: `OnGrabbed`, `OnReleased`, `WhileHeld`, `Config`. |
| `FreeGrabbable` | `IGrabbable` impl for physics objects. Creates a `TargetJoint2D` on grab targeting home; releases on drop. |
| `GrabConfig` | ScriptableObject. Spring parameters (`maxForce`, `frequency`, `dampingRatio`) for `TargetJoint2D`. |

## Spring feel

`TargetJoint2D` is Unity's force-based spring — it lives in Unity physics, not the custom velocity-write character controllers. `GrabConfig` controls how snappy vs. pendulum-like the drag feels. Heavier objects need proportionally more force to feel responsive at the same speed.
