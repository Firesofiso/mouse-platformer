using System;
using UnityEngine;
using Pathfinding;
using System.Collections.Generic;
using Tarodev.Trol;

namespace TarodevController.Trol {
    internal class MobbTrolBehaviour : MonoBehaviour {
        #region Fields

        [SerializeField] private ScriptableStats _stats;
        [SerializeField] private string sightlineStatusDebug;
        [SerializeField] private string behaviorStatusDebug;
        [SerializeField] private string movementStatusDebug;
        [SerializeField] private string pathingStatusDebug;
        [SerializeField] private float _withinReachDistance = 10f;
        [SerializeField] private MobbTrolController _controller;
        [SerializeField] private bool _showBehaviorDebugging = false;
        [SerializeField] internal float _perceivedDistanceToTarget;
        [SerializeField] private int _sightRange = 100;
        [SerializeField] private int _spearRange = 50;
        [SerializeField] private float _pathfinderJumpThreshold;
        [SerializeField] private float _obstacleDetectionDistance;
        [SerializeField] private float randomJumpChance = 1f;
        // A* pathfinding
        [SerializeField] internal AIDestinationSetter _dest;

        [SerializeField] internal AIPath _ai; // unit brain
        [SerializeField] internal Seeker _seeker; // A*

        // Private Fields
        private FrameInput _aiFrameInput; // Frame-specific input data
        private int _groundLayerMask;
        private int _oneWayLayerMask;
        private int _obstacleLayerMask;
        private Vector2 _directionToNextWaypoint;
        private float _distanceToNextWaypoint;
        private Path _path; // current path
        private int _currentWaypoint = 0;

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

        private void Start() {
            _controller.GatherAIInput += OnGatherAIInput;
            _controller.GetTrolSpeedAfterModifiers += OnGetTrolSpeedAfterModifiers;

            InvokeRepeating(nameof(UpdatePath), 0.25f, 0.25f);
            _rb = _controller._rb;

            _obstacleLayerMask = LayerMask.GetMask("Ground", "climbable");
            _groundLayerMask = LayerMask.GetMask("Ground", "one-way", "climbable");
            _oneWayLayerMask = LayerMask.GetMask("one-way");
        }

        #endregion

        #region AI & Behavior

        private FrameInput OnGatherAIInput() {
            _aiFrameInput = new FrameInput();

            bool hasSightline = AssessSituation();
            if (!_controller.StateLocked) {
                AssessPathing(hasSightline);
            } else {
                _aiFrameInput.Move.x = 0;
                movementStatusDebug = "path locked atm";
            }

            return _aiFrameInput;
        }

        /// <summary>
        /// Confirms if there is a clear sightline to the target or a specified point.
        /// </summary>
        /// <param name="pointInSpace">Optional. A specific point to check the sightline to. If null, the target's position is used.</param>
        /// <returns>True if there is a clear sightline to the target or specified point; otherwise, false.</returns>
        internal bool ConfirmSightline(Vector2? pointInSpace = null) {
            if (pointInSpace == null && _dest.target == null)
                return false;

            // if we aren't seeking a point in space, seek target
            Vector2 targetPoint = pointInSpace ?? _dest.target.position;

            float realDistanceToTarget = Vector2.Distance(_rb.position, targetPoint);
            if (realDistanceToTarget > _sightRange) {
                if (pointInSpace == null) _perceivedDistanceToTarget = float.PositiveInfinity;
                sightlineStatusDebug = "my target is too far away...";
                return false;
            }

            RaycastHit2D hit = Physics2D.Linecast(_rb.position, targetPoint, _obstacleLayerMask);

            if (hit.collider != null) {
                if (pointInSpace == null) _perceivedDistanceToTarget = float.PositiveInfinity;
                sightlineStatusDebug = "i don't see my target...";
                return false;
            }

            if (pointInSpace == null) _perceivedDistanceToTarget = realDistanceToTarget;
            sightlineStatusDebug =
                pointInSpace == null ? "i see my target!" : "i see my next waypoint!";
            return true;
        }

