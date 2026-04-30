using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Spearless : State
{
    [SerializeField] State _recover;
    [SerializeField] State _reclaim;
    [SerializeField] State _cheer;
    [SerializeField] State _dance;
    [SerializeField] State _armed;

    private MobbTrolController Controller => ((MobbTrolUnit)_unit).controller;

    public override void Enter()
    {
        _recover.Initialize(_unit);
        _cheer.Initialize(_unit);
        _dance.Initialize(_unit);
    }

    public override void Do()
    {
        // Spear reclaimed — celebrate before returning to Armed
        if (!Controller.Spearless)
        {
            if (!_cheer.IsComplete)
                SetSubstate(_cheer);
            else if (!_dance.IsComplete)
                SetSubstate(_dance);
            else
                _unit.ChangeState(_armed);
            return;
        }

        // Still spearless — recover then reclaim
        if (!_recover.IsComplete)
            SetSubstate(_recover);
        else
            SetSubstate(_reclaim);
    }
}
