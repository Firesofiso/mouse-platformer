using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Reclaim : PathfinderState
{
    [SerializeField] float _grabDistance = 10f;

    private MobbTrolController Controller => ((MobbTrolUnit)_unit).controller;

    public override void Enter()
    {
        Brain.UpdateTarget(Controller.SpearTransform);
        Brain.StartTraversing();
    }

    public override void Do()
    {
        if (Controller.SpearTransform == null) return;
        float dist = Vector2.Distance(_unit.Rb.position, Controller.SpearTransform.position);
        if (dist < _grabDistance)
            Controller.TriggerGrabSpear();
    }

    public override void Exit()
    {
        Brain.StopTraversing();
        Brain.RestorePrimaryTarget();
    }
}
