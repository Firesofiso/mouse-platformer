# AI

`_Scripts/Units/AI/`

## Perception

`PathfindingComponent` (abstract `MonoBehaviour`) — base for anything that runs per-frame perception. Subscribes to `StatefulUnit.Think` in `Awake`; subclasses implement `Think()`.

`TargetDetectionSensor` (abstract, extends `PathfindingComponent`) — tracks `PerceivedDistanceToTarget` and `Target`. Defined in `SightlineSensor.cs`.

`SightlineSensor` (concrete) — raycasts to the target each frame; returns `PositiveInfinity` when out of range or occluded. Supports optional target permanence (remembers last known position for a configurable duration).

## Pathfinding

`PathfindingBrain` + A* Pathfinding Project (`Assets/External/AstarPathfindingProject/`). Holds a `TargetDetectionSensor` reference, manages `Seeker`/`AIPath`/`AIDestinationSetter`, and exposes waypoint helpers to states.

States read `Sensor.PerceivedDistanceToTarget` and `Brain` waypoint data to drive transitions (Chase, Pursue, Patrol, etc.).
