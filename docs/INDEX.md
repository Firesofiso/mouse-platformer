# Architecture Index

- [units.md](units.md) — StatefulUnit/UnitController split, MobbTrol hierarchy, TrolManager
- [player.md](player.md) — MouseController, UnitInput, PlayerAnimator, PlayerObject
- [cursor.md](cursor.md) — CursorController modes (TrueCursor/Sidekick/FlyAway), CursorGrabber, InteractionManager/Target
- [grab.md](grab.md) — CursorGrabber, IGrabbable, PhysicsGrabbable, DirectGrabbable, PhysicsGrabConfig; cursor-grab mechanic
- [coupling.md](coupling.md) — Tight coupling contracts: CursorController↔CursorGrabber, IGrabbable interface, InteractionManager, PlayerObject collision ignore
- [state-machine.md](state-machine.md) — Hierarchical state machine powering all units (player and AI)
- [physics.md](physics.md) — GroundedPhysicsBody, ScriptableStats, Tarodev UnitController
- [ai.md](ai.md) — PathfindingBrain, SightlineSensor/TargetDetectionSensor, A* integration
- [dialogue.md](dialogue.md) — DialogueBubbles singleton, styles, bubble rendering
- [cutscenes.md](cutscenes.md) — CutsceneManager, CutsceneSequence, ICutsceneParticipant
- [scenes.md](scenes.md) — Scene layout, ZoneLoader, GameManager singleton
