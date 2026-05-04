using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using Tarodev;
using Tarodev.Trol;
using UnityEngine;

namespace TarodevController.Trol
{
    public class MobbTrolController : UnitController, ITrolUnit, ITrolBrainContext
    {
        #region Trol-specific fields

        [SerializeField] internal AIDestinationSetter _dest;
        [SerializeField] private Seeker _seeker;
        [SerializeField] private StatefulUnit _statefulUnit;

        [SerializeField] private CapsuleCollider2D _standingEntityCollider;
        [SerializeField] private CapsuleCollider2D _crouchingEntityCollider;
        [SerializeField] private PolygonCollider2D _spearTipCollider;

        internal CapsuleCollider2D _environmentCol => _col;
        private CapsuleCollider2D _entityCol;

        private int _oneWayLayerMask;

        #endregion

        #region Trol events

        public event Action<int> BustAMove;
        public event Func<FrameInput> GatherAIInput;
        public event Func<float> GetTrolSpeedAfterModifiers;
        public event Action<bool> HandleThrowing;
        public event Action HandleRecovery;
        public event Action<bool> HandleAiming;

        #endregion

        #region State lock

        [SerializeField] internal bool _stateLocked = false;
        public bool StateLocked => _stateLocked;
        internal Coroutine _lockStateCoroutine;

        internal void LockState(int t = 0)
        {
            if (_lockStateCoroutine != null) StopCoroutine(_lockStateCoroutine);
            _lockStateCoroutine = StartCoroutine(LockStateCoroutine(t));
        }

        private IEnumerator LockStateCoroutine(int t = 0)
        {
            _stateLocked = true;
            if (t > 0) { yield return new WaitForSeconds(t); UnlockState(); }
        }

        void UnlockState() { _stateLocked = false; _lockStateCoroutine = null; }

        #endregion

        protected virtual void Start()
        {
            _oneWayLayerMask = LayerMask.GetMask("one-way");
            GameManager.instance.trolManager.activeTrols.Add(this);

            // Frictionless material prevents the capsule catching on platform top edges mid-air.
            var frictionless = new PhysicsMaterial2D("TrolFrictionless") { friction = 0f, bounciness = 0f };
            _standingCollider.sharedMaterial  = frictionless;
            _crouchingCollider.sharedMaterial = frictionless;
        }

        protected override void GatherInput()
        {
            _frameInput = _input.FrameInput;

            if (_input.isPlayerUnit)
            {
                if (_stats.SnapInput)
                {
                    _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadzoneThreshold
                        ? 0 : Mathf.Sign(_frameInput.Move.x);
                    _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadzoneThreshold
                        ? 0 : Mathf.Sign(_frameInput.Move.y);
                }
            }
            else
            {
                _frameInput = GatherAIInput();
            }

            _statefulUnit?.FilterInput(ref _frameInput);
            HandleInputActions();
        }

        protected override float GetMaxSpeed() => GetTrolSpeedAfterModifiers?.Invoke() ?? _stats.MaxSpeed;

        protected override void ResetJump()
        {
            base.ResetJump();
            if (!_input.isPlayerUnit)
                _frameInput.JumpDown = _frameInput.JumpHeld = false;
        }

        protected override void CheckCollisions()
        {
            base.CheckCollisions();
            _bounceHitCount = Physics2D.CapsuleCastNonAlloc(
                _col.bounds.center, _col.size, _col.direction, 0,
                Vector2.down, _bounceHits, _stats.GrounderDistance,
                LayerMask.GetMask("EntityCollisions"));
        }

        protected override void HandleCollisions()
        {
            if (_bounceHitCount > 0)
            {
                Vector2 bounceVector = Vector2.zero;
                int validBounceCount = 0;
                foreach (RaycastHit2D boing in _bounceHits)
                {
                    if (boing.normal.y <= 0) continue;
                    validBounceCount++;
                    if (boing.collider.CompareTag("TROL") || boing.collider.CompareTag("MOUSE"))
                        bounceVector += boing.normal;
                }
                if (validBounceCount > 0)
                {
                    bounceVector /= validBounceCount;
                    bounceVector *= _stats.JumpPower;
                    SetVelocity(bounceVector, EntityForce.Decay);
                }
            }
            base.HandleCollisions();
        }

        protected override void ToggleColliders(bool isStanding)
        {
            base.ToggleColliders(isStanding);
            _entityCol = isStanding ? _standingEntityCollider : _crouchingEntityCollider;
        }

