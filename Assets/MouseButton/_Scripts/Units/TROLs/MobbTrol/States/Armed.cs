using TarodevController;
using UnityEngine;

public class Armed : PathfinderState
{
    [SerializeField]
    PathfinderState _chase;
    [SerializeField]
    PathfinderState _aim;
    [SerializeField]
    PathfinderState _idle;

    [SerializeField]
    float spearRange = 100;
    private SightlineSensor Vision => (SightlineSensor)Sensor;

    public bool WithinAimingRange {
        get => Vision.PerceivedDistanceToTarget < spearRange;
    }
    
    public override void Enter() {Debug.Log("Armed"); }

    public override void Do() {
        if (Vision.HasSightline) {
            if (WithinAimingRange) // and grounded
            {
                SetSubstate(_aim);
            } else {
                SetSubstate(_chase);
            }
        } else {
            SetSubstate(_idle);
        }
    }
}
