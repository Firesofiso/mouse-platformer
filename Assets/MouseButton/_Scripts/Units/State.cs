using System;
using System.Collections.Generic;
using System.Linq;
using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public abstract class State : MonoBehaviour {
    protected StateMachine _stateMachine;
    public StatefulUnit _unit => _stateMachine._unit;
    public State Substate { get; protected set; }
    protected Rigidbody2D Rb => _unit?.Rb;
    protected Animator Animator => _unit?.Animator;
    protected UnitInput Input => _unit?.Input;
    public bool IsComplete { get; protected set; }
    protected float _startTime;
    public float TimeElapsed => Time.time - _startTime;

    public void SetSubstate(State newSubstate) {
        if (Substate == newSubstate) return;

        Substate?.Exit();
        Substate = newSubstate;

        if (Substate == null) return;

        Substate.Initialize(_stateMachine);
        Substate.Enter();
    }

    public virtual void Enter() {
        // Debug.LogWarning($"[{name}] Cannot execute Do() — unit reference is null.");
    }

    public virtual void Do() {
        if (_unit == null) {
            Debug.LogWarning($"[{name}] Cannot execute Do() — unit reference is null.");
            return;
        }
        // Existing logic...
    }

    public virtual void FixedDo() { }

    public virtual void Exit() { }

    public void BuildCurrentStateHierarchy(List<State> h) {
        h.Add(this);
        if (Substate != null) {
            Substate.BuildCurrentStateHierarchy(h);
        }
    }

    public void Initialize(StateMachine parent) {
        _stateMachine = parent;
        IsComplete = false;
        ResetTime();
    }

    public void ResetTime() {
        _startTime = Time.time;
    }
    
    public List<State> GetActiveStateBranch(List<State> list = null) {
        if (list == null) {
            list = new List<State>();
        }

        list.Add(this);

        if (Substate == null) {
            return list;
        } else {
            return Substate.GetActiveStateBranch(list);
        }
    }
}

public abstract class PathfinderState : State {
    public PathfindingBrain Brain => _unit.Brain;
    public TargetDetectionSensor Sensor => _unit.Brain.Sensor;
}
