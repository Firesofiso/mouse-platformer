# Grab System

`_Scripts/ClickInteractions/`, `_Scripts/Player/Cursor/`

## Mechanic

The cursor grabs a world object and interacts with it in one of two modes:

- **Physics** — the object has mass, gravity, and friction. A `TargetJoint2D` spring pulls it toward the cursor. The cursor visual pins to the held object and rides its physics. Feel is tuned per object via `PhysicsGrabConfig`.
- **Direct** — the object has no physics. Its `transform.position` is written directly to the cursor position each frame. The cursor visual stays at its natural position; the object silently follows.

## Architecture

| Class | Role |
|---|---|
| `CursorGrabber` | On the cursor. Detects `IGrabbable` via `OverlapCollider`, calls `OnGrabbed/WhileHeld/OnReleased` with a `GrabContext`. Exposes `GrabPoint` (item position + grab offset) for cursor pinning. Holds `_collisionIgnoreTargets` passed through context. |
| `GrabContext` | Struct passed to all `IGrabbable` methods: `HomePosition` (spring/move target), `CursorOffset` (grab-point delta), `IgnoreColliders`. |
| `IGrabbable` | Interface: `GrabAnchor`, `OnGrabbed`, `OnReleased`, `WhileHeld`. |
| `PhysicsGrabbable` | Physics implementation. Requires `Rigidbody2D` + `TargetJoint2D` (persistent, toggled enabled/disabled). `GrabAnchor` returns `transform` — cursor pins to object. Spring target tracks `HomePosition − CursorOffset` each frame. |
| `DirectGrabbable` | Direct implementation. No Rigidbody2D. `GrabAnchor` returns `null` — cursor stays at its natural position. Writes pixel-snapped position to `transform` each frame. |
| `PhysicsGrabConfig` | ScriptableObject. Spring parameters: `maxForce`, `frequency`, `dampingRatio`, `cursorCanLift`. **Heavy** and **light** presets differ only in these values — same component. |

## Presets

Three `PhysicsGrabConfig` presets ship under `Stat Presets/GrabStats/`:

| Preset | maxForce | frequency | dampingRatio | cursorCanLift | Feel |
|---|---|---|---|---|---|
| `light` | 2000 | 12 | 0.4 | ✓ | Snappy. Tracks cursor tightly. Can be lifted and flung. Slight overshoot on reversal. |
| `heavy` | 900 | 3 | 0.9 | ✗ | Sluggish. Lags behind cursor. Slides on ground, never hoists. Dead-weight stop on release. |
| `anchor` | 120 | 1.5 | 1.0 | ✗ | Barely budges. Critically damped — no spring feel at all, just slow resistance. For fixed environmental objects with a hair of give. |

**Why `light` uses frequency 12, not 5:** `TargetJoint2D` applies maximum force only when position error is large. As the spring closes the gap, force drops toward zero — while gravity is constant. With low frequency, the spring reaches near-equilibrium below the target and gravity wins; the object sags beneath the cursor instead of reaching it. High frequency keeps the spring aggressive at small errors, sustaining the upward pull through the full lift arc.

## Adding a grabbable object

**Physics (heavy or light):**
1. Add `Rigidbody2D` and `TargetJoint2D` to the object. Leave `TargetJoint2D` disabled — `PhysicsGrabbable` manages it.
2. Add `PhysicsGrabbable`. Assign a `PhysicsGrabConfig` preset (`heavy` or `light`).

**Direct:**
1. Add `DirectGrabbable`. No Rigidbody2D needed.

## GrabAnchor contract

- `PhysicsGrabbable` returns `transform` → `CursorController` pins its visual to the held object. The cursor rides the physics.
- `DirectGrabbable` returns `null` → no pinning. Cursor visual stays at free-roam position. Object silently tracks it.

## Spring feel (Physics only)

`TargetJoint2D` is Unity's force-based spring. `PhysicsGrabConfig` controls how snappy vs. pendulum-like the drag feels. Heavier objects need proportionally more `maxForce` to feel responsive. `cursorCanLift = false` clamps the spring target Y to the object's current Y, preventing the cursor from pulling the object upward.

## Cursor pinning while held

At grab time `CursorGrabber` records `GrabOffset = cursorPos − itemPos`. Each frame `CursorController.Update` detects `IsGrabbing && CurrentHeldTransform != null` and calls `PinToHeldItem`, which snaps `transform.position` to `GrabPoint` (= `HeldTransform.position + GrabOffset`). The cursor visual anchors to the grab point and rides the object's physics.
