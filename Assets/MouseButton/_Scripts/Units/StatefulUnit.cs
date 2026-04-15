using System;
using System.Collections.Generic;
using System.Linq;
using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public abstract class StatefulUnit : MonoBehaviour {
    [SerializeField] private Rigidbody2D _rb;
    public Rigidbody2D Rb => _rb;

    [SerializeField] private Animator _animator;
    public Animator Animator => _animator;

    [SerializeField] private UnitInput _input;
    public UnitInput Input => _input;

    [SerializeField]
    private StateMachine _rootMachine;

    public State RootState => _rootMachine.RootState;

    [SerializeField] private State _initialState;
    public State InitialState => _initialState;
    public State CurrentState => GetCurrentState();
    public virtual PathfindingBrain Brain => null;

    protected virtual void Awake() {
        SetupStateMachineInstance();
        SetRootState(InitialState);
    }

    protected virtual void Update() { }

    public void SetupStateMachineInstance() {
        _rootMachine._unit = this;
    }

    protected void SetRootState(State newState, bool forceReset = false) {
        _rootMachine.ApplyState(newState, forceReset);
    }

    public void ChangeState(State newState, bool forceReset = false) {
        SetRootState(newState, forceReset);
    }

    protected List<State> GetCurrentStateHierarchy() {
        List<State> h = new();
        if (RootState != null) RootState.BuildCurrentStateHierarchy(h);
        return h;
    }

    protected State GetCurrentState() {
        return RootState.GetActiveStateBranch().Last();
    }
    
    private void OnDrawGizmos() {
        // print out all of the active states in the tree
#if UNITY_EDITOR
        if (Application.isPlaying && RootState != null) {
            List<State> states = RootState.GetActiveStateBranch();
            UnityEditor.Handles.Label(transform.position, "State: " + string.Join(" > ", states));
        }
#endif
    }
}
