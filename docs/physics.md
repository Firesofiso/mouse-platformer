# Physics

`_Scripts/Units/Physics/`

`GroundedPhysicsBody` drives gravity and grounding on top of `Rigidbody2D`. Physics tuning lives in `ScriptableStats` assets (`Assets/MouseButton/Stat Presets/`). The `IPhysicsModifier` interface lets states inject transient gravity/fall-speed overrides.

The platformer controller base (`UnitController`) comes from **Tarodev** (`Assets/External/Tarodev 2D Controller/`). `MouseController` extends it for one-way platform drop-through. `MobbTrolController` extends it for AI with A* Pathfinding Project.
