# Coupling Map — Cursor / Grab / Player

Tight contracts that must stay consistent across all three systems.

---

## 1. `CursorController` ↔ `CursorGrabber` — bidirectional, every frame

**CC → CG (serialized reference `_grabber`)**
- `FinalizePosition` reads `_grabber.HeldTransform` each frame to detect grab state (null = free)
- `FinalizePosition` reads `_grabber.HeldCursorPosition` to pin the cursor visual to the held item
- `FinalizePosition` **writes** `_grabber.SidekickHomePosition` every frame — this is the spring target `FreeGrabbable` reads

**CG → CC (RequireComponent + static events)**
- `[RequireComponent(typeof(CursorController))]` — CursorGrabber cannot exist without CursorController on the same GameObject
- Subscribes to `CursorController.OnClick` / `OnRelease` (static events) via `OnEnable`/`OnDisable`
- `FlyAway` checks static `CursorGrabber.IsGrabbing` before firing `OnRelease` to avoid a missed release

**Invariant:** `SidekickHomePosition` must be written by `CursorController` before `CursorGrabber.LateUpdate` runs each frame, because `FreeGrabbable.WhileHeld` reads it immediately there.

---

## 2. `CursorGrabber` ↔ `IGrabbable` / `FreeGrabbable` — interface passes itself as context

**CG → IGrabbable**
- All three interface methods — `OnGrabbed(CursorGrabber)`, `WhileHeld(CursorGrabber)`, `OnReleased(CursorGrabber)` — take the grabber as the only argument. Every `IGrabbable` implementation is coupled to this concrete type; there is no generic context object.
- `TryGrab` casts `IGrabbable as MonoBehaviour` to read `.transform`. Implementations **must** be MonoBehaviours or `HeldTransform` will be null even for Physics-mode grabs.

**FreeGrabbable → CursorGrabber (outbound writes)**
- `OnGrabbed` writes `grabber.HeldTransform`: sets it to `MoveTarget` (Physics) or `null` (Direct). This controls whether CC pins the cursor visual to the item.
- `WhileHeld` reads `grabber.SidekickHomePosition` to update the `TargetJoint2D` target (Physics) or compute direct position (Direct).
- `WhileHeld` reads `grabber.HeldCursorOffset` to reconstruct the target position in Direct mode.

**Invariant:** `FreeGrabbable.OnGrabbed` must set `grabber.HeldTransform` **before** the same-frame `FinalizePosition` runs, or the cursor visual will snap to free position for one frame. In practice, grabs happen in `TryGrab` (click frame), `FinalizePosition` runs in `Update`, so the frame order is: `OnClick` fires → `TryGrab` → `OnGrabbed` writes `HeldTransform` → `Update` → `FinalizePosition` reads `HeldTransform`. This is safe.

---

## 3. `CursorController` ↔ `InteractionManager` — serialized reference + static polling

**CC → IM (serialized reference `_interactionManager`)**
- `FinalizePosition` reads `_interactionManager.CurrentTarget` (null = no active target)
- When non-null, reads `CurrentTarget.IconWorldPosition` as the SmoothDamp destination

**IM → CursorGrabber (static)**
- `Update` reads `CursorGrabber.IsGrabbing` each frame — interaction selection is fully suppressed while anything is held
- `SelectNearest` reads `CursorGrabber.CurrentHeldTransform` to skip `InteractionTarget` components that are children of the held object

**Priority contract (in `FinalizePosition`):**
1. `_grabber.HeldTransform != null` → grab wins, cursor pins to item
2. `_interactionManager.CurrentTarget != null` → interaction wins, cursor SmoothDamps to icon
3. Neither → cursor moves to free position

Changing this priority requires editing the if/else chain in `FinalizePosition`.

---

## 4. `FreeGrabbable` → `PlayerObject` — `FindObjectOfType` per event

`SetPlayerCollision(bool)` is called on every grab and release. It calls `Object.FindObjectOfType<PlayerObject>()` to locate the player, then iterates all collider pairs to toggle `Physics2D.IgnoreCollision`.

- **Cost:** linear scene scan twice per grab event. Fine at current scale; will hurt with many simultaneous grabs.
- **Coupling:** `FreeGrabbable` knows about `PlayerObject` by concrete type. Any class that needs to hold a grabbable object and have its colliders ignored must implement this separately.

---

## 5. `InteractionManager` → `CutsceneManager`, `DialogueInput` — static calls on E press

`HandleInteractPress` reads `CutsceneManager.IsPlaying` (static bool) to branch between dialogue advance and interaction trigger. When playing, it calls `DialogueInput.Fire()` (static). These are one-way static calls; neither system holds a reference back to `InteractionManager`.
