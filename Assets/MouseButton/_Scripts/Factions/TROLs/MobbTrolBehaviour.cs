using System;
using System.Collections.Generic;
using Pathfinding;
using Tarodev.Trol;
using UnityEngine;

namespace TarodevController.Trol
{
    internal class MobbTrolBehaviour : MonoBehaviour
    {
        #region Fields

        [SerializeField]
        private ScriptableStats _stats;

        [SerializeField]
        private string sightlineStatusDebug;

        [SerializeField]
        private string behaviorStatusDebug;

        [SerializeField]
        private string movementStatusDebug;

        [SerializeField]
        private string pathingStatusDebug;

        [SerializeField]
        private float _withinReachDistance = 10f;

        [SerializeField]
        private MobbTrolController _controller;

        [SerializeField]
        private bool _showBehaviorDebugging = false;

        [SerializeField]
        internal float _perceivedDistanceToTarget;

        [SerializeField]
        private int _sightRange = 100;

        [SerializeField]
        private int _spearRange = 50;

        [SerializeField]
        private float _pathfinderJumpThreshold;

        [SerializeField]
        private float _obstacleDetectionDistance;

        [SerializeField]
        private float _localAvoidanceDistance;

        [SerializeField]
        private float _voidDetectionDistance;

        [SerializeField]
        private float randomJumpChance = 1f;

        // A* pathfinding
        [SerializeField]
        internal AIDestinationSetter _dest;

        [SerializeField]
        internal AIPath _ai; // unit brain

        [SerializeField]
        internal Seeker _seeker; // A*

        // Private Fields
        private FrameInput _aiFrameInput; // Frame-specific input data
        private int _groundLayerMask;
        private int _oneWayLayerMask;
        private int _obstacleLayerMask;
        private int _entityCollisionLayerMask;
        private Vector2 _directionToNextWaypoint;
        private float _distanceToNextWaypoint;
        private Path _path; // current path
        private Path CurrentPath
        {
            get => _path;
            set
            {
                if (value == null)
                {
                    _currentWaypoint = 0;
                }
                _path = value;
            }
        }
        private int _currentWaypoint = -1;

        // Public Fields
        [HideInInspector]
        public Rigidbody2D _rb; // Rigidbody2D component for physics interactions
        #endregion

        // Events
        #region Events

        public event Action ThrowSpear;
        public event Action<bool> TakingAim;
        public event Action<Transform> GrabSpear;
        public event Action BeginCelebrating;

        #endregion

        #region Unity Methods

        private void Start()
        {
            _controller.GatherAIInput += OnGatherAIInput;
            _controller.GetTrolSpeedAfterModifiers += OnGetTrolSpeedAfterModifiers;

            InvokeRepeating(nameof(UpdatePath), 0.25f, 0.25f);
            _rb = _controller._rb;

            _obstacleLayerMask = LayerMask.GetMask("Ground", "climbable");
            _groundLayerMask = LayerMask.GetMask("Ground", "one-way", "climbable");
            _oneWayLayerMask = LayerMask.GetMask("one-way");
            _entityCollisionLayerMask = LayerMask.GetMask("EntityCollisions");
        }

        #endregion

        #region AI & Behavior

        private FrameInput OnGatherAIInput()
        {
            _aiFrameInput = new FrameInput();

            bool hasSightline = AssessSituation();
            if (!_controller.StateLocked)
            {
                AssessPathing(hasSightline);
            }
            else
            {
                _aiFrameInput.Move.x = 0;
                movementStatusDebug = "path locked atm";
            }

            return _aiFrameInput;
        }

        #region Situation Assessment

        protected virtual bool AssessSituation()
        {
            if (_dest.target == null)
                return false;

            bool hasSightline = ConfirmSightline();

            if (_controller.StateLocked)
                return hasSightline;

            if (_controller.MustCelebrate && _controller.IsGrounded)
            {
                BeginCelebrating();
                return hasSightline;
            }
            else if (_controller.Spearless)
            {
                HandleSpearlessState();
            }
            else
            {
                HandleArmedState(hasSightline);
            }
            return hasSightline;
        }

