# Units

`_Scripts/Units/`

## Component split

Each unit is a prefab with two sibling MonoBehaviours that divide responsibility:

| Component | Owns |
|---|---|
| `StatefulUnit` (abstract) | State machine, animator, input routing, `Think` event |
| `UnitController` (Tarodev, abstract) | Physics: movement, jumping, collisions, velocity |

`MobbTrolUnit : StatefulUnit` — concrete state-machine side. Holds `PathfindingBrain` and `MobbTrolController` references. Exposes `Brain`, `Sensor`, and `Trol` (the `ITrolBrainContext` interface into the controller).

`MobbTrolController : UnitController, ITrolBrainContext` — concrete physics side. Handles AI input gathering (`GatherAIInput` event), spear logic, bounce collisions, celebration state, and the `StateLocked` flag. Delegates velocity application to `GroundedPhysicsBody` when one is present.

## State hierarchy (MobbTrol)

```
Armed
├─ Patrol
├─ Chase       (AI only — PathfinderState)
├─ Aim
└─ Throw
Spearless
├─ Recover
├─ Reclaim
├─ Cheer
└─ Dance
```

States live in `_Scripts/Units/TROLs/MobbTrol/States/`. Each is a `MonoBehaviour` on a child GameObject of the unit prefab.

## TrolManager

`TrolManager` (referenced via `GameManager.instance.trolManager`) maintains `activeTrols` and `activeSpears` lists used by spear reclaim logic and population-level coordination.
