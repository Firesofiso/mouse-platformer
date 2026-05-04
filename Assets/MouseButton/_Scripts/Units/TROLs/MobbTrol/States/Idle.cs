using UnityEngine;

public class Idle : State
{
    private static readonly int IdleAnim = Animator.StringToHash("Idle");

    public override void Enter() {
        _unit.Brain?.StopGenerating();
        _unit.Brain?.StopTraversing();
        _unit.Brain?.ClearPath();
        PlayAnimation(IdleAnim);
    }

    public override void Do() { }
}
