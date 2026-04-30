using TarodevController;
using TarodevController.Trol;

public class Dance : State, IInputFilter
{
    private MobbTrolController Controller => ((MobbTrolUnit)_unit).controller;

    public override void Enter()
    {
        Controller.SetDancing(3);
    }

    public override void Do()
    {
        if (!Controller.IsDancing)
        {
            Controller.MustCelebrate = false;
            IsComplete = true;
        }
    }

    public override void Exit()
    {
        Controller.MustCelebrate = false;
    }

    public void FilterInput(ref FrameInput input)
    {
        input.Move.x = 0;
        input.JumpDown = false;
        input.JumpHeld = false;
    }
}
