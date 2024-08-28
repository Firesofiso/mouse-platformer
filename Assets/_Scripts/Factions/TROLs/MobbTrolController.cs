// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pathfinding;

namespace TarodevController {
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MobbTrolController : MonoBehaviour, IPlayerController, IFactionController, TrolUnit {
        [SerializeField] private ScriptableStats _stats;

        #region Internal
        private PlayerObject Player;

        [HideInInspector] public Rigidbody2D _rb; // Hide is for serialization to avoid errors in gizmo calls
        [SerializeField] private CapsuleCollider2D _standingEnvironmentCollider;
        [SerializeField] private CapsuleCollider2D _crouchingEnvironmentCollider;
        [SerializeField] private CapsuleCollider2D _standingEntityCollider;
        [SerializeField] private CapsuleCollider2D _crouchingEntityCollider; 
        [SerializeField] private PolygonCollider2D _spearTipCollider; 
        private CapsuleCollider2D _environmentCol; // current active collider for environmental (ground, wall, platform) collisions
        private CapsuleCollider2D _entityCol; // current active collider for entity (player, enemy) collisions
        
        private PlayerInput _input;
        private bool _cachedTriggerSetting;
        private FrameInput _frameInput;
        public Vector2 _speed;
        private Vector2 _currentExternalVelocity;
        private int _fixedFrame;
        private bool _hasControl = true;

        #endregion

        #region External

        public event Action<bool, float> GroundedChanged;
        public event Action<bool, Vector2> DashingChanged;
        public event Action<bool> WallGrabChanged;
        public event Action<bool> LedgeClimbChanged;
        public event Action<bool> Jumped;
        public event Action AirJumped;
        public event Action<bool> HandleAiming;
        public event Action<bool> HandleThrowing;
        public event Action HandleRecovery;
        public event Action Attacked;
        public event Action Clicked;
        public ScriptableStats PlayerStats => _stats;
        public Vector2 Input => _frameInput.Move;
        public Vector2 Velocity => _rb.velocity;
        public Vector2 Speed => _speed; // + _currentExternalVelocity; // we should add this, right?
        public Vector2 GroundNormal { get; private set; }
        public int WallDirection { get; private set; }
        public bool Crouching { get; private set; }
        public bool ClimbingLadder { get; private set; }
        public bool GrabbingLedge { get; private set; }
        public bool ClimbingLedge { get; private set; }

        public virtual void ApplyVelocity(Vector2 vel, PlayerForce forceType) {
            if (forceType == PlayerForce.Burst) _speed += vel;
            else _currentExternalVelocity += vel;
        }

        public virtual void SetVelocity(Vector2 vel, PlayerForce velocityType) {
            if (velocityType == PlayerForce.Burst) _speed = vel;
            else _currentExternalVelocity = vel;
        }

        public virtual void TakeAwayControl(bool resetVelocity = true) {
            if (resetVelocity) _rb.velocity = Vector2.zero;
            _hasControl = false;
        }

        public virtual void ReturnControl() {
            _speed = Vector2.zero;
            _hasControl = true;
        }

        #endregion

        #region Pathfinding
        // 2D PATHFINDING - Enemy AI in Unity by Brackeys
        // https://www.youtube.com/watch?v=jvtFUfJ6CP8
        public AIPath ai; // unit brain
        public AIDestinationSetter dest;
        private Path path; // current path
        [SerializeField] int currentWaypoint = 0;
        private Seeker seeker; // A*
        private Vector2 directionToTarget; // direction for sightline raycast

        // updates path periodically
        void UpdatePath() {
            if (seeker.IsDone()) {
                if (dest?.target != null && ConfirmSightline()) { // seeker has processed most recent path & target is still in sight
                    ai.canSearch = true;
                    seeker.StartPath(_rb.position, dest.target.position, OnPathProcessed); // update path
                } else {
                    ai.canSearch = false;
                    path = seeker.GetCurrentPath(); // continue path
                }
            }
        }

        void UpdateTarget(Transform newTarget) {
            if (showBehaviorDebugging) Debug.Log("Setting new target: " + newTarget);
            StopPath();

            dest.target = newTarget;
            if (newTarget == null) return;
            
            ai.SearchPath();
            InvokeRepeating("UpdatePath", 0, 0.25f);
        }

        void StopPath() {
            CancelInvoke();
            ai.SetPath(null);
            ai.canSearch = false;
            dest.target = null;
        }

        // callback for seeker.StartPath()
        void OnPathProcessed(Path p) {
            if (p != null && !p.error && ConfirmSightline()) {
                path = p;
                currentWaypoint = 0;
            } else {
                ai.canSearch = false;
                path = null;
            }
        }

        #endregion

        protected virtual void Start() {
            GameManager.instance.trolManager.activeTrols.Add(this);
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<PlayerInput>();
            _cachedTriggerSetting = Physics2D.queriesHitTriggers;
            Physics2D.queriesStartInColliders = false;

            ToggleColliders(isStanding: true);
            //Physics.IgnoreLayerCollision(LayerMask.GetMask("player"), LayerMask.GetMask("cursor"), true);

            // init pathfinding components
            ai.canSearch = false;
            if (!_input.isInputUnit) Player = PlayerObject.Instance;
            seeker = GetComponent<Seeker>();
            InvokeRepeating("UpdatePath", 0, 0.25f);
        }

        #region AI & Behavior

        private float perceivedDistanceToTarget; // this is updated when confirming sightline to target, be careful relying on this unless sightline has been confirmed
        [SerializeField] float sightRange = 100f; 
        [SerializeField] float spearRange = 50f;

        /* ConfirmSightline */
        // takes a Vector2 representing what the character is seeking, by default the target
        // when a Vector2 parameter is provided, it is treated as a static point in space i.e. last seen waypoint
        public bool ConfirmSightline(Vector2? seekingPoint = null) {
            if (seekingPoint == null && dest.target == null) return false;
            bool isSeekingTarget = seekingPoint == null; // as opposed to seeking a point in space where target was last seen
            if (isSeekingTarget) seekingPoint = dest.target.position; // if no point to seek, grab target coords
            float realDistanceToTarget = Vector2.Distance(_rb.position, (Vector2)seekingPoint); // calculate real distance, used in determining perceived distance

            if (realDistanceToTarget > sightRange) return false; 

            if (isSeekingTarget) {
                Vector2 directionToTarget = ((Vector2)seekingPoint - _rb.position).normalized; // for raycasting purposes
                RaycastHit2D[] hits = Physics2D.RaycastAll(_rb.position, directionToTarget, sightRange, LayerMask.GetMask("player", "Entities", "Ground", "projectiles", "climbable"));

                for (int i = 0; i < hits.Length; i++) {
                    RaycastHit2D hit = hits[i];

                    if (hit.transform == dest.target) {
                        // evaluate target
                        perceivedDistanceToTarget = realDistanceToTarget;
                        return true;
                        
                    } else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") || hit.collider.gameObject.layer == LayerMask.NameToLayer("climbable") ){// || hit.collider.gameObject.tag == "TROL") {
                        // hit something that is closer than our target (obstructed by something solid)
                        perceivedDistanceToTarget = float.PositiveInfinity;
                        return false; // hit wall

                    } else {
                        // return true; // TODO seeking recent path, not target - hit.distance < perceivedDistanceToTarget;
                    }
                }
            } else {
                // isSeekingPoint
                RaycastHit2D hit = Physics2D.Raycast(_rb.position, directionToTarget, realDistanceToTarget, LayerMask.GetMask("Ground", "climbable"));
                if (hit && hit.distance < realDistanceToTarget) {
                    perceivedDistanceToTarget = float.PositiveInfinity;
                    return false; // hit something before reaching the point (obstructed by something solid)
                } else {
                    perceivedDistanceToTarget = realDistanceToTarget;
                    return true;
                }

            }
            perceivedDistanceToTarget = float.PositiveInfinity;
            return false;
        }

