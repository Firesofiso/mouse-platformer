using TarodevController;
using UnityEngine;

public class Aim : State, IInputFilter
{
    [SerializeField] State _spearless;

    private int _timeToAim = 2;
    private bool ShouldThrow => TimeElapsed > _timeToAim;

    public override void Do() {
        if (ShouldThrow) {
            ((MobbTrolUnit)_unit).controller.LaunchSpear();
            _unit.ChangeState(_spearless);
        }
    }

    public override void Enter() {
        _unit.Brain?.StopGenerating(); // no-op on player units
        ((MobbTrolUnit)_unit).controller.IsAiming = true;
        ResetTime();
    }

    public override void Exit() {
        ((MobbTrolUnit)_unit).controller.IsAiming = false;
    }

    // Blocks movement and jump for any unit while aiming
    public void FilterInput(ref FrameInput input) {
        input.Move.x = 0;
        input.JumpDown = false;
        input.JumpHeld = false;
    }
}
