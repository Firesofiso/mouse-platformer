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
    private bool _jumpHeld;

    private FrameInput GatherInput()
    {
        var move  = AssessPathing();
        var input = new FrameInput { Move = move };
        AssessJumping(ref input, move.x);
        return input;
    }

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

    // ── Jumping ───────────────────────────────────────────────────────────────
    private bool _leftGroundAfterJump;

    [Header("Jumping")]
    [SerializeField] private float _pathfinderJumpThreshold = 20f;
    [SerializeField] private float _obstacleDetectionDistance = 2f;
    [Tooltip("Jump when next A* waypoint is at least this many units above the trol")]
    [SerializeField] private float _waypointHeightThreshold = 0.5f;
    [Tooltip("How far ahead of the collider edge to cast the ledge-detection ray")]
    [SerializeField] private float _ledgeLookahead = 3f;
    [Tooltip("If sightline target is at least this many units above, allow jump even without upward waypoint")]
    [SerializeField] private float _targetAboveMinY = 1.5f;
    [Tooltip("Target is considered directly above when horizontal offset is within this range")]
    [SerializeField] private float _targetAboveMaxAbsX = 2f;
    [Tooltip("If true, horizontal allowance scales with collider width to avoid false negatives")]
    [SerializeField] private bool _scaleAboveXByColliderWidth = true;

    private int _groundMask;
    private int _obstacleMask;

    private void AssessJumping(ref FrameInput input, float moveX)
    {
        input.JumpDown = false;
        if (!_isTraversing) { _jumpHeld = false; return; }

        if (_jumpHeld)
        {
            _jumpHeld = ShouldContinueJump();
            input.JumpHeld = _jumpHeld;
        }
        else if (ShouldInitiateJump(moveX))
        {
            input.JumpDown = true;
            input.JumpHeld = true;
            _jumpHeld = true;
            _leftGroundAfterJump = false;
        }
    }

    private bool ShouldInitiateJump(float moveX)
    {
        if (!controller.IsGrounded) return false;

        bool targetUp   = IsTargetDirectlyAbove();
        bool waypointUp = IsWaypointAbove();
        bool obstacle   = waypointUp && IsObstacleAhead(moveX);
        bool ledge      = waypointUp && IsLedgeAhead();

#if UNITY_EDITOR
        RecordDebug(moveX, obstacle, targetUp, waypointUp);
#endif

        bool shouldJump = targetUp || waypointUp || obstacle || ledge;
#if UNITY_EDITOR
        if (shouldJump)
        {
            string reason = ledge ? "ledge"
                : obstacle        ? "jumped bc there's an obstacle in my way!"
                : targetUp        ? "jumped bc target is directly above me!"
                :                   "jumped bc my waypoint is above me!";
            _dbgLastJumpReason = reason;
            if (_logJumpReasons) Debug.Log($"[PathfindingBrain] jump reason: {reason}");
        }
#endif
        return shouldJump;
    }

    private bool ShouldContinueJump()
    {
        if (!_leftGroundAfterJump)
        {
            if (!controller.IsGrounded) _leftGroundAfterJump = true;
            return true; // hold jump until we've actually lifted off
        }
        if (controller.IsGrounded) return false; // landed — release so next jump can trigger
        bool nearTarget = Sensor.PerceivedDistanceToTarget < _pathfinderJumpThreshold;
        return !(nearTarget && DirectionToNextWaypoint.y < 0);
    }

    // Jump when the A* path routes upward (requires jump links configured in graph)
    private bool IsWaypointAbove() =>
        HasValidWaypoint && DirectionToNextWaypoint.y > _waypointHeightThreshold;

    private bool IsTargetDirectlyAbove()
    {
        if (Sensor == null || !Sensor.IsAwareOfTargetPosition || Sensor.Target == null) return false;

        Vector2 source = unit.Rb.position;
        if (controller?._environmentCol != null)
            source = controller._environmentCol.bounds.center;

        Vector2 toTarget = (Vector2)Sensor.Target.position - source;
        float horizontalAllowance = _targetAboveMaxAbsX;
        if (_scaleAboveXByColliderWidth && controller?._environmentCol != null)
        {
            float colliderAllowance = controller._environmentCol.bounds.extents.x * 1.25f;
            horizontalAllowance = Mathf.Max(horizontalAllowance, colliderAllowance);
        }

        return toTarget.y > _targetAboveMinY && Mathf.Abs(toTarget.x) <= horizontalAllowance;
    }

    private bool IsLedgeAhead()
    {
        float halfW = controller._environmentCol.size.x / 2f;
        float halfH = controller._environmentCol.size.y / 2f;
        float dir = DirectionToNextWaypoint.x > 0 ? 1 : -1;
        var origin = unit.Rb.position + new Vector2(dir * (halfW + _ledgeLookahead), -halfH);
        var hit = Physics2D.Raycast(origin, Vector2.down, controller.PlayerStats.AfraidOfHeight, _groundMask);
        Debug.DrawRay(origin, Vector2.down * controller.PlayerStats.AfraidOfHeight,
            hit.collider == null ? Color.red : Color.green);
        return hit.collider == null;
    }

    private bool IsObstacleAhead(float moveX)
    {
        // Use move direction; fall back to velocity if pathing returned zero (trol pressed against wall)
        float dir;
        if (Mathf.Abs(moveX) > 0.01f)
            dir = Mathf.Sign(moveX);
        else if (Mathf.Abs(unit.Rb.velocity.x) > 0.01f)
            dir = Mathf.Sign(unit.Rb.velocity.x);
        else
            return false; // no horizontal intent — skip

        float halfW = controller._environmentCol.size.x / 2f;
        float halfH = controller._environmentCol.size.y / 2f;
        var direction = new Vector2(dir, 0);

        // Cast at center and at foot level to catch low walls and platform lips
        var originCenter = unit.Rb.position + new Vector2(dir * halfW, 0);
        var originFoot   = unit.Rb.position + new Vector2(dir * halfW, -halfH + 0.2f);

        var hitCenter = Physics2D.Raycast(originCenter, direction, _obstacleDetectionDistance, _obstacleMask);
        var hitFoot   = Physics2D.Raycast(originFoot,   direction, _obstacleDetectionDistance, _obstacleMask);

        Debug.DrawRay(originCenter, direction * _obstacleDetectionDistance, hitCenter.collider ? Color.red : Color.green);
        Debug.DrawRay(originFoot,   direction * _obstacleDetectionDistance, hitFoot.collider   ? Color.red : Color.green);

        return hitCenter.collider || hitFoot.collider;
    }

    // ── Unity ─────────────────────────────────────────────────────────────────
    public void Start()
    {
        controller.GatherAIInput += GatherInput;
        controller.GetTrolSpeedAfterModifiers += OnGetTrolSpeedAfterModifiers;

        _groundMask   = LayerMask.GetMask("Ground", "one-way", "climbable");
        _obstacleMask = LayerMask.GetMask("Ground", "one-way", "climbable");

        if (Sensor != null && destination.target != null)
            Sensor.Target = destination.target;

        PrimaryTarget = destination.target;
    }

    // ── Speed modifiers ───────────────────────────────────────────────────────
    private float OnGetTrolSpeedAfterModifiers()
    {
        var stats = controller.PlayerStats;
        float max = controller.Spearless
            ? stats.MaxSpeed * 1.25f
            : stats.MaxSpeed;

        if (destination.target != null)
            max = Mathf.Clamp(
                stats.MaxSpeed * CompoundSlowdownFactor(),
                stats.MinSpeed,
                stats.MaxSpeed
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

    // ── Debug ──────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool _showDebug = true;
    [SerializeField] private bool _logJumpReasons = false;

    private string _dbgObstacleLayer = "—";
    private float  _dbgMoveX;
    private bool   _dbgGrounded, _dbgTraversing, _dbgJumpHeld;
    private bool   _dbgObstacle, _dbgTargetUp, _dbgWaypointUp;
    private float  _dbgTargetDeltaX, _dbgTargetDeltaY;
    private float  _dbgTargetMaxAbsX;
    private bool   _dbgTargetAware;
    private string _dbgLastJumpReason = "—";

    // Call from ShouldInitiateJump — records state each eval without spamming logs
    private void RecordDebug(float moveX, bool obstacle, bool targetUp, bool waypointUp)
    {
        _dbgMoveX      = moveX;
        _dbgGrounded   = controller.IsGrounded;
        _dbgTraversing = _isTraversing;
        _dbgJumpHeld   = _jumpHeld;
        _dbgObstacle   = obstacle;
        _dbgTargetUp   = targetUp;
        _dbgWaypointUp = waypointUp;
        _dbgLastJumpReason = "none";

        if (Sensor != null && Sensor.Target != null)
        {
            Vector2 source = unit.Rb.position;
            if (controller?._environmentCol != null)
                source = controller._environmentCol.bounds.center;

            Vector2 toTarget = (Vector2)Sensor.Target.position - source;
            _dbgTargetDeltaX = toTarget.x;
            _dbgTargetDeltaY = toTarget.y;
            _dbgTargetAware = Sensor.IsAwareOfTargetPosition;

            _dbgTargetMaxAbsX = _targetAboveMaxAbsX;
            if (_scaleAboveXByColliderWidth && controller?._environmentCol != null)
            {
                float colliderAllowance = controller._environmentCol.bounds.extents.x * 1.25f;
                _dbgTargetMaxAbsX = Mathf.Max(_dbgTargetMaxAbsX, colliderAllowance);
            }
        }
        else
        {
            _dbgTargetDeltaX = 0f;
            _dbgTargetDeltaY = 0f;
            _dbgTargetMaxAbsX = _targetAboveMaxAbsX;
            _dbgTargetAware = false;
        }

        // Unmask raycast to find what layer the obstacle actually is on
        if (!obstacle && _isTraversing && Mathf.Abs(moveX) > 0.01f)
        {
            float dir = Mathf.Sign(moveX);
            float halfW = controller._environmentCol.size.x / 2f;
            float halfH = controller._environmentCol.size.y / 2f;
            var   fwd   = new Vector2(dir, 0);
            var   orig  = unit.Rb.position + new Vector2(dir * halfW, -halfH + 0.2f);
            var   hit   = Physics2D.Raycast(orig, fwd, _obstacleDetectionDistance);
            _dbgObstacleLayer = hit.collider
                ? $"{LayerMask.LayerToName(hit.collider.gameObject.layer)} ({hit.collider.gameObject.name})"
                : "nothing";
        }
    }

    private void OnGUI()
    {
        if (!_showDebug || !Application.isPlaying) return;

        var cam = Camera.main;
        if (cam == null) return;
        var screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPos.z < 0) return;
        screenPos.y = Screen.height - screenPos.y;

        var style = new GUIStyle(GUI.skin.box) { fontSize = 10, alignment = TextAnchor.UpperLeft };
        string label =
            $"traversing={_dbgTraversing}  grounded={_dbgGrounded}  jumpHeld={_dbgJumpHeld}\n" +
            $"moveX={_dbgMoveX:F1}  obstacle={_dbgObstacle}  targetUp={_dbgTargetUp}  wpUp={_dbgWaypointUp}\n" +
            $"targetAware={_dbgTargetAware}  targetΔ=({_dbgTargetDeltaX:F1}, {_dbgTargetDeltaY:F1})  maxAbsX={_dbgTargetMaxAbsX:F1}\n" +
            $"lastJump={_dbgLastJumpReason}\n" +
            $"unmasked hit: {_dbgObstacleLayer}";
        GUI.Box(new Rect(screenPos.x - 10, screenPos.y, 360, 84), label, style);
    }
#endif
}