        private void HandleSpearlessState()
        {
            if (_dest.target == null)
            {
                behaviorStatusDebug = "targeting new spear...";
                Transform s = GetClosestSpear()?.transform;
                if (s != null)
                {
                    UpdateTarget(s);
                }
                else
                {
                    ClearPath();
                    behaviorStatusDebug = "no spear :(";
                }
            }

            if (_perceivedDistanceToTarget < _withinReachDistance)
            {
                behaviorStatusDebug = "grabbing spear...";
                HandleGrabSpear();
            }
        }

        private void HandleGrabSpear()
        {
            GrabSpear.Invoke(_dest.target);
            UpdateTarget(PlayerObject.Instance.transform); // todo may not always seek player
        }

        private void HandleArmedState(bool hasSightline)
        {
            if (
                hasSightline
                && _perceivedDistanceToTarget < _spearRange
                && !_controller.MustCelebrate
            )
            {
                behaviorStatusDebug = "in sight & range...";
                if (_controller.ShouldThrow) // enough aim time has elapsed
                {
                    behaviorStatusDebug = "throwing...";
                    ThrowSpear.Invoke();
                    UpdateTarget(null);
                    return;
                }
                _controller.IsAiming = _controller.IsGrounded;
            }
            else
            {
                _controller.IsAiming = false;
            }
        }

        #endregion

        #region Pathing

        protected virtual void AssessPathing(bool hasSightline)
        {
            if (
                CurrentPath?.vectorPath == null
                || _currentWaypoint >= CurrentPath.vectorPath.Count
                || _controller.IsAiming
                || _perceivedDistanceToTarget < _ai.endReachedDistance
            )
            {
                _aiFrameInput.Move.x = 0;
                movementStatusDebug = "...";
                return;
            }

            if (
                _currentWaypoint < CurrentPath.vectorPath.Count - 1
                && _distanceToNextWaypoint < _ai.pickNextWaypointDist
            )
            {
                _currentWaypoint++;
                movementStatusDebug = "next waypoint...";
            }

            _directionToNextWaypoint = (
                (Vector2)CurrentPath.vectorPath?[_currentWaypoint] - _rb.position
            ).normalized;
            _distanceToNextWaypoint = Vector2.Distance(
                _rb.position,
                CurrentPath.vectorPath[_currentWaypoint]
            );
            _aiFrameInput.Move.x = _directionToNextWaypoint.x > 0 ? 1 : -1;
            movementStatusDebug = "omw...";
            AssessJumping();
        }

        private void UpdatePath()
        {
            if (!_seeker.IsDone())
                return;

            if (_controller.Spearless)
            {
                Transform closestSpearTransform = GetClosestSpear()?.transform;
                if (closestSpearTransform != _dest.target)
                    UpdateTarget(closestSpearTransform);
            }

            if (ConfirmSightline())
            {
                _seeker.StartPath(_rb.position, _dest.target.position, OnPathProcessed);
                pathingStatusDebug = "Attempting to update path...";
            }
            else
            {
                CurrentPath = null;
                pathingStatusDebug = "Can't update path...";
            }
        }

        private void UpdateTarget(Transform newTarget)
        {
            ClearPath();
            _dest.target = newTarget;
            if (newTarget == null)
            {
                behaviorStatusDebug = "Cleared target...";
                return;
            }

            behaviorStatusDebug = "Setting new target: " + newTarget;
            InvokeRepeating(nameof(UpdatePath), 0.25f, 0.25f);
        }

        private void ClearPath()
        {
            pathingStatusDebug = "Cleared path...";
            CurrentPath = null;
            _ai.SetPath(null);
        }

