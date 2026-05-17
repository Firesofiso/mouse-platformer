using System;
using UnityEngine;

namespace TarodevController
{
    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action<bool, Vector2> DashingChanged;
        public event Action<bool> WallGrabChanged;
        public event Action<bool> LedgeClimbChanged;
        public event Action<bool> Jumped;
        public event Action AirJumped;
        public event Action Attacked;
        public event Action Clicked;
        public ScriptableStats PlayerStats { get; }
        public Vector2 Input { get; }
        public Vector2 Speed { get; }
        public Vector2 Velocity { get; }
        public Vector2 GroundNormal { get; }
        public int WallDirection { get; }
        public bool Crouching { get; }
        public bool ClimbingLadder { get; }
        public bool GrabbingLedge { get; }
        public bool ClimbingLedge { get; }
        public void ApplyVelocity(Vector2 vel, EntityForce forceType);
        public void SetVelocity(Vector2 vel, EntityForce velocityType);
    }

    public enum EntityForce
    {
        Burst,
        Decay,
    }


    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class UnitController : MonoBehaviour, IPlayerController
    {
        [SerializeField] protected ScriptableStats _stats;

        #region Internal

        [HideInInspector] public Rigidbody2D _rb;

        [SerializeField] protected CapsuleCollider2D _standingCollider;
        [SerializeField] protected CapsuleCollider2D _crouchingCollider;
        protected CapsuleCollider2D _col;

        protected UnitInput _input;
        protected bool _cachedTriggerSetting;
        protected FrameInput _frameInput;
        protected Vector2 _speed;
        protected Vector2 _currentExternalVelocity;
        protected int _fixedFrame;
        protected bool _hasControl = true;

        #endregion

        #region External

        public event Action<bool, float> GroundedChanged;
        public event Action<bool, Vector2> DashingChanged;
        public event Action<bool> WallGrabChanged;
        public event Action<bool> LedgeClimbChanged;
        public event Action<bool> Jumped;
        public event Action AirJumped;
        public event Action Attacked;
        public event Action Clicked;

        public ScriptableStats PlayerStats => _stats;
        public Vector2 Input => _frameInput.Move;
        public Vector2 Velocity => _rb.velocity;
        public Vector2 Speed => _speed;
        public Vector2 GroundNormal { get; protected set; }
        public int WallDirection { get; protected set; }
        public bool Crouching { get; protected set; }
        public bool ClimbingLadder { get; protected set; }
        public bool GrabbingLedge { get; protected set; }
        public bool JumpHeld => _frameInput.JumpHeld;
        public bool ClimbingLedge { get; protected set; }
        public bool IsGrounded => _grounded;
        public bool DroppingDown => _droppingDown;

        public virtual void ApplyVelocity(Vector2 vel, EntityForce forceType)
        {
            if (forceType == EntityForce.Burst) _speed += vel;
            else _currentExternalVelocity += vel;
        }

        public virtual void SetVelocity(Vector2 vel, EntityForce velocityType)
        {
            if (velocityType == EntityForce.Burst) _speed = vel;
            else _currentExternalVelocity = vel;
        }

        public virtual void TakeAwayControl(bool resetVelocity = true)
        {
            if (resetVelocity) _rb.velocity = Vector2.zero;
            _hasControl = false;
        }

        public virtual void ReturnControl()
        {
            _speed = Vector2.zero;
            _hasControl = true;
        }

        #endregion

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<UnitInput>();
            _cachedTriggerSetting = Physics2D.queriesHitTriggers;
            Physics2D.queriesStartInColliders = false;
            ToggleColliders(isStanding: true);
        }

        protected virtual void Update() => GatherInput();

        protected virtual void GatherInput()
        {
            _frameInput = _input.FrameInput;

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadzoneThreshold
                    ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadzoneThreshold
                    ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            HandleInputActions();
        }

        protected virtual void HandleInputActions()
        {
            if (_frameInput.DropDown)
                HandleDropDownInput();

            if (!_droppingDown && _frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _frameJumpWasPressed = _fixedFrame;
            }

            if (_frameInput.Move.x != 0) _stickyFeet = false;
            if (_frameInput.DashDown && _stats.AllowDash) _dashToConsume = true;
            if (_frameInput.AttackDown && _stats.AllowAttacks) _attackToConsume = true;
            if (_frameInput.ClickDown && _stats.AllowClicks) _isHoldingClick = true;
        }

        protected virtual void HandleDropDownInput() { }

        protected virtual void FixedUpdate()
        {
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

        private readonly Collider2D[] _standCheckBuffer = new Collider2D[4];
        protected readonly RaycastHit2D[] _groundHits = new RaycastHit2D[2];
        protected readonly RaycastHit2D[] _ceilingHits = new RaycastHit2D[2];
        protected readonly RaycastHit2D[] _bounceHits = new RaycastHit2D[5];
        protected int _bounceHitCount;
        private readonly Collider2D[] _wallHits = new Collider2D[5];
        private readonly Collider2D[] _ladderHits = new Collider2D[1];
        private RaycastHit2D _hittingWall;
        protected int _groundHitCount;
        private int _ceilingHitCount;
        private int _wallHitCount;
        private int _ladderHitCount;
        private int _frameLeftGrounded = int.MinValue;
        protected bool _grounded;
        private Vector2 _skinWidth = new(0.02f, 0.02f);

        protected virtual void CheckCollisions()
        {
            Physics2D.queriesHitTriggers = false;

            _groundHitCount = Physics2D.CapsuleCastNonAlloc(
                _col.bounds.center, _col.size, _col.direction, 0,
                Vector2.down, _groundHits, _stats.GrounderDistance, ~_stats.PlayerLayer);
            _ceilingHitCount = Physics2D.CapsuleCastNonAlloc(
                _col.bounds.center, _col.size, _col.direction, 0,
                Vector2.up, _ceilingHits, _stats.GrounderDistance, ~_stats.PlayerLayer);

            var bounds = GetWallDetectionBounds();
            _wallHitCount = Physics2D.OverlapBoxNonAlloc(
                bounds.center, bounds.size, 0, _wallHits, _stats.ClimbableLayer);
            _hittingWall = Physics2D.CapsuleCast(
                _col.bounds.center, _col.size, _col.direction, 0,
                new Vector2(_frameInput.Move.x, 0), _stats.GrounderDistance, ~_stats.PlayerLayer);

            Physics2D.queriesHitTriggers = true;
            _ladderHitCount = Physics2D.OverlapBoxNonAlloc(
                bounds.center, bounds.size, 0, _ladderHits, _stats.LadderLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
        }

        protected virtual void HandleCollisions()
        {
            if (_ceilingHitCount > 0 && IsCeilingHitSolid())
            {
                _currentExternalVelocity.y = Mathf.Min(0f, _currentExternalVelocity.y);
                _speed.y = Mathf.Min(0, _speed.y);
            }

            if (!_grounded && _groundHitCount > 0)
            {
                _grounded = true;
                ResetDash();
                ResetJump();
                GroundedChanged?.Invoke(true, Mathf.Abs(_speed.y));
                if (_frameInput.Move.x == 0) _stickyFeet = true;
            }
            else if (_grounded && _groundHitCount == 0)
            {
                _grounded = false;
                _frameLeftGrounded = _fixedFrame;
                GroundedChanged?.Invoke(false, 0);
            }
        }

        // Override to filter which ceiling hits block upward velocity (e.g. ignore one-way platforms)
        protected virtual bool IsCeilingHitSolid() => true;

        protected virtual bool TryGetGroundNormal(out Vector2 groundNormal)
        {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.Raycast(_rb.position, Vector2.down,
                _stats.GrounderDistance * 2, ~_stats.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            groundNormal = hit.normal;
            return hit.collider;
        }

        private Bounds GetWallDetectionBounds()
        {
            var colliderOrigin = _rb.position + _standingCollider.offset;
            return new Bounds(colliderOrigin, _stats.WallDetectorSize);
        }

        private bool IsStandingPosClear(Vector2 pos) => CheckPos(pos, _standingCollider);
        private bool IsCrouchingPosClear(Vector2 pos) => CheckPos(pos, _crouchingCollider);

        protected virtual bool CheckPos(Vector2 pos, CapsuleCollider2D col)
        {
            Physics2D.queriesHitTriggers = false;
            int count = Physics2D.OverlapCapsuleNonAlloc(
                pos + col.offset, col.size - _skinWidth, col.direction,
                0, _standCheckBuffer, ~_stats.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            for (int i = 0; i < count; i++)
                if (_standCheckBuffer[i].GetComponent<OneWayPlatformBehaviour>() == null)
                    return false;
            return true;
        }

        #endregion

        #region Walls

        private readonly ContactPoint2D[] _wallContact = new ContactPoint2D[1];
        private float _currentWallJumpMoveMultiplier = 1f;
        private int _lastWallDirection;
        private int _frameLeftWall;
        protected bool _isLeavingWall;
        public bool IsOnWall { get; private set; }

        protected virtual void HandleWalls()
        {
            if (!_stats.AllowWalls) return;

            _currentWallJumpMoveMultiplier = Mathf.MoveTowards(
                _currentWallJumpMoveMultiplier, 1f, 1f / _stats.WallJumpInputLossFrames);

            if (_wallHits[0]) _wallHits[0].GetContacts(_wallContact);
            WallDirection = _wallHitCount > 0
                ? (int)Mathf.Sign(_wallContact[0].point.x - transform.position.x) : 0;
            if (WallDirection != 0) _lastWallDirection = WallDirection;

            if (!IsOnWall && ShouldStickToWall()) ToggleOnWall(true);
            else if (IsOnWall && !ShouldStickToWall()) ToggleOnWall(false);

            bool ShouldStickToWall()
            {
                if (WallDirection == 0 || _grounded) return false;
                return !_stats.RequireInputPush
                    || (HorizontalInputPressed && Mathf.Sign(_frameInput.Move.x) == WallDirection);
            }
        }

        private void ToggleOnWall(bool on)
        {
            IsOnWall = on;
            if (on)
            {
                _speed = Vector2.zero;
                _currentExternalVelocity = Vector2.zero;
                _bufferedJumpUsable = true;
                _wallJumpCoyoteUsable = true;
            }
            else
            {
                _frameLeftWall = _fixedFrame;
                _isLeavingWall = false;
                ResetAirJumps();
                ResetWallShimmy();
            }
            WallGrabChanged?.Invoke(on);
        }

        #endregion

        #region Ledges

        private Vector2 _ledgeCornerPos;
        private bool _climbIntoCrawl;

        private bool LedgeClimbInputDetected =>
            Input.y > _stats.VerticalDeadzoneThreshold || Input.x == WallDirection;

        protected virtual void HandleLedges()
        {
            if (!_stats.AllowLedges || ClimbingLedge || !IsOnWall) return;
            GrabbingLedge = TryGetLedgeCorner(out _ledgeCornerPos);
            if (GrabbingLedge) HandleLedgeGrabbing();
        }

        protected virtual bool TryGetLedgeCorner(out Vector2 cornerPos)
        {
            cornerPos = Vector2.zero;
            var grabHeight = _rb.position + _stats.LedgeGrabPoint.y * Vector2.up;

            var hit1 = Physics2D.Raycast(grabHeight + _stats.LedgeRaycastSpacing * Vector2.down,
                WallDirection * Vector2.right, 0.5f, _stats.ClimbableLayer);
            if (!hit1.collider) return false;

            var hit2 = Physics2D.Raycast(grabHeight + _stats.LedgeRaycastSpacing * Vector2.up,
                WallDirection * Vector2.right, 0.5f, _stats.ClimbableLayer);
            if (hit2.collider) return false;

            var hit3 = Physics2D.Raycast(
                grabHeight + new Vector2(WallDirection * 0.5f, _stats.LedgeRaycastSpacing),
                Vector2.down, 0.5f, _stats.ClimbableLayer);
            if (!hit3.collider) return false;

            cornerPos = new(hit1.point.x, hit3.point.y);
            return true;
        }

        protected virtual void HandleLedgeGrabbing()
        {
            if (Input.x == 0 && _hasControl)
            {
                var targetPos = _ledgeCornerPos - Vector2.Scale(_stats.LedgeGrabPoint, new(WallDirection, 1f));
                _rb.position = Vector2.MoveTowards(_rb.position, targetPos,
                    _stats.LedgeGrabDeceleration * Time.fixedDeltaTime);
            }

            if (LedgeClimbInputDetected)
            {
                var finalPos = _ledgeCornerPos + Vector2.Scale(_stats.StandUpOffset, new(WallDirection, 1f));
                if (IsStandingPosClear(finalPos)) { _climbIntoCrawl = false; StartLedgeClimb(); }
                else if (_stats.AllowCrouching && IsCrouchingPosClear(finalPos)) { _climbIntoCrawl = true; StartLedgeClimb(intoCrawl: true); }
            }
        }

        protected virtual void StartLedgeClimb(bool intoCrawl = false)
        {
            LedgeClimbChanged?.Invoke(intoCrawl);
            TakeAwayControl();
            ClimbingLedge = true;
            GrabbingLedge = false;
            _rb.position = _ledgeCornerPos - Vector2.Scale(_stats.LedgeGrabPoint, new(WallDirection, 1f));
        }

        public virtual void TeleportMidLedgeClimb()
        {
            transform.position = _rb.position =
                _ledgeCornerPos + Vector2.Scale(_stats.StandUpOffset, new(WallDirection, 1f));
            if (_climbIntoCrawl) TryToggleCrouching(shouldCrouch: true);
            ToggleOnWall(false);
        }

        public virtual void FinishClimbingLedge()
        {
            ClimbingLedge = false;
            ReturnControl();
        }

        #endregion

        #region Ladders

        private Vector2 _ladderSnapVel;
        private int _frameLeftLadder;

        private bool CanEnterLadder => _ladderHitCount > 0 && _fixedFrame > _frameLeftLadder + _stats.LadderCooldownFrames;
        private bool ShouldMountLadder => _stats.AutoAttachToLadders
            || _frameInput.Move.y > _stats.VerticalDeadzoneThreshold
            || (!_grounded && _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold);
        private bool ShouldDismountLadder => !_stats.AutoAttachToLadders && _grounded
            && _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold;
        private bool ShouldCenterOnLadder => _stats.SnapToLadders && _frameInput.Move.x == 0 && _hasControl;

        protected virtual void HandleLadders()
        {
            if (!_stats.AllowLadders) return;
            if (!ClimbingLadder && CanEnterLadder && ShouldMountLadder) ToggleClimbingLadder(true);
            else if (ClimbingLadder && (_ladderHitCount == 0 || ShouldDismountLadder)) ToggleClimbingLadder(false);

            if (ClimbingLadder && ShouldCenterOnLadder)
            {
                var targetX = _ladderHits[0].transform.position.x;
                _rb.position = Vector2.SmoothDamp(_rb.position,
                    new Vector2(targetX, _rb.position.y), ref _ladderSnapVel, _stats.LadderSnapTime);
            }
        }

        private void ToggleClimbingLadder(bool on)
        {
            if (ClimbingLadder == on) return;
            if (on) { _speed = Vector2.zero; _ladderSnapVel = Vector2.zero; }
            else
            {
                if (_ladderHitCount > 0) _frameLeftLadder = _fixedFrame;
                if (_frameInput.Move.y > 0) _speed.y += _stats.LadderPopForce;
            }
            ClimbingLadder = on;
            ResetAirJumps();
        }

        #endregion

        #region Crouching

        private int _frameStartedCrouching;
        private bool CrouchPressed => _frameInput.Move.y < -_stats.VerticalDeadzoneThreshold;
        private bool CanStand => IsStandingPosClear(_rb.position + new Vector2(0, _stats.CrouchBufferCheck));

        protected virtual void HandleCrouching()
        {
            if (!_stats.AllowCrouching) return;
            if (!Crouching && CrouchPressed && _grounded) TryToggleCrouching(true);
            else if (Crouching && (!CrouchPressed || !_grounded)) TryToggleCrouching(false);
        }

        protected virtual bool TryToggleCrouching(bool shouldCrouch)
        {
            if (Crouching && !CanStand) return false;
            Crouching = shouldCrouch;
            ToggleColliders(!shouldCrouch);
            if (Crouching) _frameStartedCrouching = _fixedFrame;
            return true;
        }

        protected virtual void ToggleColliders(bool isStanding)
        {
            _col = isStanding ? _standingCollider : _crouchingCollider;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        protected bool _droppingDown;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private bool _wallJumpCoyoteUsable;
        private int _frameJumpWasPressed;
        private int _airJumpsRemaining;

        private bool HasBufferedJump => _bufferedJumpUsable && _fixedFrame < _frameJumpWasPressed + _stats.JumpBufferFrames;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _fixedFrame < _frameLeftGrounded + _stats.CoyoteFrames;
        private bool CanWallJump => (IsOnWall && !_isLeavingWall)
            || (_wallJumpCoyoteUsable && _fixedFrame < _frameLeftWall + _stats.WallJumpCoyoteFrames);
        private bool CanAirJump => !_grounded && _airJumpsRemaining > 0;

        protected virtual void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0)
                _endedJumpEarly = true;

            HandleDropDown();

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (CanWallJump) WallJump();
            else if (_grounded || ClimbingLadder || CanUseCoyote) NormalJump();
            else if (_jumpToConsume && CanAirJump) AirJump();

            _jumpToConsume = false;
        }

        protected virtual void HandleDropDown() { }

        protected virtual void NormalJump()
        {
            if (Crouching && !TryToggleCrouching(false)) return;
            _endedJumpEarly = false;
            _frameJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            ToggleClimbingLadder(false);
            _speed.y = _stats.JumpPower;
            Jumped?.Invoke(false);
        }

        protected virtual void WallJump()
        {
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            if (IsOnWall) { _isLeavingWall = true; ResetWallShimmy(); }
            _wallJumpCoyoteUsable = false;
            _currentWallJumpMoveMultiplier = 0;
            _speed = Vector2.Scale(_stats.WallJumpPower, new(-_lastWallDirection, 1));
            Jumped?.Invoke(true);
        }

        protected virtual void AirJump()
        {
            _endedJumpEarly = false;
            _airJumpsRemaining--;
            _speed.y = _stats.JumpPower;
            _currentExternalVelocity.y = 0;
            AirJumped?.Invoke();
        }

        protected virtual void ResetJump()
        {
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
        protected bool _dashing;
        private int _startedDashing;

        protected virtual void HandleDash()
        {
            if (_dashToConsume && _canDash && !Crouching)
            {
                var dir = new Vector2(_frameInput.Move.x, Mathf.Max(_frameInput.Move.y, 0f)).normalized;
                if (dir == Vector2.zero) { _dashToConsume = false; return; }
                _dashVel = dir * _stats.DashVelocity;
                _dashing = true;
                _canDash = false;
                _startedDashing = _fixedFrame;
                DashingChanged?.Invoke(true, dir);
                _currentExternalVelocity = Vector2.zero;
            }

            if (_dashing)
            {
                _speed = _dashVel;
                if (_fixedFrame > _startedDashing + _stats.DashDurationFrames)
                {
                    _dashing = false;
                    DashingChanged?.Invoke(false, Vector2.zero);
                    _speed.y = Mathf.Min(0, _speed.y);
                    _speed.x *= _stats.DashEndHorizontalMultiplier;
                    if (_grounded) ResetDash();
                }
            }

            _dashToConsume = false;
        }

        protected virtual void ResetDash() => _canDash = true;

        #endregion

        #region Attacking

        private bool _attackToConsume;
        private int _frameLastAttacked = int.MinValue;

        protected virtual void HandleAttacking()
        {
            if (!_attackToConsume) return;
            if (_fixedFrame > _frameLastAttacked + _stats.AttackFrameCooldown)
            {
                _frameLastAttacked = _fixedFrame;
                Attacked?.Invoke();
            }
            _attackToConsume = false;
        }

        #endregion

        #region Clicking

        private bool _isHoldingClick;
        private bool _wasHoldingClick;

        protected virtual void HandleClicking()
        {
            _wasHoldingClick = _isHoldingClick;
            if (_wasHoldingClick && !_isHoldingClick) { /* click released — fire Clicked? */ }
            _isHoldingClick = false;
        }

        #endregion

        #region Horizontal

        private bool HorizontalInputPressed => Mathf.Abs(_frameInput.Move.x) > _stats.HorizontalDeadzoneThreshold;
        private bool _stickyFeet;

        protected virtual void HandleHorizontal()
        {
            if (_dashing
                || (shimmying && _frameInput.Move.x > 0 && WallDirection > 0)
                || (_frameInput.Move.x < 0 && WallDirection < 0))
                return;

            if (!HorizontalInputPressed)
            {
                var decel = _grounded
                    ? _stats.GroundDeceleration * (_stickyFeet ? _stats.StickyFeetMultiplier : 1)
                    : _stats.AirDeceleration;
                _speed.x = Mathf.MoveTowards(_speed.x, 0, decel * Time.fixedDeltaTime);
            }
            else if (Crouching && _grounded)
            {
                var crouchPoint = Mathf.InverseLerp(0, _stats.CrouchSlowdownFrames, _fixedFrame - _frameStartedCrouching);
                var maxSpeed = _stats.MaxSpeed * Mathf.Lerp(1, _stats.CrouchSpeedPenalty, crouchPoint);
                _speed.x = Mathf.MoveTowards(_speed.x, _frameInput.Move.x * maxSpeed,
                    _stats.GroundDeceleration * Time.fixedDeltaTime);
            }
            else
            {
                if (_hittingWall.collider && Mathf.Abs(_rb.velocity.x) < 0.02f && !_isLeavingWall)
                    _speed.x = 0;
                var xInput = _frameInput.Move.x * (ClimbingLadder ? _stats.LadderShimmySpeedMultiplier : 1);
                _speed.x = Mathf.MoveTowards(_speed.x, xInput * GetMaxSpeed(),
                    _currentWallJumpMoveMultiplier * _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        protected virtual float GetMaxSpeed() => _stats.MaxSpeed;

        #endregion

        #region Vertical

        protected bool canShimmy = true;
        public bool shimmying = false;

        protected void ResetWallShimmy() { canShimmy = true; shimmying = false; }

        protected virtual void HandleVertical()
        {
            if (_dashing) return;

            if (ClimbingLadder)
            {
                var yInput = _frameInput.Move.y;
                _speed.y = yInput * (yInput > 0 ? _stats.LadderClimbSpeed : _stats.LadderSlideSpeed);
            }
            else if (_grounded && _speed.y <= 0f)
            {
                _speed.y = _stats.GroundingForce;
                if (TryGetGroundNormal(out var groundNormal))
                {
                    GroundNormal = groundNormal;
                    if (!Mathf.Approximately(GroundNormal.y, 1f))
                    {
                        _speed.y = _speed.x * -GroundNormal.x / GroundNormal.y;
                        if (_speed.x != 0) _speed.y += _stats.GroundingForce;
                    }
                }
            }
            else if (shimmying)
            {
                _speed.y = _stats.WallClimbSpeed;
            }
            else if (IsOnWall && !_isLeavingWall)
            {
                if ((_frameInput.Move.x > 0 && WallDirection > 0) || (_frameInput.Move.x < 0 && WallDirection < 0))
                    _speed.x = 0;

                if (_frameInput.Move.y > 0)
                {
                    if (canShimmy) { _speed.y = _stats.WallClimbSpeed; shimmying = true; canShimmy = false; }
                    else _speed.y = -1;
                }
                else if (_frameInput.Move.y < 0)
                    _speed.y = -_stats.MaxWallFallSpeed;
                else if (GrabbingLedge)
                    _speed.y = Mathf.MoveTowards(_speed.y, 0, _stats.LedgeGrabDeceleration * Time.fixedDeltaTime);
                else
                    _speed.y = Mathf.MoveTowards(Mathf.Min(_speed.y, 0), -_stats.MaxWallFallSpeed,
                        _stats.WallFallAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                var gravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _speed.y > 0) gravity *= _stats.JumpEndEarlyGravityModifier;
                _speed.y = Mathf.MoveTowards(_speed.y, -_stats.MaxFallSpeed, gravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        protected virtual void ApplyMovement()
        {
            if (!_hasControl) return;
            _rb.velocity = _speed + _currentExternalVelocity;
            _currentExternalVelocity = Vector2.MoveTowards(
                _currentExternalVelocity, Vector2.zero, _stats.ExternalVelocityDecay * Time.fixedDeltaTime);
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (_stats == null) return;
            if (_stats.ShowWallDetection && _standingCollider != null)
            {
                Gizmos.color = Color.white;
                var bounds = GetWallDetectionBounds();
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
            if (_stats.AllowLedges && _stats.ShowLedgeDetection)
            {
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

        protected virtual void OnEnable()
        {
            if (_stats == null) return;
            _stats.PlayerLayer = LayerMask.GetMask("player");
            _stats.ClimbableLayer = LayerMask.GetMask("climbable");
            _stats.LadderLayer = LayerMask.GetMask("ladder");
        }

        protected virtual void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Assign a ScriptableStats asset.", this);
            if (_standingCollider == null) Debug.LogWarning("Assign a Standing Collider.", this);
            if (_crouchingCollider == null) Debug.LogWarning("Assign a Crouching Collider.", this);
            if (_rb == null && !TryGetComponent(out _rb)) Debug.LogWarning("Needs a Rigidbody2D.", this);
        }
#endif
    }
}
