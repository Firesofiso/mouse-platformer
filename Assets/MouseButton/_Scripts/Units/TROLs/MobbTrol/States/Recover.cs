using TarodevController;
using TarodevController.Trol;

public class Recover : State, IInputFilter
{
    private MobbTrolController Controller => ((MobbTrolUnit)_unit).controller;

    private int _recoveryTime;

    public override void Enter()
    {
        _recoveryTime = Controller.LastThrowTripped ? 5 : 3;
        ResetTime();
    }

    public override void Do()
    {
        if (TimeElapsed > _recoveryTime)
        {
            Controller.SetRecovering();
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
