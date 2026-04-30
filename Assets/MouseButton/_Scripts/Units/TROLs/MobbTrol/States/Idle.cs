using UnityEngine;

public class Idle : State
{
    public override void Do() { }

    public override void Enter() {
        _unit.Brain?.StopGenerating();
        _unit.Brain?.StopTraversing();
        _unit.Brain?.ClearPath();
    }
}
