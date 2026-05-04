# State Machine

`_Scripts/Units/StateMachine/`

`StatefulUnit` (abstract `MonoBehaviour`) owns a root `State` and walks a substate chain each frame:

- `Update` → fires `Think` event (sensors subscribe here)
- `LateUpdate` → calls `DoState` recursively down the substate chain
- `FixedUpdate` → calls `FixedDoState` recursively

`State` is also a `MonoBehaviour` (lives as a child GameObject). States declare `AnimationHash`, `Enter/Exit/Do/DoPlayer/FixedDo`. Player-controlled units call `DoPlayer()`; AI units call `Do()`. Use `SetSubstate()` to layer states.

Enemy states for `MobbTrol` are in `_Scripts/Units/TROLs/MobbTrol/States/`.
