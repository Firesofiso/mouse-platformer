using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class PathfindingBrain : MonoBehaviour
{
    [SerializeField]
    public MobbTrolUnit unit;
    MobbTrolController controller => unit.controller;

    private FrameInput _aiFrameInput; // Frame-specific input data

    [Header("A* PATHFINDING PROJECT")]
    [SerializeField]
    public Seeker seeker;

    [SerializeField]
    public AIDestinationSetter destination;

    [SerializeField]
    public AIPath pathfinder;
    public int _currentWaypointIndex = -1;

    private Path _currentPath;
    public Path CurrentPath
    {
        get => _currentPath;
        set
        {
            if (value == null)
            {
                _currentWaypointIndex = 0;
            }
            _currentPath = value;
        }
    }
    private Vector2 CurrentWaypoint
    {
        get => (Vector2)_currentPath?.vectorPath?[_currentWaypointIndex];
    }
    private float DistanceToNextWaypoint
    {
        get => Vector2.Distance(transform.position, CurrentWaypoint);
    }
    private Vector2 DirectionToNextWaypoint
    {
        get => CurrentWaypoint - (Vector2)transform.position;
    }

    [SerializeField]
    public TargetDetectionSensor Sensor;

    public bool isThinking = true;

    public event Action Think;
    public FrameInput aiInput;

    public void Start()
    {
        InvokeRepeating(nameof(UpdatePath), 0, 0.25f);

        controller.GatherAIInput += OnGatherAIInput;

        controller.GetTrolSpeedAfterModifiers += OnGetTrolSpeedAfterModifiers;
    }

    private float OnGetTrolSpeedAfterModifiers()
    {
        float modifiedMaxSpeed = controller.Spearless
            ? controller._stats.MaxSpeed * 1.25f
            : controller._stats.MaxSpeed;
        if (destination.target != null)
        {
            modifiedMaxSpeed = Mathf.Clamp(
                controller._stats.MaxSpeed * CompoundSlowdownFactor(),
                controller._stats.MinSpeed,
                controller._stats.MaxSpeed
            );
        }
        return modifiedMaxSpeed;
    }

    // each slowdown factor contributes a portion of speed loss
    // when all factors are in full effect, slowdown is maximized
    private float CompoundSlowdownFactor()
    {
        // perceived proximity to target
        float targetProximityFactor = Mathf.Min(
            Sensor.PerceivedDistanceToTarget / pathfinder.slowdownDistance,
            1
        );
        // horizontal distance to next waypoint
        // takes away some slipperiness when the waypoint is above or below the target
        float horizontalOffsetFactor = Mathf.Min(
            Mathf.Abs(DirectionToNextWaypoint.x) / pathfinder.slowdownDistance,
            1
        );
        return (targetProximityFactor * 2 + horizontalOffsetFactor) / 3;
    }

    public void Update()
    {
        TryThink();
    }

    private void TryThink()
    {
        if (!isThinking)
        {
            return;
        }

        Think.Invoke();
    }

    FrameInput OnGatherAIInput()
    {
        aiInput = new() { Move = AssessPathing() };
        return aiInput;
    }

    private void UpdatePath()
    {
        if (!seeker.IsDone())
            return;

        if (Sensor.isAwareOfTargetPosition) { // & unit should be pursuing
            Debug.Log("update seen target");
            seeker.StartPath(unit.Rb.position, destination.target.position, OnPathProcessed);
        }
        // if (!PursuingOutOfSight)
        //     PursuingOutOfSight = true;
        // if (GaveUpOnPursuit)
        //     ClearPath();
    }

    private void UpdateTarget(Transform newTarget)
    {
        ClearPath();
        destination.target = newTarget;
        InvokeRepeating(nameof(UpdatePath), 0, 0.25f);
        if (newTarget == null) {
            return;
        }
    }

    public void ClearPath()
    {
        Debug.Log("Path: stop + clear");
        CancelInvoke("UpdatePath");

        // Stop movement and further searches
        if (pathfinder != null) {
            pathfinder.isStopped = true;
            pathfinder.canSearch = false;
            pathfinder.canMove = false;
            // Drop the current path and freeze velocity
            CurrentPath = null;            // also resets _currentWaypointIndex via setter
            pathfinder.SetPath(null);
            // Avoid repathing to stale destination
            pathfinder.destination = pathfinder.position;
        }

        // Cancel any pending path calculation
        if (seeker != null)
        {
            seeker.CancelCurrentPathRequest();
        }
    }

    private void OnPathProcessed(Path p)
    {
        if (p == null || p.error || p.vectorPath == null || !Sensor.isAwareOfTargetPosition)
        {
            return;
        }
        CurrentPath = p;

        _currentWaypointIndex = 0;
    }

    // simulates input for the unit to follow its pathfinding target
    internal virtual Vector2 AssessPathing()
    {
        Vector2 aiInputMove = Vector2.zero;

        if (
            CurrentPath?.vectorPath == null
            || _currentWaypointIndex >= CurrentPath.vectorPath.Count
            || Sensor.PerceivedDistanceToTarget < pathfinder.endReachedDistance
        )
        {
            return aiInputMove;
        }

        if (
            _currentWaypointIndex < CurrentPath.vectorPath.Count - 1
            && DistanceToNextWaypoint < pathfinder.pickNextWaypointDist
        )
        {
            _currentWaypointIndex++;
        }

        aiInputMove.x = DirectionToNextWaypoint.x > 0 ? 1 : -1;

        // AssessJumping();
        return aiInputMove;
    }
}
