using System;
using System.Collections.Generic;
using System.Linq;
using TarodevController;
using UnityEngine;

public abstract class StatefulUnit : MonoBehaviour {
    [SerializeField] private Rigidbody2D _rb;
    public Rigidbody2D Rb => _rb;

    [SerializeField] private Animator _animator;
    public Animator Animator => _animator;

    [SerializeField] private UnitInput _input;
    public UnitInput Input => _input;

    public State RootState { get; private set; }

    [SerializeField] private State _initialState;
    public State InitialState => _initialState;
    public State CurrentState => RootState?.GetActiveStateBranch().Last();

    public virtual PathfindingBrain Brain => null;
    public virtual TargetDetectionSensor Sensor => null;

    // Fired every Update — sensors and perception components subscribe to this
    public event Action Think;

    protected virtual void Awake() {
        SetRootState(InitialState);
    }

    // Think fires before states (Update before LateUpdate)
    protected virtual void Update() {
        Think?.Invoke();
    }

    protected virtual void LateUpdate() {
        DoState(RootState);
    }

    protected virtual void FixedUpdate() {
        FixedDoState(RootState);
    }

    public void ChangeState(State newState, bool forceReset = false) {
        SetRootState(newState, forceReset);
    }

    // Walks the active state branch and applies any input filters declared by states
    public void FilterInput(ref FrameInput input) {
        if (RootState == null) return;
        foreach (var state in RootState.GetActiveStateBranch())
            if (state is IInputFilter f) f.FilterInput(ref input);
    }

    protected void SetRootState(State newState, bool forceReset = false) {
        if (newState == null) {
            Debug.LogWarning("Attempted to apply a null state.");
            return;
        }

        if (RootState != newState || forceReset) {
            if (RootState != null)
                foreach (var state in Enumerable.Reverse(RootState.GetActiveStateBranch()))
                    state.Exit();
            RootState = newState;
            RootState.Initialize(this);
            RootState.Enter();
        }
    }

    private void DoState(State state, HashSet<State> visited = null) {
        if (state == null) return;
        visited ??= new HashSet<State>();
        if (!visited.Add(state)) {
            Debug.LogError("Substate loop detected in DoState.");
            return;
        }
        if (Input != null && Input.isPlayerUnit)
            state.DoPlayer();
        else
            state.Do();
        DoState(state.Substate, visited);
    }

    private void FixedDoState(State state, HashSet<State> visited = null) {
        if (state == null) return;
        visited ??= new HashSet<State>();
        if (!visited.Add(state)) {
            Debug.LogError("Substate loop detected in FixedDoState.");
            return;
        }
        state.FixedDo();
        FixedDoState(state.Substate, visited);
    }

    private void OnDrawGizmos() {
#if UNITY_EDITOR
        if (Application.isPlaying && RootState != null) {
            List<State> states = RootState.GetActiveStateBranch();
            UnityEditor.Handles.Label(transform.position, "State: " + string.Join(" > ", states));
        }
#endif
    }
}
