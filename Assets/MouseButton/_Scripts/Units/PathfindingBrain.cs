using Pathfinding;
using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class PathfindingBrain : MonoBehaviour
{
    [SerializeField] public MobbTrolUnit unit;
    MobbTrolController controller => unit.controller;

    [Header("A* PATHFINDING PROJECT")]
    [SerializeField] public Seeker seeker;
    [SerializeField] public AIDestinationSetter destination;
    [SerializeField] public AIPath pathfinder;
    [SerializeField] public float _refreshPathInterval = 0.25f;

    public Transform PrimaryTarget { get; private set; }

    public Path CurrentPath { get; private set; }
    public int _currentWaypointIndex;

    private bool HasValidWaypoint => CurrentPath?.vectorPath != null
                                     && _currentWaypointIndex < CurrentPath.vectorPath.Count;
    private Vector2 CurrentWaypoint => HasValidWaypoint
                                       ? (Vector2)CurrentPath.vectorPath[_currentWaypointIndex]
                                       : Vector2.zero;
    private float DistanceToNextWaypoint => Vector2.Distance(transform.position, CurrentWaypoint);
    private Vector2 DirectionToNextWaypoint => CurrentWaypoint - (Vector2)transform.position;

    [SerializeField] public TargetDetectionSensor Sensor;

    // ── Generation ────────────────────────────────────────────────────────────
    private bool _isGenerating;

    internal void StartGenerating()
    {
        _isGenerating = true;
        CancelInvoke(nameof(UpdatePath));
        InvokeRepeating(nameof(UpdatePath), 0, _refreshPathInterval);
    }

    internal void StopGenerating()
    {
        _isGenerating = false;
        CancelInvoke(nameof(UpdatePath));
        seeker.CancelCurrentPathRequest();
    }

    private void UpdatePath()
    {
        if (!seeker.IsDone()) return;
        if (!Sensor.IsAwareOfTargetPosition) return;
        seeker.StartPath(unit.Rb.position, destination.target.position, OnPathComplete);
    }

    private void OnPathComplete(Path p)
    {
        if (p == null || p.error || p.vectorPath == null) return;
        CurrentPath = p;

        float minDist = float.MaxValue;
        _currentWaypointIndex = 0;
        for (int i = 0; i < p.vectorPath.Count; i++)
        {
            float d = Vector2.Distance(transform.position, p.vectorPath[i]);
            if (d < minDist) { minDist = d; _currentWaypointIndex = i; }
        }
    }

    // ── Traversal ─────────────────────────────────────────────────────────────
    private bool _isTraversing;

    internal void StartTraversing() => _isTraversing = true;
    internal void StopTraversing()  => _isTraversing = false;

    // ── Path data ─────────────────────────────────────────────────────────────
    internal void ClearPath()
    {
        CurrentPath = null;
        _currentWaypointIndex = 0;
    }

    internal void UpdateTarget(Transform newTarget)
    {
        destination.target = newTarget;
        if (Sensor != null) Sensor.Target = newTarget;
        if (newTarget == null) { StopGenerating(); ClearPath(); return; }
        StartGenerating();
    }

    internal void RestorePrimaryTarget() => UpdateTarget(PrimaryTarget);

    // ── Input ─────────────────────────────────────────────────────────────────
    internal virtual Vector2 AssessPathing()
    {
        if (
            !_isTraversing
            || CurrentPath?.vectorPath == null
            || _currentWaypointIndex >= CurrentPath.vectorPath.Count
            || Sensor.PerceivedDistanceToTarget < pathfinder.endReachedDistance
        )
            return Vector2.zero;

        if (
            _currentWaypointIndex < CurrentPath.vectorPath.Count - 1
            && DistanceToNextWaypoint < pathfinder.pickNextWaypointDist
        )
            _currentWaypointIndex++;

        return new Vector2(DirectionToNextWaypoint.x > 0 ? 1 : -1, 0);
    }

    // ── Unity ─────────────────────────────────────────────────────────────────
    public void Start()
    {
        controller.GatherAIInput += () => new FrameInput { Move = AssessPathing() };
        controller.GetTrolSpeedAfterModifiers += OnGetTrolSpeedAfterModifiers;

        // Sync sensor target with whatever destination was set in the inspector
        if (Sensor != null && destination.target != null)
            Sensor.Target = destination.target;

        PrimaryTarget = destination.target;
    }

    // ── Speed modifiers ───────────────────────────────────────────────────────
    private float OnGetTrolSpeedAfterModifiers()
    {
        float max = controller.Spearless
            ? controller._stats.MaxSpeed * 1.25f
            : controller._stats.MaxSpeed;

        if (destination.target != null)
            max = Mathf.Clamp(
                controller._stats.MaxSpeed * CompoundSlowdownFactor(),
                controller._stats.MinSpeed,
                controller._stats.MaxSpeed
            );

        return max;
    }

    private float CompoundSlowdownFactor()
    {
        float targetProximityFactor = Mathf.Min(
            Sensor.PerceivedDistanceToTarget / pathfinder.slowdownDistance, 1);
        float horizontalOffsetFactor = Mathf.Min(
            Mathf.Abs(DirectionToNextWaypoint.x) / pathfinder.slowdownDistance, 1);
        return (targetProximityFactor * 2 + horizontalOffsetFactor) / 3;
    }
}
