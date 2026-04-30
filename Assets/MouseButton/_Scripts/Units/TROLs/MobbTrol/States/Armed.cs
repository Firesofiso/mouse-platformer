using UnityEngine;

public class Armed : State
{
    [SerializeField] State _chase;
    [SerializeField] State _aim;
    [SerializeField] State _idle;

    [SerializeField] float _spearRange = 100;

    private SightlineSensor Vision => (SightlineSensor)Sensor;
    private bool WithinAimingRange => Vision.PerceivedDistanceToTarget < _spearRange;

    public override void Enter() { Debug.Log("Armed"); }

    // AI: route based on sensor awareness
    public override void Do() {
        if (Vision.IsAwareOfTargetPosition) {
            if (WithinAimingRange)
                SetSubstate(_aim);
            else
                SetSubstate(_chase);
        } else if (Vision.HasTargetPermanence && Vision.TargetPermanenceNotElapsed) {
            SetSubstate(_chase);
        } else {
            SetSubstate(_idle);
        }
    }

    // Player: route based on input + sensor
    public override void DoPlayer() {
        if (Input.FrameInput.AttackDown && Vision.IsAwareOfTargetPosition && WithinAimingRange) // swap AttackDown → AttackHeld once added to FrameInput
            SetSubstate(_aim);
        else
            SetSubstate(_idle);
    }
}
