using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Dance : State, IInputFilter
{
    private static readonly int DanceAnim = Animator.StringToHash("Dance");

    private ITrolBrainContext Trol => ((MobbTrolUnit)_unit).Trol;

    public override void Enter()
    {
        Trol.SetDancing(3);
        PlayAnimation(DanceAnim);
    }

    public override void Do()
    {
        if (!Trol.IsDancing)
        {
            Trol.MustCelebrate = false;
            IsComplete = true;
        }
    }

    public override void Exit()
    {
        Trol.MustCelebrate = false;
    }

    public void FilterInput(ref FrameInput input)
    {
        input.Move.x = 0;
        input.JumpDown = false;
        input.JumpHeld = false;
    }
}