        private void OnPathProcessed(Path p)
        {
            if (p == null || p.error || p.vectorPath == null || !ConfirmSightline())
            {
                return;
            }
            CurrentPath = p;

            _currentWaypoint = 0;
            _directionToNextWaypoint = (
                (Vector2)CurrentPath.vectorPath[_currentWaypoint] - _rb.position
            ).normalized;
            _distanceToNextWaypoint = Vector2.Distance(
                _rb.position,
                CurrentPath.vectorPath[_currentWaypoint]
            );
            pathingStatusDebug = "Path processed!";
        }

        internal void HandleLocalAvoidance()
        {
            Vector2 rayOrigin =
                _rb.position
                + new Vector2(
                    _controller._environmentCol.size.x / 2 + _obstacleDetectionDistance,
                    0
                );
            float rayLength = _obstacleDetectionDistance * 2 + _controller._environmentCol.size.x;
            Debug.DrawRay(rayOrigin, Vector2.left * rayLength, Color.red);

            RaycastHit2D[] crowd = Physics2D.RaycastAll(
                rayOrigin,
                Vector2.left,
                rayLength,
                _entityCollisionLayerMask
            );

            foreach (RaycastHit2D hit in crowd)
            {
                // if (hit.collider.gameObject.CompareTag("TROL")) return;
                if (
                    // hit.collider.gameObject != gameObject &&
                    Vector3.Distance(hit.collider.transform.position, transform.position) < 5f
                )
                {
                    Vector3 avoidanceDirection = (
                        transform.position - hit.collider.transform.position
                    ).normalized;
                    transform.position += avoidanceDirection;
                }
            }
        }

        #endregion

        #region Sightline

        /// <summary>
        /// Confirms if there is a clear sightline to the target or a specified point.
        /// </summary>
        /// <param name="pointInSpace">Optional. A specific point to check the sightline to. If null, the target's position is used.</param>
        /// <returns>True if there is a clear sightline to the target or specified point; otherwise, false.</returns>
        internal bool ConfirmSightline(Vector2? pointInSpace = null)
        {
            if (pointInSpace == null && _dest.target == null)
                return false;

            // if we aren't seeking a point in space, seek target
            Vector2 targetPoint = pointInSpace ?? _dest.target.position;

            float realDistanceToTarget = Vector2.Distance(_rb.position, targetPoint);
            if (realDistanceToTarget > _sightRange)
            {
                if (pointInSpace == null)
                    _perceivedDistanceToTarget = float.PositiveInfinity;
                sightlineStatusDebug = "my target is too far away...";
                return false;
            }

            RaycastHit2D hit = Physics2D.Linecast(_rb.position, targetPoint, _obstacleLayerMask);

            if (hit.collider != null)
            {
                if (pointInSpace == null)
                    _perceivedDistanceToTarget = float.PositiveInfinity;
                sightlineStatusDebug = "i don't see my target...";
                return false;
            }

            if (pointInSpace == null)
                _perceivedDistanceToTarget = realDistanceToTarget;
            sightlineStatusDebug =
                pointInSpace == null ? "i see my target!" : "i see my next waypoint!";
            return true;
        }

        #endregion

        #region Speed Modifiers

        private float OnGetTrolSpeedAfterModifiers()
        {
            float modifiedMaxSpeed = _controller.Spearless
                ? _stats.MaxSpeed * 1.25f
                : _stats.MaxSpeed;
            if (_dest.target != null && ConfirmSightline())
            {
                modifiedMaxSpeed = Mathf.Clamp(
                    _stats.MaxSpeed
                        * (
                            Mathf.Abs(_dest.target.position.x - transform.position.x)
                            / _ai.slowdownDistance
                        ),
                    _stats.MinSpeed,
                    _stats.MaxSpeed
                );
            }

            return modifiedMaxSpeed;
        }

        #endregion

        #region Obstacle Detection

