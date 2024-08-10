using UnityEngine;

namespace TarodevController {
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour {
        private IPlayerController _player;
        private Animator _anim;
        private SpriteRenderer _renderer;
        private AudioSource _source;

        private void Awake() {
            _player = GetComponentInParent<IPlayerController>();
            _anim = GetComponent<Animator>();
            _renderer = GetComponent<SpriteRenderer>();
            _source = GetComponent<AudioSource>();
        }

        private void Start() {
            // define animator functions for unit controller to call
            _player.GroundedChanged += OnGroundedChanged;
            _player.WallGrabChanged += OnWallGrabChanged;
            _player.DashingChanged += OnDashingChanged;
            _player.LedgeClimbChanged += OnLedgeClimbChanged;
            _player.Jumped += OnJumped;
            _player.AirJumped += OnAirJumped;
            _player.Attacked += OnAttacked;
        }

        private void Update() {
            HandleSpriteFlipping();
            HandleColliderFlipping();
            HandleGroundEffects();
            HandleWallSlideEffects();
            SetParticleColor(Vector2.down, _moveParticles);
            HandleAnimations();
        }

        private void HandleSpriteFlipping() {
            if (_player.ClimbingLedge) return;
            if (_isOnWall & _player.WallDirection != 0) _renderer.flipX = _player.WallDirection == -1;
            else if (_wallJumped) _renderer.flipX = _player.Speed.x < 0;
            else if (Mathf.Abs(_player.Input.x) > 0.1f) _renderer.flipX = _player.Input.x < 0;
        }

        [SerializeField] private CapsuleCollider2D _standingCollider;

        private void HandleColliderFlipping() {
            _standingCollider.offset = new Vector2((_renderer.flipX ? -1 : 1) * -0.03039861f, -0.001882311f);
        }

        #region Ground Movement

        [Header("GROUND MOVEMENT")] 
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private float _tiltChangeSpeed = .05f;
        [SerializeField] private float _maxTiltAngle = 45;
        [SerializeField] private AudioClip[] _footstepClips;

        private ParticleSystem.MinMaxGradient _currentGradient = new(Color.white, Color.white);
        private Vector2 _tiltVelocity;

        private void HandleGroundEffects() {
            // Move particles get bigger as you gain momentum
            var speedPoint = Mathf.InverseLerp(0, _player.PlayerStats.MaxSpeed, Mathf.Abs(_player.Speed.x));
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * (speedPoint + 5), 2 * Time.deltaTime);

            // Tilt with slopes
            var withinAngle = Vector2.Angle(Vector2.up, _player.GroundNormal) <= _maxTiltAngle;
            transform.up = Vector2.SmoothDamp(transform.up, _grounded && withinAngle ? _player.GroundNormal : Vector2.up, ref _tiltVelocity, _tiltChangeSpeed);
        }

        private int _stepIndex;

        // Called from AnimationEvent
        public void PlayFootstepSound() {
            _stepIndex = (_stepIndex + 1) % _footstepClips.Length;
            PlaySound(_footstepClips[_stepIndex], 0.01f);
        }

        #endregion

        #region Wall Sliding and Climbing

        [Header("WALL")] 
        [SerializeField] private float _wallHitAnimTime = 0.167f;
        [SerializeField] private ParticleSystem _wallSlideParticles;
        [SerializeField] private AudioSource _wallSlideSource;
        [SerializeField] private AudioClip[] _wallClimbClips;
        [SerializeField] private ParticleSystem _wallClimbParticles;
        [SerializeField] private float _maxWallSlideVolume = 0.2f;
        [SerializeField] private float _wallSlideVolumeSpeed = 0.6f;
        [SerializeField] private float _wallSlideParticleOffset = 10f;

        private bool _hitWall, _isOnWall, _isClimbing, _isSliding, _dismountedWall;

        private void OnWallGrabChanged(bool onWall) {
            _hitWall = _isOnWall = onWall;
            _dismountedWall = !onWall;
        }

