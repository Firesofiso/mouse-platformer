# Player

`_Scripts/Player/Mouse/`, `_Scripts/Player/`

## Architecture

The player has no `StatefulUnit`; it drives animation directly from `UnitController` events. Four components on the player prefab:

| Component | Role |
|---|---|
| `MouseController` | Physics/movement — extends `UnitController` with player-specific overrides |
| `UnitInput` | Gathers `FrameInput` from Input System (or legacy) each frame and exposes it to `UnitController` |
| `PlayerAnimator` | Subscribes to `UnitController` events; drives Animator and particles |
| `PlayerObject` | DontDestroyOnLoad singleton — placeholder; collision exclusion is now handled via `CursorGrabber._collisionIgnoreTargets` |

## MouseController

Thin extension of `UnitController` (Tarodev):

- **CharSelect freeze** — skips `Update`/`FixedUpdate` while `IsInCharacterSelect` is set; `CharSelectController` toggles this flag.
- **One-way drop-through** — `HandleDropDownInput` only sets `_droppingDown` when actually standing on a one-way platform (guards against accidental drops on solid ground). `HandleDropDown` calls `OneWayPlatformBehaviour.AllowObjectPassThrough` on the hit platforms.
- **Ceiling exclusion** — `IsCeilingHitSolid` ignores colliders that have `OneWayPlatformBehaviour`, so upward velocity isn't killed by overhead one-way platforms.

## UnitInput

Gathers one `FrameInput` struct per `Update` when `isPlayerUnit` is true.

```
FrameInput { Move, JumpDown, JumpHeld, DropDown, DashDown, AttackDown, ClickDown }
```

`ClickDown` feeds into `CursorController` for grab/interact events. Works with both Unity Input System and legacy Input Manager.

## PlayerAnimator

Subscribes to `MouseController` events at `Start`: `GroundedChanged`, `WallGrabChanged`, `DashingChanged`, `LedgeClimbChanged`, `Jumped`, `AirJumped`, `Attacked`. Drives a flat Animator with named states (Run, Jump, Fall, WallClimb, LedgeClimb, etc.).

Notable: idle cycle randomly selects between `Idle`, `IdleWag`, and `IdleScritch` on a timer; particles sample the ground surface color to tint themselves.

## PlayerObject

Minimal DontDestroyOnLoad singleton. No active functionality — collision exclusion during grab is now serialized directly on `CursorGrabber._collisionIgnoreTargets`.
