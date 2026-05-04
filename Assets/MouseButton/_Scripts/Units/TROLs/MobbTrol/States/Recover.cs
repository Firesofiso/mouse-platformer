using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Recover : State, IInputFilter
{
    private static readonly int Throw = Animator.StringToHash("Throw");
    private static readonly int ThrowTrip = Animator.StringToHash("ThrowTrip");

    private ITrolBrainContext Trol => ((MobbTrolUnit)_unit).Trol;

    private int _recoveryTime;

    public override void Enter()
    {
        _recoveryTime = Trol.LastThrowTripped ? 5 : 3;
        ResetTime();
        PlayAnimation(Trol.LastThrowTripped ? ThrowTrip : Throw);
    }

    public override void Do()
    {
        if (TimeElapsed > _recoveryTime)
        {
            Trol.SetRecovering();
            IsComplete = true;
        }
    }

    public void FilterInput(ref FrameInput input)
    {
        input.Move.x = 0;
        input.JumpDown = false;
        input.JumpHeld = false;
    }
}