        private void HandleWallSlideEffects() {
            var slidingThisFrame = _isOnWall && !_grounded && _player.Speed.y < 0;
            // _wallClimbParticles.Stop();
            if (!_isSliding && slidingThisFrame) {
                _isSliding = true;
                _wallSlideParticles.Play();
            }
            else if (_isSliding && !slidingThisFrame) {
                _isSliding = false;
                _wallSlideParticles.Stop();
            }
            // else if (_isOnWall && _player.Input.y > 0 && !_isClimbing) {
            //     _isClimbing = true;
            //     _wallClimbParticles.Play();
            // } else if (_player.Input.y <= 0  && _isClimbing || !_isOnWall) {
            //     _isClimbing = false;
            //     _wallClimbParticles.Stop();
            // }

            SetParticleColor(new Vector2(_player.WallDirection, 0), _wallSlideParticles);
            _wallSlideParticles.transform.localPosition = new Vector3(5 * _player.WallDirection, 0, 0);

            _wallSlideSource.volume = _isSliding || _player.ClimbingLadder && _player.Speed.y < 0
                ? Mathf.MoveTowards(_wallSlideSource.volume, _maxWallSlideVolume, _wallSlideVolumeSpeed * Time.deltaTime)
                : 0;
        }

        public void TriggerWallClimbParticles() {
            ParticleSystem.MainModule _wallClimbMain = _wallClimbParticles.main;
            if (_renderer.flipX) {
                _wallClimbParticles.transform.localPosition = new Vector3(7, 3, 0);
                _wallClimbMain.startSpeed = new ParticleSystem.MinMaxCurve(-0.5f, 1.0f);
            } else {
                _wallClimbParticles.transform.localPosition = new Vector3(-5, 3, 0);
                _wallClimbMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            }
            _wallClimbParticles.Play();
        }

        private int _wallClimbIndex = 0;

        // Called from AnimationEvent
        public void PlayWallClimbSound() {
            _wallClimbIndex = (_wallClimbIndex + 1) % _wallClimbClips.Length;
            PlaySound(_wallClimbClips[_wallClimbIndex], 0.1f);
        }

        #endregion

        #region Ledge Grabbing and Climbing

        private bool _isLedgeClimbing;
        private bool _climbIntoCrawl;

        private void OnLedgeClimbChanged(bool intoCrawl) {
            _isLedgeClimbing = true;
            _climbIntoCrawl = intoCrawl;
            
            UnlockAnimationLock(); // unlocks the LockState, so that ledge climbing animation doesn't get skipped
        }

        // Called from AnimationEvent
        public void TeleportPlayerMidLedgeClimb() {
            if (_player is PlayerController player) player.TeleportMidLedgeClimb();
        }

        // Called from AnimationEvent
        public void FinishLedgeClimbing() {
            _grounded = true;
            if (_player is PlayerController player) player.FinishClimbingLedge();
        }

        public void ShimmyComplete() {
            GetComponentInParent<PlayerController>().shimmying = false;
        }

        #endregion

        #region Ladders

        [Header("LADDER")]
        [SerializeField] private AudioClip[] _ladderClips;
        private int _climbIndex = 0;

        // Called from AnimationEvent
        public void PlayLadderClimbSound() {
            if (_player.Speed.y < 0) return;
            _climbIndex = (_climbIndex + 1) % _ladderClips.Length;
            PlaySound(_ladderClips[_climbIndex], 0.07f);
        }

        #endregion

        #region Dash

        [Header("DASHING")] 
        [SerializeField] private AudioClip _dashClip;
        [SerializeField] private ParticleSystem _dashParticles, _dashRingParticles;
        [SerializeField] private Transform _dashRingTransform;

        private void OnDashingChanged(bool dashing, Vector2 dir) {
            if (dashing) {
                _dashRingTransform.up = dir;
                _dashRingParticles.Play();
                _dashParticles.Play();
                PlaySound(_dashClip, 0.1f);
            }
            else {
                _dashParticles.Stop();
            }
        }

        #endregion

        #region Jumping and Landing

        [Header("JUMPING")] 
        [SerializeField] private float _minImpactForce = 5;
        [SerializeField] private float _maxImpactForce = 10;
        [SerializeField] private float _landAnimDuration = 1f;
        [SerializeField] private AudioClip _landClip, _jumpClip, _doubleJumpClip;
        [SerializeField] private ParticleSystem _jumpParticles, _launchParticles, _doubleJumpParticles, _landParticles;
        [SerializeField] private Transform _jumpParticlesParent;