        protected virtual bool AssessSituation() {
            if (_dest.target == null) return false;

            bool hasSightline = ConfirmSightline();

            if (_controller.StateLocked)
                return hasSightline;

            if (_controller.MustCelebrate && _controller.Grounded) {
                BeginCelebrating();
                return hasSightline;
            } else if (_controller.Spearless) {
                HandleSpearlessState();
            } else {
                HandleArmedState(hasSightline);
            }
            return hasSightline;
        }

        private void HandleSpearlessState() {
            if (_dest.target == null) {
                behaviorStatusDebug = "targeting new spear...";
                Transform s = GetClosestSpear()?.transform;
                if (s != null) {
                    UpdateTarget(s);
                } else {
                    ClearPath();
                    behaviorStatusDebug = "no spear :(";
                }
            }

            if (_perceivedDistanceToTarget < _withinReachDistance) {
                behaviorStatusDebug = "grabbing spear...";
                HandleGrabSpear();
            }
        }

        private void HandleGrabSpear() {
            GrabSpear.Invoke(_dest.target);
            UpdateTarget(PlayerObject.Instance.transform); // todo may not always seek player
        }

        private void HandleArmedState(bool hasSightline) {
            if (hasSightline && _perceivedDistanceToTarget < _spearRange) {
                behaviorStatusDebug = "in sight & range...";
                if (_controller.ShouldThrow) {
                    behaviorStatusDebug = "throwing...";
                    ThrowSpear.Invoke();
                    UpdateTarget(null);
                } else if (_controller.Grounded && !_controller.MustCelebrate) {
                    behaviorStatusDebug = "aiming...";
                    _controller.IsAiming = true;
                }
            } else {
                behaviorStatusDebug = "outta sight and/or range...";
                _controller.IsAiming = false;
            }
        }

        protected virtual void AssessPathing(bool hasSightline) {
            if (_path?.vectorPath == null || _controller.IsAiming || _perceivedDistanceToTarget < _ai.endReachedDistance) {
                _aiFrameInput.Move.x = 0;
                movementStatusDebug = "...";
                return;
            }

            if (_currentWaypoint < _path.vectorPath.Count - 1 && _distanceToNextWaypoint < _ai.pickNextWaypointDist) {
                _currentWaypoint++;
                movementStatusDebug = "next waypoint...";
            }

            _directionToNextWaypoint = ((Vector2)_path.vectorPath[_currentWaypoint] - _rb.position).normalized;
            _distanceToNextWaypoint = Vector2.Distance(_rb.position, _path.vectorPath[_currentWaypoint]);
            _aiFrameInput.Move.x = _directionToNextWaypoint.x > 0 ? 1 : -1;
            movementStatusDebug = "omw...";
            AssessJumping();
        }

        private float OnGetTrolSpeedAfterModifiers() {
            float modifiedMaxSpeed = _controller.Spearless ? _stats.MaxSpeed * 1.25f : _stats.MaxSpeed;
            if (_dest.target != null && ConfirmSightline()) {
                modifiedMaxSpeed = Mathf.Clamp(
                    _stats.MaxSpeed * (Mathf.Abs(_dest.target.position.x - transform.position.x) / _ai.slowdownDistance),
                    _stats.MinSpeed,
                    _stats.MaxSpeed
                );
            }

            return modifiedMaxSpeed;
        }

        private bool IsLedgeAhead() {
            Vector2 rayOrigin = _rb.position + new Vector2(_aiFrameInput.Move.x > 0 ? _controller._environmentCol.size.x / 2 : -_controller._environmentCol.size.x / 2, -_controller._environmentCol.size.y / 2);
            float rayLength = _stats.AfraidOfHeight;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, _groundLayerMask);
            Debug.DrawRay(rayOrigin, Vector2.down * rayLength, Color.red);

            return hit.collider == null;
        }

