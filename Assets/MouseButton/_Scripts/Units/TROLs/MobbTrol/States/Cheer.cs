using TarodevController;
using TarodevController.Trol;

public class Cheer : State
{
    private MobbTrolController Controller => ((MobbTrolUnit)_unit).controller;

    public override void Do()
    {
        if (Controller.IsGrounded) IsComplete = true;
    }
}