        private bool _jumpTriggered;
        private bool _landed;
        private bool _grounded;
        private bool _wallJumped;

        private void OnJumped(bool wallJumped) {
            if (_player.ClimbingLedge) return;
            
            _jumpTriggered = true;
            _wallJumped = wallJumped;
            PlaySound(_jumpClip, 0.05f, Random.Range(0.98f, 1.02f));

            _jumpParticlesParent.localRotation = Quaternion.Euler(0, 0, _player.WallDirection * 60f);

            SetColor(_jumpParticles);
            SetColor(_launchParticles);
            _jumpParticles.Play();
        }

        private void OnAirJumped() {
            _jumpTriggered = true;
            _wallJumped = false;
            PlaySound(_doubleJumpClip, 0.1f);
            _doubleJumpParticles.Play();
        }

        private void OnGroundedChanged(bool grounded, float impactForce) {
            _grounded = grounded;
            _wallJumped = false;
            if (impactForce >= _minImpactForce) {
                var p = Mathf.InverseLerp(_minImpactForce, _maxImpactForce, impactForce);
                _landed = true;
                _landParticles.transform.localScale = new Vector3(p*8,p*4,1);
                _landParticles.Play();
                SetColor(_landParticles);
                PlaySound(_landClip, p * 0.1f);
            }

            if (_grounded) _moveParticles.Play();
            else _moveParticles.Stop();
        }

        #endregion

        #region Attack

        [Header("ATTACK")] 
        [SerializeField] private float _attackAnimTime = 0.25f;
        [SerializeField] private AudioClip _attackClip;
        private bool _attacked;

        private void OnAttacked() => _attacked = true;

        // Called from AnimationEvent
        public void PlayAttackSound() => PlaySound(_attackClip, 0.1f, Random.Range(0.97f, 1.03f));

        #endregion

        #region Animation

        private float _lockedTill, _isIdle;
        private int _wagInterval, _scritchInterval;

        private void HandleAnimations() {
            var state = GetState();
            ResetFlags();
            if (state == _currentState) return;
            _anim.Play(state, 0); //_anim.CrossFade(state, 0, 0);
            _currentState = state;

            int GetState() {
                if (Time.time < _lockedTill) return _currentState;
                if (_isLedgeClimbing) return LockState(_climbIntoCrawl ? LedgeClimbIntoCrawl : LedgeClimb, _player.PlayerStats.LedgeClimbDuration);
                if (_attacked) return LockState(Attack, _attackAnimTime);
                if (_player.ClimbingLadder) return _player.Speed.y == 0 || _grounded ? ClimbIdle : Climb;

                if (!_grounded) {
                    if (_hitWall) return LockState(WallHit, _wallHitAnimTime);
                    if (_isOnWall) {
                        if (_player.Input.y < 0 && _player.Velocity.y > Mathf.Abs(0.5f)) return WallSlide;
                        if (_player.GrabbingLedge) return LedgeGrab; // does this priority order give the right feel/look?
                        if (_player.Speed.y > 0) {
                            float normalizedSpeed = Mathf.Clamp((float)(_player.Speed.y / 2.25), 0, 1);
                            _anim.SetFloat("ClimbSpeed", normalizedSpeed);
                            return WallClimb;
                        } else return WallIdle;
                    }
                }

                if (_player.Crouching) return _player.Input.x == 0 || !_grounded ? Crouch : Crawl;
                if (_landed) return LockState(Land, _landAnimDuration);
                if (_jumpTriggered) return _wallJumped ? WallJump : Jump;

                if (_grounded)
                    if (_player.Input.x == 0 && _player.Speed.x == 0) { // stationary
                        if (_isIdle == 0) {
                            _isIdle = Time.time;
                            _wagInterval = Random.Range(3, 5);
                            _scritchInterval = Random.Range(10, 20);
                        } else if (_isIdle + _scritchInterval < Time.time || _isIdle > Time.time) {
                            if (_isIdle + _scritchInterval < Time.time) {
                                _isIdle = Time.time + 1;
                                _scritchInterval += Random.Range(10, 20);
                                _wagInterval = 0;
                            }
                            return IdleScritch;
                        } else if (_isIdle + _wagInterval < Time.time) {
                            return IdleWag;
                        } else {
                            return Idle;
                        }
                        return _currentState;
                    } else if ((_player.Input.x == 0 && _player.Speed.x != 0) | (_player.Input.x > 0) != (_player.Speed.x > 0)) { // changing direction or stopping
                        _isIdle = 0;
                        return Skid;
                    } else {
                        _isIdle = 0;
                        double xSpeedAbs = Mathf.Abs(_player.Speed.x);
                        if (xSpeedAbs > 3.5) {
                            float normalizedSpeed = Mathf.Clamp((float)(xSpeedAbs / 45) - 1, 0, 1); // max speed of 90, 1x speed up to 45
                            float runSpeed = Mathf.Lerp(1, 3, normalizedSpeed); // 1x @ 45 speed, 3x at 90 speed
                            _anim.SetFloat("RunSpeed", runSpeed);
                        } else {
                            _anim.SetFloat("RunSpeed", 1);
                        }
                        return Run;
                    }
                if (_player.Speed.y > 0) return _wallJumped ? WallJump : Jump;
                return _dismountedWall && _player.Input.x != 0 ? LockState(WallDismount, 0.1f) : Fall;
                // TODO: If WallDismount looks/feels good enough to keep, we should add clip duration (0.167f) to Stats

                int LockState(int s, float t) {
                    _lockedTill = Time.time + t;
                    return s;
                }
            }

            void ResetFlags() {
                _jumpTriggered = false;
                _landed = false;
                _attacked = false;
                _hitWall = false;
                _dismountedWall = false;
                _isLedgeClimbing = false;
            }
        }

