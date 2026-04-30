using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public abstract class State : MonoBehaviour {
    protected StatefulUnit _unit;
    public State Substate { get; protected set; }
    protected Rigidbody2D Rb => _unit?.Rb;
    protected Animator Animator => _unit?.Animator;
    protected UnitInput Input => _unit?.Input;
    protected TargetDetectionSensor Sensor => _unit?.Sensor;
    public bool IsComplete { get; protected set; }
    protected float _startTime;
    public float TimeElapsed => Time.time - _startTime;

    public void SetSubstate(State newSubstate) {
        if (Substate == newSubstate) return;

        Substate?.Exit();
        Substate = newSubstate;

        if (Substate == null) return;

        Substate.Initialize(_unit);
        Substate.Enter();
    }

    public virtual void Enter() { }

    public virtual void Do() {
        if (_unit == null)
            Debug.LogWarning($"[{name}] Cannot execute Do() — unit reference is null.");
    }

    // Override for player-specific logic. Defaults to Do() so shared states need no changes.
    public virtual void DoPlayer() => Do();

    public virtual void FixedDo() { }

    public virtual void Exit() { }

    public void Initialize(StatefulUnit unit) {
        _unit = unit;
        IsComplete = false;
        Substate = null;
        ResetTime();
    }

    public void ResetTime() {
        _startTime = Time.time;
    }

    public void BuildCurrentStateHierarchy(List<State> h) {
        h.Add(this);
        Substate?.BuildCurrentStateHierarchy(h);
    }

    public List<State> GetActiveStateBranch(List<State> list = null) {
        list ??= new List<State>();
        list.Add(this);
        return Substate == null ? list : Substate.GetActiveStateBranch(list);
    }
}

// AI-only states that need direct pathfinding access extend this
public abstract class PathfinderState : State {
    public PathfindingBrain Brain => _unit.Brain;
}
