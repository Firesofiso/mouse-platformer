using UnityEngine;

public class Aim : PathfinderState {
    private int timeToAim = 2;
    private bool ShouldThrow => TimeElapsed > timeToAim;

    public override void Do() {
        if (ShouldThrow) {
            // controller.ThrowSpear?.Invoke();
            Brain.destination.target = null;
            IsComplete = true;
            Debug.Log("thrown!");
        } else {
            // controller.IsAiming = false;
            Debug.Log("keep aiming");
            IsComplete = true;
        }
    }

    public override void Enter() {
        base.Enter();
        Brain.ClearPath();
    }

    public override void Exit() {
        base.Exit();
    }
}