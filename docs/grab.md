# Grab System

`_Scripts/ClickInteractions/`, `_Scripts/Player/Cursor/`

## Mechanic

The cursor grabs a world object and is pinned to it — the object's mass, gravity, and friction now govern everything. A spring (`TargetJoint2D`) continuously pulls the object toward the cursor's natural home position. The cursor visual follows the held object at the exact grab offset, so the object appears to be dragged from wherever it was touched.

## Architecture

| Class | Role |
|---|---|
| `CursorGrabber` | On the cursor. Detects `IGrabbable` via `OverlapCollider`, calls `OnGrabbed/WhileHeld/OnReleased`. Exposes `SidekickHomePosition` (set by `CursorController` each frame) and `HeldCursorPosition` (grab-offset-corrected cursor position for visual pinning). |
| `IGrabbable` | Interface: `OnGrabbed`, `OnReleased`, `WhileHeld`, `Config`. |
| `FreeGrabbable` | `IGrabbable` impl for physics objects. Creates a `TargetJoint2D` on grab; its target tracks `SidekickHomePosition` each `FixedUpdate`. Disables player↔object collision while held. |
| `GrabConfig` | ScriptableObject. Spring parameters (`maxForce`, `frequency`, `dampingRatio`) for `TargetJoint2D`. |

## Cursor pinning while held

At grab time `CursorGrabber` records `HeldCursorOffset = cursorPos − itemPos`. Each frame `CursorController.FinalizePosition` snaps `transform.position` to `HeldCursorPosition` (= `itemPos + offset`) instead of the free-roam home. The cursor visual is anchored to the grab point on the item and rides its physics.

## Spring feel

`TargetJoint2D` is Unity's force-based spring — it lives in Unity physics, not the custom velocity-write character controllers. `GrabConfig` controls how snappy vs. pendulum-like the drag feels. Heavier objects need proportionally more force to feel responsive at the same speed. A fast-moving item at grab time will fling the cursor before the spring can correct.

## Legacy: ClickableElement

`_Scripts/ClickInteractions/ClickableElement.cs` — an older, simpler drag system that predates `FreeGrabbable`. Still active in the scene. Writes `objectTransform.position` directly to cursor position each frame (no physics/spring). Snaps to whole-unit on release.

`InteractionManager` checks both `ClickableElement.IsDragging` and `CursorGrabber.IsGrabbing` to suppress interaction targeting while anything is being dragged. The two systems don't conflict — `ClickableElement` is for non-physics objects where position-write drag is sufficient.
