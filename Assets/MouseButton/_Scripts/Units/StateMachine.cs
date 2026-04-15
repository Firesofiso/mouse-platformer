using UnityEngine;
using System.Collections.Generic;

public class StateMachine : MonoBehaviour {
    public State RootState { get; private set; }
    public StatefulUnit _unit;

    public void ApplyState(State newState, bool forceReset = false) {
        if (newState == null) {
            Debug.LogWarning("Attempted to apply a null state.");
            return;
        }

        if (RootState != newState || forceReset) {
            RootState?.Exit();
            RootState = newState;
            RootState.Initialize(this);
            RootState.Enter();
        }
    }

    public void ChangeState(State newState, bool forceReset = false) {
        ApplyState(newState, forceReset);
    }

    private void Update() {
        if (RootState != null) DoState(RootState);
    }

    private void FixedUpdate() {
        if (RootState != null) FixedDoState(RootState);
    }

    private void DoState(State state, HashSet<State> visited = null) {
        if (state == null) return;
        visited ??= new HashSet<State>();
        if (!visited.Add(state)) {
            Debug.LogError("Substate loop detected in DoState.");
            return;
        }

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
}