        // Trols don't wall-grab — prevents levitating against platform sides mid-jump.
        protected override void HandleWalls() { }

        protected override void HandleDropDownInput()
        {
            if (_col != null && _col.IsTouchingLayers(_oneWayLayerMask))
                _droppingDown = true;
        }

        protected override void HandleDropDown()
        {
            if (!_droppingDown) return;
            Physics2D.IgnoreLayerCollision(8, 10, true);
            Invoke(nameof(DropDownFinish), 0.5f);
            _droppingDown = false;
        }

        private void DropDownFinish() => Physics2D.IgnoreLayerCollision(8, 10, false);

        // Body owns gravity — controller only handles special vertical cases.
        protected override void HandleVertical()
        {
            if (_dashing) return;

            if (ClimbingLadder)
            {
                var yInput = _frameInput.Move.y;
                _speed.y = yInput * (yInput > 0 ? _stats.LadderClimbSpeed : _stats.LadderSlideSpeed);
                return;
            }

            if (_grounded && _speed.y <= 0f)
            {
                // Slope handling only — Body applies its own GroundingForce
                if (TryGetGroundNormal(out var groundNormal))
                {
                    GroundNormal = groundNormal;
                    _speed.y = Mathf.Approximately(GroundNormal.y, 1f)
                        ? 0f
                        : _speed.x * -GroundNormal.x / GroundNormal.y;
                }
                else
                {
                    _speed.y = 0f;
                }
                return;
            }

            if (shimmying)
            {
                _speed.y = _stats.WallClimbSpeed;
                return;
            }

            if (IsOnWall && !_isLeavingWall)
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
                return;
            }

            // Free fall — Body owns gravity. Sync _speed.y so in-air checks stay coherent.
            // Skip sync on jump frames: _rb.velocity.y is still 0 before ApplyMovement writes it.
            if (!_jumpedThisFrame)
                _speed.y = _rb.velocity.y;
        }

        private bool _jumpedThisFrame;

        protected override void NormalJump()  { base.NormalJump();  _jumpedThisFrame = true; }
        protected override void AirJump()     { base.AirJump();     _jumpedThisFrame = true; }
        protected override void WallJump()    { base.WallJump();    _jumpedThisFrame = true; }

        protected override void ApplyMovement()
        {
            if (!_hasControl) return;

            _currentExternalVelocity = Vector2.MoveTowards(
                _currentExternalVelocity, Vector2.zero, _stats.ExternalVelocityDecay * Time.fixedDeltaTime);

            var body = _statefulUnit?.Body;
            if (body == null) { _rb.velocity = _speed + _currentExternalVelocity; return; }

            body.SpeedX = _speed.x + _currentExternalVelocity.x;

            bool controllingY = _grounded || _dashing || ClimbingLadder || shimmying
                                || (IsOnWall && !_isLeavingWall) || _jumpedThisFrame
                                || _currentExternalVelocity.y != 0f;

            if (controllingY)
            {
                body.SpeedY = _speed.y + _currentExternalVelocity.y;
                body.OverrideGravity = true;
            }
            // else: Body handles gravity uninterrupted

            _jumpedThisFrame = false;
        }

        #region Spear

        private Coroutine _throwSpearCoroutine;

        [SerializeField] private TrolSpear _spear;
        public Transform SpearTransform => _spear?.transform;
        private bool _spearless;
        public bool Spearless
        {
            get => _spearless;
            set => _spearless = value;
        }

        private float _aimingTill = -1;
        public bool IsAiming
        {
            get => _aimingTill != -1;
            set
            {
                if (!value) { _aimingTill = -1; }
                else if (value && _aimingTill == -1) { _aimingTill = Time.time + 2; }
                HandleAiming?.Invoke(value);
            }
        }
        public bool ShouldThrow => IsAiming && _aimingTill < Time.time;

        internal void TriggerThrow() => OnThrowSpear();

        private bool _lastThrowTripped;
        public bool LastThrowTripped => _lastThrowTripped;

        public void LaunchSpear()
        {
            if (_throwSpearCoroutine != null) StopCoroutine(_throwSpearCoroutine);
            _spear.gameObject.SetActive(true);
            _lastThrowTripped = DidTrip();
            HandleThrowing?.Invoke(_lastThrowTripped);
            GameManager.instance.trolManager.activeSpears.Add(_spear);
            StartCoroutine(_spear.TemporarilyIgnoreColliders(
                new List<Collider2D>() { _environmentCol, _entityCol, _spearTipCollider }));
            Vector3 directionToTarget = _dest.target.position - transform.position;
            _spear.transform.up = directionToTarget;
            _spear._rb.velocity = directionToTarget * 2;
            Spearless = true;
            IsAiming = false;
        }

