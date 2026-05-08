# Coupling Map — Cursor / Grab / Player

Tight contracts that must stay consistent across all three systems.

---

## 1. `CursorController` ↔ `CursorGrabber` — bidirectional, every frame

**CC → CG (serialized reference `_grabber`)**
- `Update` reads `CursorGrabber.IsGrabbing` and `CursorGrabber.CurrentHeldTransform` to decide whether to call `PinToHeldItem` or run normal cursor logic
- `PinToHeldItem` reads `_grabber.GrabPoint` to snap `transform.position` to the held item each frame
- `BuildContext` reads `sidekickTarget.transform.position + _carryOffset` and exposes it as `CarryTargetPosition` — this is the `HomePosition` that `CursorGrabber` passes to `IGrabbable.WhileHeld`

**CG → CC (RequireComponent + static events)**
- `[RequireComponent(typeof(CursorController))]` — `CursorGrabber` cannot exist without `CursorController` on the same GameObject
- Subscribes to `CursorController.OnClick` / `OnRelease` (static events) via `OnEnable`/`OnDisable`
- `FlyAway` checks static `CursorGrabber.IsGrabbing` before firing `OnRelease` to avoid a missed release

**`PinToHeldItem` vs. normal update (in `CursorController.Update`):**
- `IsGrabbing && CurrentHeldTransform != null` → physics grab; cursor pins to object via `PinToHeldItem`
- `IsGrabbing && CurrentHeldTransform == null` → direct grab; cursor moves normally; `DirectGrabbable.WhileHeld` tracks it
- Neither → free cursor or sidekick

**Invariant:** `CarryTargetPosition` must be computed from the current frame's `sidekickTarget` position. `CursorGrabber.LateUpdate` reads it via `BuildContext` → `GrabContext.HomePosition`. No stale value.

---

## 2. `CursorGrabber` ↔ `IGrabbable` — interface receives `GrabContext`, not the grabber

All three interface methods take a `GrabContext` struct, not `CursorGrabber` directly. A second grabber type can construct its own `GrabContext` and reuse any `IGrabbable` without change.

**`GrabAnchor` property**
- `CursorGrabber.TryGrab` reads `best.GrabAnchor` to set `HeldTransform`. The grabbable declares its pin point; the grabber owns its own state.
- `GrabAnchor == null` → don't pin the cursor visual (Direct mode). Cursor moves freely; object follows transform.
- `GrabAnchor != null` → pin cursor to that transform (Physics mode). Cursor rides the object.

**`GrabContext` fields**
- `HomePosition` — spring target (`PhysicsGrabbable`) or direct-move target (`DirectGrabbable`). Set from `CursorController.CarryTargetPosition`.
- `CursorPosition` — `transform.position` of the cursor at call time. Used by `DirectGrabbable` to write object position.
- `IgnoreColliders` — colliders to toggle with `Physics2D.IgnoreCollision` on grab/release; populated from `CursorGrabber._collisionIgnoreTargets` (point at player colliders).

**Invariant:** `GrabContext` is a snapshot. `WhileHeld` gets a fresh context each `LateUpdate`; `OnGrabbed` and `OnReleased` each get one context built at that instant. Don't cache across frames.

---

## 3. `CursorController` ↔ `InteractionManager` — serialized reference + static polling

**CC → IM (serialized reference `_interactionManager`)**
- `FinalizePosition` reads `_interactionManager.CurrentTarget` (null = no active target)
- When non-null, reads `CurrentTarget.IconWorldPosition` as the SmoothDamp destination

**IM → CursorGrabber (static)**
- `Update` reads `CursorGrabber.IsGrabbing` — interaction selection is fully suppressed while anything is held
- `SelectNearest` reads `CursorGrabber.CurrentHeldTransform` to skip `InteractionTarget` components that are children of the held object

**Priority contract (in `FinalizePosition`):**
1. `_grabber.HeldTransform != null` → grab wins, cursor pins to item
2. `_interactionManager.CurrentTarget != null` → interaction wins, cursor SmoothDamps to icon
3. Neither → cursor moves to free position

Changing this priority requires editing the if/else chain in `FinalizePosition`.

---

## 4. `InteractionManager` → `CutsceneManager`, `DialogueInput` — static calls on E press

`HandleInteractPress` reads `CutsceneManager.IsPlaying` (static bool) to branch between dialogue advance and interaction trigger. When playing, it calls `DialogueInput.Fire()` (static). These are one-way static calls; neither system holds a reference back to `InteractionManager`.
