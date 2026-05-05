# Units

`_Scripts/Units/`

## Component split

Each unit is a prefab with two sibling MonoBehaviours:

| Component | Owns |
|---|---|
| `StatefulUnit` (abstract) | State machine, animator, input routing, `Think` event |
| `UnitController` (Tarodev, abstract) | Physics: movement, jumping, collisions, velocity |

`MobbTrolUnit : StatefulUnit` — state-machine side. Holds `PathfindingBrain` and sensor references.

`MobbTrolController : UnitController` — physics side. Handles AI input, spear logic, and delegates to `GroundedPhysicsBody` when present.

## State hierarchy (MobbTrol)

```
Armed
├─ Patrol
├─ Chase
├─ Aim
└─ Throw
Spearless
├─ Recover
├─ Reclaim
├─ Cheer
└─ Dance
```

States live in `_Scripts/Units/TROLs/MobbTrol/States/`.

## TrolManager

Maintains `activeTrols` and `activeSpears` lists for spear reclaim and population-level coordination. Accessed via `GameManager.instance.trolManager`.