        public void SetRecovering() => HandleRecovery?.Invoke();

        protected virtual void OnThrowSpear()
        {
            if (_throwSpearCoroutine != null) StopCoroutine(_throwSpearCoroutine);
            _spear.gameObject.SetActive(true);
            _throwSpearCoroutine = StartCoroutine(ThrowSpearCoroutine());
        }

        private IEnumerator ThrowSpearCoroutine()
        {
            bool tripped = DidTrip();
            int recoveryTime = tripped ? 5 : 3;
            HandleThrowing?.Invoke(tripped);
            GameManager.instance.trolManager.activeSpears.Add(_spear);
            StartCoroutine(_spear.TemporarilyIgnoreColliders(
                new List<Collider2D>() { _environmentCol, _entityCol, _spearTipCollider }));
            Vector3 directionToTarget = _dest.target.position - transform.position;
            _spear.transform.up = directionToTarget;
            _spear._rb.velocity = directionToTarget * 2;
            Spearless = true;
            IsAiming = false;
            LockState();
            yield return new WaitForSeconds(recoveryTime);
            HandleRecovery?.Invoke();
            UnlockState();
        }

        public void TriggerGrabSpear() => OnGrabSpear(_spear?.transform);
        public void TriggerGrabSpear(Transform spearTransform) => OnGrabSpear(spearTransform);

        public Transform SelectClosestActiveSpear()
        {
            var trolManager = GameManager.instance?.trolManager;
            if (trolManager == null || trolManager.activeSpears == null || trolManager.activeSpears.Count == 0)
                return _spear?.transform;

            Transform closest = null;
            float closestSqrDist = float.PositiveInfinity;
            Vector2 myPosition = transform.position;

            foreach (var activeSpear in trolManager.activeSpears)
            {
                if (activeSpear == null || !activeSpear.gameObject.activeInHierarchy) continue;

                float sqrDist = ((Vector2)activeSpear.transform.position - myPosition).sqrMagnitude;
                if (sqrDist >= closestSqrDist) continue;
                closestSqrDist = sqrDist;
                closest = activeSpear.transform;
            }

            if (closest == null) return _spear?.transform;

            var spearComponent = closest.GetComponent<TrolSpear>();
            if (spearComponent != null) _spear = spearComponent;
            return closest;
        }

        void OnGrabSpear(Transform s)
        {
            if (!_spearless || s == null) return;
            _spear = s.GetComponent<TrolSpear>();
            if (_spear == null) return;
            GameManager.instance.trolManager.activeSpears.Remove(_spear);
            s.SetParent(transform);
            s.localPosition = Vector3.zero;
            s.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            s.gameObject.SetActive(false);
            Spearless = false;
            MustCelebrate = true;
        }

        protected virtual bool DidTrip() => UnityEngine.Random.Range(0, 4) == 0;

        #endregion

        #region Celebration

        private Coroutine _celebrateCoroutine;
        public float _celebratingTill = -1;

        public bool MustCelebrate
        {
            get => _celebratingTill == float.PositiveInfinity;
            set => _celebratingTill = value ? float.PositiveInfinity : -1;
        }

        public bool IsDancing
        {
            get => _celebratingTill > -1 && !MustCelebrate && Time.time < _celebratingTill;
            set => _celebratingTill = value ? Time.time + 3 : -1;
        }

        public void SetDancing(int durationInSeconds) => _celebratingTill = Time.time + durationInSeconds;

        void OnBeginCelebrating()
        {
            if (_celebrateCoroutine != null) StopCoroutine(_celebrateCoroutine);
            _celebrateCoroutine = StartCoroutine(CelebrateCoroutine());
        }

        private IEnumerator CelebrateCoroutine()
        {
            LockState();
            yield return new WaitForSeconds(1);
            if (!_input.isPlayerUnit)
            {
                SetDancing(3);
                yield return new WaitForSeconds(3);
            }
            MustCelebrate = false;
            UnlockState();
        }

        #endregion

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (_standingEntityCollider == null)
                Debug.LogWarning("Assign a Standing Entity Collider.", this);
        }
#endif
    }
}