        protected virtual void Update() {
            GatherInput();
        }

        protected virtual void GatherInput() {
            if (_input.isInputUnit) { // player is controlling this unit
                _frameInput = _input.FrameInput;

                if (_stats.SnapInput) {
                    _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadzoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                    _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadzoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
                }

                if (_frameInput.DropDown && _environmentCol.IsTouchingLayers(LayerMask.GetMask("one-way"))) {
                    _droppingDown = true;
                } else if (!_droppingDown && _frameInput.JumpDown) {
                    _jumpToConsume = true;
                    _frameJumpWasPressed = _fixedFrame;
                }

                if (_frameInput.Move.x != 0) _stickyFeet = false;

                if (_frameInput.DashDown && _stats.AllowDash) _dashToConsume = true;
                if (_frameInput.AttackDown && _stats.AllowAttacks) _attackToConsume = true;
                if (_frameInput.ClickDown && _stats.AllowClicks) _isHoldingClick = true;
            } else { // emulate input for unit behavior
                AssessSituation();
                MakeMoves();
            }
        }

        protected virtual void TakingAim(bool isAiming) {
            // set or reset aiming window
            if (!isAiming) _aimingTill = 0;
            else if (isAiming && _aimingTill == 0) _aimingTill = Time.time + 2;

            HandleAiming?.Invoke(isAiming);
        }

        private float _recoveringTill;
        GameObject spear;
        [SerializeField] GameObject spearPrefab;
        bool _spearless = false;
        protected virtual void ThrowSpear() {

            var tripped = DidTrip(); // determines if extra recovery time is needed
            _recoveringTill = tripped ? Time.time + 3 : Time.time; // penalize recovery window

            HandleThrowing?.Invoke(tripped); // animate

            // generates spear prefab to huck
            spear = Instantiate(spearPrefab, transform);
            GameManager.instance.trolManager.activeSpears.Add(spear.GetComponent<TrolSpear>());
            StartCoroutine(spear.GetComponent<TrolSpear>().TemporarilyIgnoreColliders(new Collider2D[] {_environmentCol, _entityCol, _spearTipCollider})); // disables interactions with parent colliders, then re-enables after 0.25s

            spear.transform.up = dest.target.position - transform.position; // point at target
            spear.GetComponent<Rigidbody2D>().velocity = dest.target.position - transform.position * 2; // huck
            _spearless = true;

            StopPath();
        }

