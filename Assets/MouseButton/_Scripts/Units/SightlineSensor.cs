using UnityEngine;

public abstract class TargetDetectionSensor : PathfindingComponent
{
    [SerializeField] internal int _detectionRange = 200;
    [SerializeField] internal float _perceivedDistanceToTarget = float.PositiveInfinity;
    public float PerceivedDistanceToTarget => _perceivedDistanceToTarget;
    public bool IsAwareOfTargetPosition => _perceivedDistanceToTarget != float.PositiveInfinity;

    [SerializeField] internal float _targetPermanenceDuration = float.PositiveInfinity;
    public bool HasTargetPermanence => _targetPermanenceDuration != float.PositiveInfinity;
    protected float _targetPermanenceStart;
    public bool TargetPermanenceNotElapsed => Time.time - _targetPermanenceStart < _targetPermanenceDuration;

    // Set by PathfindingBrain.UpdateTarget() for AI, or by a targeting component for players
    public Transform Target;

    public void ResetTargetPermanence() {
        _targetPermanenceStart = Time.time;
    }

    protected abstract void HandleTargetPermanence();
}

public class SightlineSensor : TargetDetectionSensor
{
    private int _obstacleLayerMask;

    protected void Start() {
        _obstacleLayerMask = LayerMask.GetMask("Ground", "climbable");
    }

    protected override void Think() {
        if (Target == null) return;
        float newDistance = ConfirmSightline();

        if (HasTargetPermanence && newDistance == float.PositiveInfinity)
            HandleTargetPermanence();

        _perceivedDistanceToTarget = newDistance;
    }

    public float ConfirmSightline(Vector2? seekingArbitraryPoint = null)
    {
        Vector2 seekingPoint = seekingArbitraryPoint ?? Target.position;
        float realDistanceToTarget = Vector2.Distance(Unit.Rb.position, seekingPoint);

        if (realDistanceToTarget > _detectionRange)
            return float.PositiveInfinity;

        RaycastHit2D hit = Physics2D.Linecast(Unit.Rb.position, seekingPoint, _obstacleLayerMask);
        return hit.collider == null ? realDistanceToTarget : float.PositiveInfinity;
    }

    protected override void HandleTargetPermanence() {
        if (IsAwareOfTargetPosition)
            ResetTargetPermanence();
    }
}
