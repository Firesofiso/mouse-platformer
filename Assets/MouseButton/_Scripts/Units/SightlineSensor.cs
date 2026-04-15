using Pathfinding;
using UnityEngine;

public class SightlineSensor : TargetDetectionSensor
{
    public bool HasSightline => isAwareOfTargetPosition;
    private int _obstacleLayerMask;

    protected void Start()
    {
        _obstacleLayerMask = LayerMask.GetMask("Ground", "climbable");
    }

    protected override void Think() {
        _perceivedDistanceToTarget = ConfirmSightline();
        // Debug.Log("perceived distance: " + _perceivedDistanceToTarget);
    }

    // returns perceived distance to target/point
    public float ConfirmSightline(Vector2? nonTargetSeekingPoint = null)
    {
        if (nonTargetSeekingPoint == null && Brain.destination.target == null)
            return float.PositiveInfinity;

        // if we aren't seeking a point in space, seek target
        Vector2 seekingPoint = nonTargetSeekingPoint ?? Brain.destination.target.position;
        float realDistanceToTarget = Vector2.Distance(Unit.Rb.position, seekingPoint);

        // if out of range, cannot see
        if (realDistanceToTarget > _detectionRange)
        {
            return float.PositiveInfinity;
        }

        RaycastHit2D hit = Physics2D.Linecast(Unit.Rb.position, seekingPoint, _obstacleLayerMask);

        // did not hit obstacle before target/point
        if (hit.collider == null) {
            Debug.Log("has sight");
            return realDistanceToTarget;
        } else {
            return float.PositiveInfinity;
        }
    }
}