        private void UnlockAnimationLock() => _lockedTill = 0f;

        #region Cached Properties

        private int _currentState;

        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int IdleScritch = Animator.StringToHash("IdleScritch");
        private static readonly int IdleWag = Animator.StringToHash("IdleWag");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int Skid = Animator.StringToHash("Skid");
        private static readonly int Crouch = Animator.StringToHash("Crouch");
        private static readonly int Crawl = Animator.StringToHash("Crawl");

        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Fall = Animator.StringToHash("Fall");
        private static readonly int Land = Animator.StringToHash("Land");
        
        private static readonly int ClimbIdle = Animator.StringToHash("ClimbIdle");
        private static readonly int Climb = Animator.StringToHash("Climb");
        
        private static readonly int WallHit = Animator.StringToHash("WallHit");
        private static readonly int WallIdle = Animator.StringToHash("WallIdle");
        private static readonly int WallClimb = Animator.StringToHash("WallClimb");
        private static readonly int WallSlide = Animator.StringToHash("WallSlide");
        private static readonly int WallDismount = Animator.StringToHash("WallDismount");
        private static readonly int WallJump = Animator.StringToHash("WallJump");

        private static readonly int LedgeGrab = Animator.StringToHash("LedgeGrab");
        private static readonly int LedgeClimb = Animator.StringToHash("LedgeClimb");
        private static readonly int LedgeClimbIntoCrawl = Animator.StringToHash("LedgeClimbIntoCrawl");

        private static readonly int Attack = Animator.StringToHash("Attack");
        #endregion

        #endregion

        #region Particles

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[2];

        private void SetParticleColor(Vector2 detectionDir, ParticleSystem system) {
            var hitCount = Physics2D.RaycastNonAlloc(transform.position, detectionDir, _groundHits, 2);
            if (hitCount <= 0) return;

            _currentGradient = _groundHits[0].transform.TryGetComponent(out SpriteRenderer r) 
                ? new ParticleSystem.MinMaxGradient(r.color * 0.9f, r.color * 1.2f) 
                : new ParticleSystem.MinMaxGradient(Color.white);

            SetColor(system);
        }

        private void SetColor(ParticleSystem ps) {
            var main = ps.main;
            main.startColor = _currentGradient;
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip, float volume = 1, float pitch = 1) {
            //_source.pitch = pitch;
            _source.PlayOneShot(clip, volume);
        }

        #endregion
    }
}