using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Cheer : State
{
    private static readonly int CheerAnim = Animator.StringToHash("ReclaimSpear");

    private ITrolBrainContext Trol => ((MobbTrolUnit)_unit).Trol;

    public override void Enter() => PlayAnimation(CheerAnim);

    public override void Do()
    {
        if (Trol.IsGrounded) IsComplete = true;
    }
}