        private bool IsLedgeAhead()
        {
            Vector2 rayOrigin =
                _rb.position
                + new Vector2(
                    _aiFrameInput.Move.x > 0
                        ? _controller._environmentCol.size.x / 2
                        : -_controller._environmentCol.size.x / 2,
                    -_controller._environmentCol.size.y / 2
                );
            float rayLength = _stats.AfraidOfHeight;

            RaycastHit2D hit = Physics2D.Raycast(
                rayOrigin,
                Vector2.down,
                rayLength,
                _groundLayerMask
            );
            Debug.DrawRay(rayOrigin, Vector2.down * rayLength, Color.red);

            return hit.collider == null;
        }

        private bool IsObstacleAhead()
        {
            Vector2 rayOrigin =
                _rb.position
                + new Vector2(
                    _aiFrameInput.Move.x > 0
                        ? _controller._environmentCol.size.x / 2
                        : -_controller._environmentCol.size.x / 2,
                    0
                );
            float rayLength = _obstacleDetectionDistance;

            RaycastHit2D hit = Physics2D.Raycast(
                rayOrigin,
                new Vector2(_aiFrameInput.Move.x, 0),
                rayLength,
                _groundLayerMask
            );
            Debug.DrawRay(rayOrigin, new Vector2(_aiFrameInput.Move.x, 0) * rayLength, Color.blue);

            return hit.collider != null;
        }

        private bool IsVoidBelow()
        {
            Vector2 rayOrigin =
                _rb.position + new Vector2(0, _controller._environmentCol.size.x / 2);
            float rayLength = _voidDetectionDistance;

            RaycastHit2D hit = Physics2D.Raycast(
                rayOrigin,
                Vector2.down,
                rayLength,
                _groundLayerMask
            );
            Debug.DrawRay(rayOrigin, Vector2.down * rayLength, Color.blue);

            return hit.collider == null;
        }

        #endregion

        #region Jumping

        private const float DirectionThreshold = 0.75f;
        private const int RandomJumpChanceMax = 1000;

        private bool AssessJumpConditions()
        {
            bool withinRangeOfTarget = _perceivedDistanceToTarget < _pathfinderJumpThreshold;
            bool jumpStrategically = IsLedgeAhead() || IsObstacleAhead() || IsVoidBelow();
            bool jumpRandomly = ShouldJumpRandomly();
            bool fallInstead = withinRangeOfTarget && _directionToNextWaypoint.y < 0; // if nearby & below

            return (jumpStrategically || jumpRandomly) && !fallInstead;
        }

        private bool ShouldJumpRandomly()
        {
            return _directionToNextWaypoint.y > DirectionThreshold
                && UnityEngine.Random.Range(0, RandomJumpChanceMax) < randomJumpChance;
        }

        private void AssessJumping()
        {
            bool shouldJump = AssessJumpConditions();

            _aiFrameInput.JumpDown = shouldJump && !_aiFrameInput.JumpHeld;
            _aiFrameInput.JumpHeld = shouldJump;
        }

        #endregion

        #region Spear Handling

        private TrolSpear GetClosestSpear()
        {
            List<TrolSpear> spears = GameManager.instance.trolManager.activeSpears;
            TrolSpear bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;

            foreach (TrolSpear potentialTarget in spears)
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (
                    dSqrToTarget < closestDistanceSqr
                    && ConfirmSightline(potentialTarget.transform.position)
                )
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget;
                }
            }

            behaviorStatusDebug =
                "Closest spear found: " + (bestTarget != null ? bestTarget.ToString() : "none...");
            return bestTarget;
        }

        #endregion

        #endregion

        #region Debugging

        private void OnDrawGizmos()
        {
            if (CurrentPath?.vectorPath != null)
            {
                for (int i = 0; i < CurrentPath.vectorPath.Count - 1; i++)
                {
                    Gizmos.color =
                        i == _currentWaypoint - 1
                            ? new Color(0, 0, 1F, 1F)
                            : new Color(0, 1F, 0, 1F);
                    Gizmos.DrawLine(CurrentPath.vectorPath[i], CurrentPath.vectorPath[i + 1]);
                }
            }
        }

        #endregion
    }
}
