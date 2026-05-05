# Physics

`_Scripts/Units/Physics/`

## World scale

**1 px = 1 world unit.** All sprites are imported at `pixelsPerUnit = 1`. One tile is 8 × 8 units.

## Physics systems

Two custom systems, both velocity-write (not force-based):

### UnitController (Tarodev)
`_Scripts/Units/UnitController.cs`, `Assets/External/Tarodev 2D Controller/`

The platformer controller base. Drives the player and MobbTrol controller layer. Gravity, jump, acceleration, and wall/ledge/ladder logic are all parameterised via `ScriptableStats`. `MouseController` extends it for one-way platform drop-through. `MobbTrolController` extends it for AI with A* Pathfinding Project.

### GroundedPhysicsBody
`_Scripts/Units/Physics/GroundedPhysicsBody.cs`

Lightweight body used when a unit needs shared gravity state across components. Reads fall acceleration, max fall speed, and grounding force from `ScriptableStats`. Supports `IPhysicsModifier` for transient gravity overrides. Used by MobbTrol (free-fall layer) and GuineaPig.

## Unity gravity

`Physics2D.gravity` is left at the Unity default but has no gameplay effect — all character controllers write `_rb.velocity` directly every `FixedUpdate`, overriding it. The exception is `TrolSpear`, which uses Unity physics directly.

## ScriptableStats

Live in `Assets/MouseButton/Stat Presets/`. Each unit has its own asset. Tune values there — don't hardcode them.