        private bool IsObstacleAhead() {
            Vector2 rayOrigin = _rb.position + new Vector2(_aiFrameInput.Move.x > 0 ? _controller._environmentCol.size.x / 2 : -_controller._environmentCol.size.x / 2, 0);
            float rayLength = _obstacleDetectionDistance;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, new Vector2(_aiFrameInput.Move.x, 0), rayLength, _groundLayerMask);
            Debug.DrawRay(rayOrigin, new Vector2(_aiFrameInput.Move.x, 0) * rayLength, Color.blue);

            return hit.collider != null;
        }

        private void AssessJumping() {
            bool withinRangeOfTarget = _perceivedDistanceToTarget < _pathfinderJumpThreshold;
            if ((withinRangeOfTarget && _directionToNextWaypoint.y < 0) || _controller.IsAiming) {
                _aiFrameInput.JumpDown = false;
                _aiFrameInput.JumpHeld = false;
            } else if (!_controller.IsAiming && ((_directionToNextWaypoint.y > .75 && UnityEngine.Random.Range(0, 1000) < randomJumpChance) || IsLedgeAhead() || IsObstacleAhead())) {
                _aiFrameInput.JumpDown = !_aiFrameInput.JumpHeld;
                _aiFrameInput.JumpHeld = true;
            }
        }
        #endregion

        #region Pathfinding

        private void UpdatePath() {
            if (!_seeker.IsDone())
                return;

            if (_controller.Spearless) {
                Transform closestSpearTransform = GetClosestSpear()?.transform;
                if (closestSpearTransform != _dest.target)
                    UpdateTarget(closestSpearTransform);
            }

            if (ConfirmSightline()) {
                _seeker.StartPath(_rb.position, _dest.target.position, OnPathProcessed);
                pathingStatusDebug = "Attempting to update path...";
            } else {
                pathingStatusDebug = "Can't update path...";
            }
        }

        private void UpdateTarget(Transform newTarget) {
            ClearPath();
            _dest.target = newTarget;
            if (newTarget == null) {
                behaviorStatusDebug = "Cleared target...";
                return;
            }

            behaviorStatusDebug = "Setting new target: " + newTarget;
            InvokeRepeating(nameof(UpdatePath), 0.25f, 0.25f);
        }

        private void ClearPath() {
            pathingStatusDebug = "Cleared path...";
            _path = null;
            _ai.SetPath(null);
        }

        private void OnPathProcessed(Path p) {
            if (p == null || p.error || p.vectorPath == null || !ConfirmSightline()) {
                return;
            }
            _path = p;

            _currentWaypoint = 0;
            _directionToNextWaypoint = ((Vector2)_path.vectorPath[_currentWaypoint] - _rb.position).normalized;
            _distanceToNextWaypoint = Vector2.Distance(_rb.position, _path.vectorPath[_currentWaypoint]);
            pathingStatusDebug = "Path processed!";
        }

        #endregion

        #region Spear Handling

        private TrolSpear GetClosestSpear() {
            List<TrolSpear> spears = GameManager.instance.trolManager.activeSpears;
            TrolSpear bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;

            foreach (TrolSpear potentialTarget in spears) {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr && ConfirmSightline(potentialTarget.transform.position)) {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget;
                }
            }

            behaviorStatusDebug = "Closest spear found: " + (bestTarget != null ? bestTarget.ToString() : "none...");
            return bestTarget;
        }

        #endregion

        #region Helper Methods

        #endregion

        #region Debugging

        private void OnDrawGizmos() {
            if (_path?.vectorPath != null) {
                for (int i = 0; i < _path.vectorPath.Count - 1; i++) {
                    Gizmos.color = i == _currentWaypoint - 1 ? new Color(0, 0, 1F, 1F) : new Color(0, 1F, 0, 1F);
                    Gizmos.DrawLine(_path.vectorPath[i], _path.vectorPath[i + 1]);
                }
            }
        }

        #endregion
    }
}