        Transform GetClosestSpear () {
            List<TrolSpear> spears = GameManager.instance.trolManager.activeSpears;
            Transform bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = transform.position;
            foreach(TrolSpear potentialTarget in spears)
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if(dSqrToTarget < closestDistanceSqr && ConfirmSightline(potentialTarget.transform.position))
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget.transform;
                }
            }
        
            // if (showBehaviorDebugging) Debug.Log("Closest spear found: " + (bestTarget != null ? bestTarget : "none..."));
            return bestTarget != null ? bestTarget.transform : null;
        }

        void GrabSpear(Transform s) {
            // Debug.Log("yoink!");
        }

        protected virtual bool DidTrip() {
            return UnityEngine.Random.Range(0,4) == 0;
        }

        protected virtual void Recovered() {
            HandleRecovery?.Invoke();
            _recoveringTill = 0;
            _aimingTill = 0;
        }

        Vector2 directionToNextWaypoint;
        [SerializeField] float distanceToNextWaypoint;
        private float _aimingTill;
        [SerializeField] float withinReachDistance = 10f;
        [SerializeField] bool showBehaviorDebugging = false;

        protected virtual void AssessSituation() {

            if (_recoveringTill > 0 && _recoveringTill < Time.time) { // recovering from throw
                if (showBehaviorDebugging) Debug.Log("recovered...");
                Recovered();
            }

            if (_spearless) {
                // get spear NOW
                Transform s = GetClosestSpear();
                if (dest.target != s) {
                    if (showBehaviorDebugging) Debug.Log("targeting new spear...");
                    UpdateTarget(s);
                }

                if (perceivedDistanceToTarget < withinReachDistance) {
                    if (showBehaviorDebugging) Debug.Log("grabbing spear...");
                    GrabSpear(dest.target);
                }

            } else if (!_spearless && ConfirmSightline() && perceivedDistanceToTarget < spearRange) {
                // in sight && in range
                if (showBehaviorDebugging) Debug.Log("in sight & range...");
                if (_aimingTill > 0 && _aimingTill < Time.time) {
                    if (showBehaviorDebugging) Debug.Log("throwing...");
                    ThrowSpear(); // if aiming, throw after 2 seconds of aiming
                }
                else {
                    if (showBehaviorDebugging) Debug.Log("aiming...");
                    TakingAim(true); // begin aiming
                }

            } else {
                if (showBehaviorDebugging) Debug.Log("outta sight & range...");
                // outta sight || outta range
                if (!_spearless) {
                    if (showBehaviorDebugging) Debug.Log("stop aiming...");
                    TakingAim(false);
                }
            }

            if (path == null || path?.vectorPath == null) return; // no current path

            if (currentWaypoint < path.vectorPath.Count - 1) {
                // follow most recent path in pursuit of target
                directionToNextWaypoint = ((Vector2)path.vectorPath[currentWaypoint] - _rb.position).normalized;
                distanceToNextWaypoint = Vector2.Distance(_rb.position, path.vectorPath[currentWaypoint]);
            }
        }

        protected virtual void MakeMoves() {
            // if (path != null && path.vectorPath != null && currentWaypoint < path.vectorPath.Count - 1) {
            //     directionToNextWaypoint = ((Vector2)path.vectorPath[currentWaypoint] - _rb.position).normalized;
            //     distanceToNextWaypoint = Vector2.Distance(_rb.position, path.vectorPath[currentWaypoint]);
            // }

            if (_aimingTill > 0 || _recoveringTill > 0) {
                if (showBehaviorDebugging) Debug.Log("preoccupied...");
                _frameInput.Move.x = 0;
                return;
            } else if (currentWaypoint < path?.vectorPath?.Count - 1) {
                if (!ConfirmSightline() && !ConfirmSightline(path.vectorPath[currentWaypoint])) {
                    // can't path further
                    // todo question marks ?????
                    if (showBehaviorDebugging) Debug.Log("lacking direction...");
                    _frameInput.Move.x = 0;
                    return;
                }
                // continue pathing
                if (path != null && dest.target != null) {
                    if (directionToNextWaypoint.x > 0) _frameInput.Move.x = 1; // whatever we're after, it's to the right
                    else if (directionToNextWaypoint.x < 0) _frameInput.Move.x = -1; // or the left
                    if (distanceToNextWaypoint < ai.pickNextWaypointDist) {
                        currentWaypoint++; // reached waypoint
                        if (showBehaviorDebugging) Debug.Log("next waypoint...");
                    }
                    return;
                }
                if (showBehaviorDebugging) Debug.Log("out of options....");
                _frameInput.Move.x = 0; // stop twitching if next waypoint is very close
            }
        }

        #endregion

        protected virtual void FixedUpdate() {
            _fixedFrame++;

            CheckCollisions();
            HandleCollisions();
            HandleWalls();
            HandleLedges();
            HandleLadders();

            HandleCrouching();
            HandleJump();
            HandleDash();
            HandleAttacking();
            HandleClicking();

            HandleHorizontal();
            HandleVertical();
            ApplyMovement();
        }

        #region Collisions

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[2];
        private readonly RaycastHit2D[] _ceilingHits = new RaycastHit2D[2];
        private readonly Collider2D[] _wallHits = new Collider2D[5];
        private readonly Collider2D[] _ladderHits = new Collider2D[1];
        private RaycastHit2D _hittingWall;
        private int _groundHitCount;
        private int _ceilingHitCount;
        private int _wallHitCount;
        private int _ladderHitCount;
        private int _frameLeftGrounded = int.MinValue;
        private bool _grounded;
        private Vector2 _skinWidth = new(0.02f, 0.02f); // Expose this?

        protected virtual void CheckCollisions() {
            Physics2D.queriesHitTriggers = false;

            // Ground and Ceiling
            _groundHitCount = Physics2D.CapsuleCastNonAlloc(_environmentCol.bounds.center, _environmentCol.size, _environmentCol.direction, 0, Vector2.down, _groundHits, _stats.GrounderDistance, ~_stats.PlayerLayer);
            _ceilingHitCount = Physics2D.CapsuleCastNonAlloc(_environmentCol.bounds.center, _environmentCol.size, _environmentCol.direction, 0, Vector2.up, _ceilingHits, _stats.GrounderDistance, ~_stats.PlayerLayer);

            // Walls and Ladders
            var bounds = GetWallDetectionBounds(); // won't be able to detect a wall if we're crouching mid-air
            _wallHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _wallHits, _stats.ClimbableLayer);

            _hittingWall = Physics2D.CapsuleCast(_environmentCol.bounds.center, _environmentCol.size, _environmentCol.direction, 0, new Vector2(_input.FrameInput.Move.x, 0), _stats.GrounderDistance, ~_stats.PlayerLayer);

            Physics2D.queriesHitTriggers = true; // Ladders are set to Trigger
            _ladderHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _ladderHits, _stats.LadderLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
        }

        protected virtual bool TryGetGroundNormal(out Vector2 groundNormal) {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.Raycast(_rb.position, Vector2.down, _stats.GrounderDistance * 2, ~_stats.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            groundNormal = hit.normal; // defaults to Vector2.zero if nothing was hit
            return hit.collider;
        }

        private Bounds GetWallDetectionBounds() {
            var colliderOrigin = _rb.position + _standingEnvironmentCollider.offset;
            return new Bounds(colliderOrigin, _stats.WallDetectorSize);
        }

        protected virtual void HandleCollisions() {
            // Hit a Ceiling
            if (_ceilingHitCount > 0) {
                // prevent sticking to ceiling if we did an InAir jump after receiving external velocity w/ PlayerForce.Decay
                _currentExternalVelocity.y = Mathf.Min(0f, _currentExternalVelocity.y);
                _speed.y = Mathf.Min(0, _speed.y);
            }

            // Landed on the Ground
            if (!_grounded && _groundHitCount > 0) {
                _grounded = true;
                ResetDash();
                ResetJump();
                GroundedChanged?.Invoke(true, Mathf.Abs(_speed.y));
                if (_frameInput.Move.x == 0) _stickyFeet = true;
            }
            // Left the Ground
            else if (_grounded && _groundHitCount == 0) {
                _grounded = false;
                _frameLeftGrounded = _fixedFrame;
                GroundedChanged?.Invoke(false, 0);
            }
        }

        private bool IsStandingPosClear(Vector2 pos) => CheckPos(pos, _standingEnvironmentCollider);
        private bool IsCrouchingPosClear(Vector2 pos) => CheckPos(pos, _crouchingEnvironmentCollider);

        protected virtual bool CheckPos(Vector2 pos, CapsuleCollider2D col) {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.OverlapCapsule(pos + col.offset, col.size - _skinWidth, col.direction, 0, ~_stats.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            return !hit;
        }

        #endregion

        #region Walls

        private readonly ContactPoint2D[] _wallContact = new ContactPoint2D[1];
        private float _currentWallJumpMoveMultiplier = 1f; // aka "Horizontal input influence"
        private int _lastWallDirection; // for coyote wall jumps
        private int _frameLeftWall; // for coyote wall jumps
        private bool _isLeavingWall; // prevents immediate re-sticking to wall
        public bool IsOnWall { get; private set; }

        protected virtual void HandleWalls() {
            if (!_stats.AllowWalls) return;

            _currentWallJumpMoveMultiplier = Mathf.MoveTowards(_currentWallJumpMoveMultiplier, 1f, 1f / _stats.WallJumpInputLossFrames);

            // May need to prioritize the nearest wall here... But who is going to make a climbable wall that tight?
            if (_wallHits[0]) _wallHits[0].GetContacts(_wallContact);
            WallDirection = _wallHitCount > 0 ? (int)Mathf.Sign(_wallContact[0].point.x - transform.position.x) : 0;
            if (WallDirection != 0) _lastWallDirection = WallDirection;

            if (!IsOnWall && ShouldStickToWall()) ToggleOnWall(true);// && _speed.y <= 0
            else if (IsOnWall && !ShouldStickToWall()) ToggleOnWall(false);

            bool ShouldStickToWall() {
                if (WallDirection == 0 || _grounded) return false;
                return !_stats.RequireInputPush || (HorizontalInputPressed && Mathf.Sign(_frameInput.Move.x) == WallDirection);
            }
        }

        private void ToggleOnWall(bool on) {
            IsOnWall = on;
            if (on) {
                _speed = Vector2.zero;
                _currentExternalVelocity = Vector2.zero;
                _bufferedJumpUsable = true;
                _wallJumpCoyoteUsable = true;
            }
            else {
                _frameLeftWall = _fixedFrame;
                _isLeavingWall = false; // after we've left the wall
                ResetAirJumps(); // so that we can air jump even if we didn't leave via a wall jump
                ResetWallShimmy();
            }

            WallGrabChanged?.Invoke(on);
        }

        #endregion

        #region Ledges

        private Vector2 _ledgeCornerPos;
        private bool _climbIntoCrawl;

        private bool LedgeClimbInputDetected => Input.y > _stats.VerticalDeadzoneThreshold || Input.x == WallDirection;

        protected virtual void HandleLedges() {
            if (!_stats.AllowLedges) return;
            if (ClimbingLedge || !IsOnWall) return;

            GrabbingLedge = TryGetLedgeCorner(out _ledgeCornerPos);

            if (GrabbingLedge) HandleLedgeGrabbing();
        }

        protected virtual bool TryGetLedgeCorner(out Vector2 cornerPos) {
            cornerPos = Vector2.zero;
            var grabHeight = _rb.position + _stats.LedgeGrabPoint.y * Vector2.up;

            var hit1 = Physics2D.Raycast(grabHeight + _stats.LedgeRaycastSpacing * Vector2.down, WallDirection * Vector2.right, 0.5f, _stats.ClimbableLayer);
            if (!hit1.collider) return false; // Should hit below the ledge. Mainly used to determine xPos accurately

            var hit2 = Physics2D.Raycast(grabHeight + _stats.LedgeRaycastSpacing * Vector2.up, WallDirection * Vector2.right, 0.5f, _stats.ClimbableLayer);
            if (hit2.collider)
                return false; // we only are within ledge-grab range when the first hits and second doesn't

            var hit3 = Physics2D.Raycast(grabHeight + new Vector2(WallDirection * 0.5f, _stats.LedgeRaycastSpacing), Vector2.down, 0.5f, _stats.ClimbableLayer);
            if (!hit3.collider) return false; // gets our yPos of the corner

            cornerPos = new(hit1.point.x, hit3.point.y);
            return true;
        }

        protected virtual void HandleLedgeGrabbing() {
            // Nudge towards better grabbing position
            if (Input.x == 0 && _hasControl) {
                var pos = _rb.position;
                var targetPos = _ledgeCornerPos - Vector2.Scale(_stats.LedgeGrabPoint, new(WallDirection, 1f));
                _rb.position = Vector2.MoveTowards(pos, targetPos, _stats.LedgeGrabDeceleration * Time.fixedDeltaTime);
            }

            if (LedgeClimbInputDetected) {
                var finalPos = _ledgeCornerPos + Vector2.Scale(_stats.StandUpOffset, new(WallDirection, 1f));
                
                if (IsStandingPosClear(finalPos)) {
                    _climbIntoCrawl = false;
                    StartLedgeClimb();
                }
                else if (_stats.AllowCrouching && IsCrouchingPosClear(finalPos)) {
                    _climbIntoCrawl = true;
                    StartLedgeClimb(intoCrawl: true);
                }
            }
        }

        protected virtual void StartLedgeClimb(bool intoCrawl = false) {
            LedgeClimbChanged?.Invoke(intoCrawl);
            TakeAwayControl();
            ClimbingLedge = true;
            GrabbingLedge = false;
            _rb.position = _ledgeCornerPos - Vector2.Scale(_stats.LedgeGrabPoint, new(WallDirection, 1f));
        }

        public virtual void TeleportMidLedgeClimb() {
            transform.position = _rb.position = _ledgeCornerPos + Vector2.Scale(_stats.StandUpOffset, new(WallDirection, 1f));
            if (_climbIntoCrawl) TryToggleCrouching(shouldCrouch: true);
            ToggleOnWall(false);
        }

        public virtual void FinishClimbingLedge() {
            ClimbingLedge = false;
            ReturnControl();
        }

        #endregion

        #region Ladders

        private Vector2 _ladderSnapVel;
        private int _frameLeftLadder;

        private bool CanEnterLadder => _ladderHitCount > 0 && _fixedFrame > _frameLeftLadder + _stats.LadderCooldownFrames;
        private bool ShouldMountLadder => _stats.AutoAttachToLadders || _frameInput.Move.y > _stats.VerticalDeadzoneThreshold || (!_grounded && _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold);
        private bool ShouldDismountLadder => !_stats.AutoAttachToLadders && _grounded && _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold;
        private bool ShouldCenterOnLadder => _stats.SnapToLadders && _frameInput.Move.x == 0 && _hasControl;

        protected virtual void HandleLadders() {
            if (!_stats.AllowLadders) return;

            if (!ClimbingLadder && CanEnterLadder && ShouldMountLadder) ToggleClimbingLadder(true);
            else if (ClimbingLadder && (_ladderHitCount == 0 || ShouldDismountLadder)) ToggleClimbingLadder(false);

            if (ClimbingLadder && ShouldCenterOnLadder) {
                var pos = _rb.position;
                var targetX = _ladderHits[0].transform.position.x;
                _rb.position = Vector2.SmoothDamp(pos, new Vector2(targetX, pos.y), ref _ladderSnapVel, _stats.LadderSnapTime);
            }
        }

        private void ToggleClimbingLadder(bool on) {
            if (ClimbingLadder == on) return;
            if (on) {
                _speed = Vector2.zero;
                _ladderSnapVel = Vector2.zero; // reset damping velocity for consistency
            }
            else {
                if (_ladderHitCount > 0) _frameLeftLadder = _fixedFrame; // to prevent immediately re-mounting ladder
                if (_frameInput.Move.y > 0) _speed.y += _stats.LadderPopForce; // Pop off ladders
            }

            ClimbingLadder = on;
            ResetAirJumps();
        }

        #endregion

        #region Crouching

        private int _frameStartedCrouching;

        private bool CrouchPressed => _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold;
        private bool CanStand => IsStandingPosClear(_rb.position + new Vector2(0, _stats.CrouchBufferCheck));

        protected virtual void HandleCrouching() {
            if (!_stats.AllowCrouching) return;

            if (!Crouching && CrouchPressed && _grounded) TryToggleCrouching(true);
            else if (Crouching && (!CrouchPressed || !_grounded)) TryToggleCrouching(false);
        }

        protected virtual bool TryToggleCrouching(bool shouldCrouch) {
            if (Crouching && !CanStand) return false;

            Crouching = shouldCrouch;
            ToggleColliders(!shouldCrouch);
            if (Crouching) _frameStartedCrouching = _fixedFrame;
            return true;
        }

        protected virtual void ToggleColliders(bool isStanding) {
            _environmentCol = isStanding ? _standingEnvironmentCollider : _crouchingEnvironmentCollider;
            _entityCol = isStanding ? _standingEntityCollider : _crouchingEntityCollider;
            //_standingEnvironmentCollider.enabled = isStanding;
            //_crouchingEnvironmentCollider.enabled = !isStanding;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _droppingDown;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private bool _wallJumpCoyoteUsable;
        private int _frameJumpWasPressed;
        private int _airJumpsRemaining;

        private bool HasBufferedJump => _bufferedJumpUsable && _fixedFrame < _frameJumpWasPressed + _stats.JumpBufferFrames;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _fixedFrame < _frameLeftGrounded + _stats.CoyoteFrames;
        private bool CanWallJump => (IsOnWall && !_isLeavingWall) || (_wallJumpCoyoteUsable && _fixedFrame < _frameLeftWall + _stats.WallJumpCoyoteFrames);
        private bool CanAirJump => !_grounded && _airJumpsRemaining > 0;

        protected virtual void HandleJump() {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true; // Early end detection

            if (_droppingDown) {
                Physics2D.IgnoreLayerCollision(8, 10, true);
                Invoke("DropDown", 0.5f);
            }

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (CanWallJump) WallJump();
            else if (_grounded || ClimbingLadder || CanUseCoyote) NormalJump();
            else if (_jumpToConsume && CanAirJump) AirJump();

            _jumpToConsume = false; // Always consume the flag
        }

        protected virtual void DropDown() {
            Physics2D.IgnoreLayerCollision(8, 10, false);
            _droppingDown = false;
        }

        // Includes Ladder Jumps
        protected virtual void NormalJump() {
            if (Crouching && !TryToggleCrouching(false)) return; // try standing up first so we don't get stuck in low ceilings
            _endedJumpEarly = false;
            _frameJumpWasPressed = 0; // prevents double-dipping 1 input's jumpToConsume and buffered jump for low ceilings
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            ToggleClimbingLadder(false);
            _speed.y = _stats.JumpPower;
            Jumped?.Invoke(false);
        }

        protected virtual void WallJump() {
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            if (IsOnWall) {
                _isLeavingWall = true; // only toggle if it's a real WallJump, not CoyoteWallJump
                ResetWallShimmy();
            }
            _wallJumpCoyoteUsable = false;
            _currentWallJumpMoveMultiplier = 0;
            _speed = Vector2.Scale(_stats.WallJumpPower, new(-_lastWallDirection, 1));
            Jumped?.Invoke(true);
        }

        protected virtual void AirJump() {
            _endedJumpEarly = false;
            _airJumpsRemaining--;
            _speed.y = _stats.JumpPower;
            _currentExternalVelocity.y = 0; // optional. test it out with a Bouncer if this feels better or worse
            AirJumped?.Invoke();
        }

        protected virtual void ResetJump() {
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            ResetAirJumps();
        }

        protected virtual void ResetAirJumps() => _airJumpsRemaining = _stats.MaxAirJumps;

        #endregion

        #region Dashing

        private bool _dashToConsume;
        private bool _canDash;
        private Vector2 _dashVel;
        private bool _dashing;
        private int _startedDashing;

        protected virtual void HandleDash() {
            if (_dashToConsume && _canDash && !Crouching) {
                var dir = new Vector2(_frameInput.Move.x, Mathf.Max(_frameInput.Move.y, 0f)).normalized;
                if (dir == Vector2.zero) {
                    _dashToConsume = false;
                    return;
                }

                _dashVel = dir * _stats.DashVelocity;
                _dashing = true;
                _canDash = false;
                _startedDashing = _fixedFrame;
                DashingChanged?.Invoke(true, dir);

                _currentExternalVelocity = Vector2.zero; // Strip external buildup
            }

            if (_dashing) {
                _speed = _dashVel;
                // Cancel when the time is out or we've reached our max safety distance
                if (_fixedFrame > _startedDashing + _stats.DashDurationFrames) {
                    _dashing = false;
                    DashingChanged?.Invoke(false, Vector2.zero);
                    _speed.y = Mathf.Min(0, _speed.y);
                    _speed.x *= _stats.DashEndHorizontalMultiplier;
                    if (_grounded) ResetDash();
                }
            }

            _dashToConsume = false;
        }

        protected virtual void ResetDash() {
            _canDash = true;
        }

        #endregion

        #region Attacking

        private bool _attackToConsume;
        private int _frameLastAttacked = int.MinValue;


        protected virtual void HandleAttacking() {
            if (!_attackToConsume) return;
            // note: animation looks weird if we allow attacking while crouched. consider different attack animations or not allow it while crouched
            if (_fixedFrame > _frameLastAttacked + _stats.AttackFrameCooldown) {
                _frameLastAttacked = _fixedFrame;
                Attacked?.Invoke();
            }

            _attackToConsume = false;
        }

        #endregion

        #region Clicking

        private bool _isHoldingClick;
        private bool _wasHoldingClick;
        private bool _clickToConsume;

        protected virtual void HandleClicking() {
            if (_isHoldingClick) {
                _wasHoldingClick = true;
            } else {
                _wasHoldingClick = false;
            }
            if (_wasHoldingClick && !_isHoldingClick) {
                _clickToConsume = true; // click released
            }
        }

        #endregion

        #region Horizontal

        private bool HorizontalInputPressed => Mathf.Abs(_frameInput.Move.x) > _stats.HorizontalDeadzoneThreshold;
        private bool _stickyFeet;

        protected virtual void HandleHorizontal() {
            if (_dashing || (shimmying && (_frameInput.Move.x > 0 && WallDirection > 0) || (_frameInput.Move.x < 0 && WallDirection < 0))) return;

            // Deceleration
            if (!HorizontalInputPressed) {
                var deceleration = _grounded ? _stats.GroundDeceleration * (_stickyFeet ? _stats.StickyFeetMultiplier : 1) : _stats.AirDeceleration;
                _speed.x = Mathf.MoveTowards(_speed.x, 0, deceleration * Time.fixedDeltaTime);
            }
            // Crawling
            else if (Crouching && _grounded) {
                var crouchPoint = Mathf.InverseLerp(0, _stats.CrouchSlowdownFrames, _fixedFrame - _frameStartedCrouching);
                var diminishedMaxSpeed = _stats.MaxSpeed * Mathf.Lerp(1, _stats.CrouchSpeedPenalty, crouchPoint);
                _speed.x = Mathf.MoveTowards(_speed.x, _frameInput.Move.x * diminishedMaxSpeed, _stats.GroundDeceleration * Time.fixedDeltaTime);
            }
            // Regular Horizontal Movement
            else {
                // Prevent useless horizontal speed buildup when against a wall
                if (_hittingWall.collider && Mathf.Abs(_rb.velocity.x) < 0.02f && !_isLeavingWall) _speed.x = 0;

                var xInput = _frameInput.Move.x * (ClimbingLadder ? _stats.LadderShimmySpeedMultiplier : 1);
                if (dest.target != null) {
                    float clampedArrivalSpeed = Mathf.Min(_stats.MaxSpeed * (perceivedDistanceToTarget / ai.slowdownDistance), _stats.MaxSpeed);
                }
                _speed.x = Mathf.MoveTowards(_speed.x, xInput * TrolSpeedAfterModifiers(), _currentWallJumpMoveMultiplier * _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        protected virtual float TrolSpeedAfterModifiers() {
            float modifiedMaxSpeed = _stats.MaxSpeed;
            if (dest.target != null) {
                modifiedMaxSpeed = Mathf.Min(_stats.MaxSpeed * (perceivedDistanceToTarget / ai.slowdownDistance), _stats.MaxSpeed); // clamp based on proximity
            }
            if (_spearless) {
                modifiedMaxSpeed *= 1.25f;
            } 

            // todo increase based on onscreen trol count

            return modifiedMaxSpeed;
        }

        #endregion

        #region Vertical

        private bool canShimmy = true;
        public bool shimmying = false;

        private void ResetWallShimmy() {
            canShimmy = true;
            shimmying = false;
        }

        protected virtual void HandleVertical() {
            if (_dashing) return;

            // Ladder
            if (ClimbingLadder) {
                var yInput = _frameInput.Move.y;
                _speed.y = yInput * (yInput > 0 ? _stats.LadderClimbSpeed : _stats.LadderSlideSpeed);
            }
            // Grounded & Slopes
            else if (_grounded && _speed.y <= 0f) {
                _speed.y = _stats.GroundingForce;

                if (TryGetGroundNormal(out var groundNormal)) {
                    GroundNormal = groundNormal;
                    if (!Mathf.Approximately(GroundNormal.y, 1f)) {
                        // on a slope
                        _speed.y = _speed.x * -GroundNormal.x / GroundNormal.y;
                        if (_speed.x != 0) _speed.y += _stats.GroundingForce;
                    }
                }
            }
            // Wall Climbing & Sliding
            else if (shimmying) {
                _speed.y = _stats.WallClimbSpeed;
            } else if (IsOnWall && !_isLeavingWall) {
                if (_frameInput.Move.x > 0 && WallDirection > 0 || _frameInput.Move.x < 0 && WallDirection < 0) {
                    _speed.x = 0;
                }
                if (_frameInput.Move.y > 0) {
                    if (canShimmy) {
                        _speed.y = _stats.WallClimbSpeed;
                        shimmying = true;
                        canShimmy = false;
                    } else {
                        _speed.y = -1;
                    }
                    
                } else if (_frameInput.Move.y < 0) _speed.y = -_stats.MaxWallFallSpeed;
                else if (GrabbingLedge) _speed.y = Mathf.MoveTowards(_speed.y, 0, _stats.LedgeGrabDeceleration * Time.fixedDeltaTime);
                else _speed.y = Mathf.MoveTowards(Mathf.Min(_speed.y, 0), -_stats.MaxWallFallSpeed, _stats.WallFallAcceleration * Time.fixedDeltaTime);
            }
            // In Air
            else {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _speed.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _speed.y = Mathf.MoveTowards(_speed.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        protected virtual void ApplyMovement() {
            if (!_hasControl) return;

            _rb.velocity = _speed + _currentExternalVelocity;
            _currentExternalVelocity = Vector2.MoveTowards(_currentExternalVelocity, Vector2.zero, _stats.ExternalVelocityDecay * Time.fixedDeltaTime);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (_stats == null) return;

            if (_stats.ShowWallDetection && _standingEnvironmentCollider != null) {
                Gizmos.color = Color.white;
                var bounds = GetWallDetectionBounds();
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            if (_stats.AllowLedges && _stats.ShowLedgeDetection) {
                Gizmos.color = Color.red;
                var facingDir = Mathf.Sign(WallDirection);
                var grabHeight = transform.position + _stats.LedgeGrabPoint.y * Vector3.up;
                var grabPoint = grabHeight + facingDir * _stats.LedgeGrabPoint.x * Vector3.right;
                Gizmos.DrawWireSphere(grabPoint, 0.05f);
                Gizmos.DrawWireSphere(grabPoint + Vector3.Scale(_stats.StandUpOffset, new(facingDir, 1)), 0.05f);
                Gizmos.DrawRay(grabHeight + _stats.LedgeRaycastSpacing * Vector3.down, 0.5f * facingDir * Vector3.right);
                Gizmos.DrawRay(grabHeight + _stats.LedgeRaycastSpacing * Vector3.up, 0.5f * facingDir * Vector3.right);
            }
        }

        private void OnEnable()
        {
            _stats.PlayerLayer = LayerMask.GetMask("player");
            _stats.ClimbableLayer = LayerMask.GetMask("climbable");
            _stats.LadderLayer = LayerMask.GetMask("ladder");
        }

        private void OnValidate() {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
            if (_standingEnvironmentCollider == null) Debug.LogWarning("Please assign a Capsule Collider to the Standing Environment Collider slot", this);
            if (_crouchingEnvironmentCollider == null) Debug.LogWarning("Please assign a Capsule Collider to the Crouching Environment Collider slot", this);
            if (_rb == null && !TryGetComponent(out _rb)) Debug.LogWarning("Ensure the GameObject with the Player Controller has a Rigidbody2D", this);
        }
#endif
    }

}