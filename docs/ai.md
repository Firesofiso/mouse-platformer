# AI

`_Scripts/Units/AI/`

## Perception

`SightlineSensor` raycasts to the target each frame and tracks perceived distance. Supports optional target permanence — remembers last known position for a configurable duration after losing line of sight. Sensors subscribe to `StatefulUnit.Think` so perception runs in sync with the state machine.

## Pathfinding

`PathfindingBrain` wraps A* Pathfinding Project (`Assets/External/AstarPathfindingProject/`). States read sensor distance and brain waypoints to drive transitions (Chase, Patrol, etc.).
