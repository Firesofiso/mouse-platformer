using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Aim : State, IInputFilter
{
    private static readonly int AimAnim = Animator.StringToHash("Aim");

    [SerializeField] State _spearless;

    private int _timeToAim = 2;
    private bool ShouldThrow => TimeElapsed > _timeToAim;

    private ITrolBrainContext Trol => ((MobbTrolUnit)_unit).Trol;

    public override void Enter() {
        _unit.Brain?.StopGenerating();
        Trol.IsAiming = true;
        ResetTime();
        PlayAnimation(AimAnim);
    }

    public override void Do() {
        if (ShouldThrow) {
            Trol.LaunchSpear();
            _unit.ChangeState(_spearless);
        }
    }

    public override void Exit() {
        Trol.IsAiming = false;
    }

    public void FilterInput(ref FrameInput input) {
        input.Move.x = 0;
        input.JumpDown = false;
        input.JumpHeld = false;
    }
}
