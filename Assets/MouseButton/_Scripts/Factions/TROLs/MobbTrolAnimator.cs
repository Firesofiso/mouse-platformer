using UnityEngine;

namespace TarodevController.Trol.MobbTrol
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class MobbTrolAnimator : MonoBehaviour
    {
        // Serialized Fields
        [SerializeField]
        private CapsuleCollider2D _standingCollider;

        [SerializeField]
        private PolygonCollider2D _spearCol;

        [SerializeField]
        private PlatformEffector2D _spearEffector;

        [Header("JUMPING")]
        [SerializeField]
        private float _minImpactForce = 5;

        [SerializeField]
        private float _maxImpactForce = 10;

        [SerializeField]
        private float _landAnimDuration = .1f;

        // Private Fields
        private MobbTrolController _controller;
        private Animator _anim;
        private SpriteRenderer _renderer;
        private AudioSource _source;
        private float spearOffsetXHalf = 4.5f;
        private float newSpearOffsetX = 0f;
        private bool _jumpTriggered;
        private bool _landed;
        private bool _recoveringFromThrow;
        private bool _tripped;
        private float _lockedTill;
        private int _currentState;

        // Public Fields
        public int spearColOffsetY = 0;

        private void Awake()
        {
            _controller = GetComponentInParent<MobbTrolController>();
            _anim = GetComponent<Animator>();
            _renderer = GetComponent<SpriteRenderer>();
            _source = GetComponent<AudioSource>();
        }

        private void Start()
        {
            _controller.GroundedChanged += OnGroundedChanged;
            _controller.Jumped += OnJumped;
            _controller.HandleAiming += HandleAiming;
            _controller.HandleThrowing += HandleThrowing;
            _controller.HandleRecovery += HandleRecovery;
        }

        private void Update()
        {
            HandleSpriteFlipping();
            HandleAnimations();
        }

        private void HandleSpriteFlipping()
        {
            if (_controller.IsAiming)
                _renderer.flipX =
                    _controller._dest?.target?.position.x < _controller._rb.position.x;
            if (_controller.Input.x != 0)
                _renderer.flipX = _controller.Input.x < 0;

            if (_renderer.flipX)
                newSpearOffsetX = 4.5f + spearOffsetXHalf;
            else
                newSpearOffsetX = 4.5f - spearOffsetXHalf;
            _spearCol.offset = new Vector2(newSpearOffsetX, spearColOffsetY);
        }

        #region Jumping and Landing

        private void OnJumped(bool wallJumped)
        {
            _jumpTriggered = true;
        }

        private void OnGroundedChanged(bool grounded, float impactForce)
        {
            if (impactForce >= _minImpactForce)
            {
                _landed = true;
            }
        }

        #endregion

        #region Attack

        private void HandleAiming(bool isAiming)
        {
            if (isAiming)
            {
                spearOffsetXHalf = -1f;
                _spearEffector.rotationalOffset = _renderer.flipX ? 45 : -45;
            }
            else
            {
                spearOffsetXHalf = 4.5f;
                _spearEffector.rotationalOffset = 0;
            }
        }

        private void HandleThrowing(bool tripped)
        {
            HandleAiming(false);
            _recoveringFromThrow = true;
            _tripped = tripped;
            _spearCol.enabled = false;
        }

        private void HandleRecovery()
        {
            _recoveringFromThrow = false;
            UnlockAnimationLock();
        }

        #endregion

        #region Animation

        private void HandleAnimations()
        {
            var state = GetState();
            ResetFlags();
            if (state == _currentState)
                return;
            _anim.Play(state, 0);
            _currentState = state;

            int GetState()
            {
                if (Time.time < _lockedTill)
                    return _currentState;

                if ((_controller.MustCelebrate || _controller.IsDancing) && _controller.IsGrounded)
                {
                    return _controller.IsDancing ? Dance : Celebrate;
                }
                if (_controller.IsAiming)
                    return Aim;
                if (_recoveringFromThrow)
                    return LockState(_tripped ? ThrowTrip : Throw, 10);
                if (_landed)
                    return LockState(
                        _controller.Spearless ? SpearlessLand : Land,
                        _landAnimDuration
                    );
                if (_jumpTriggered)
                    return _controller.Spearless ? SpearlessJump : Jump;

                if (_controller.IsGrounded)
                {
                    return GetGroundedState();
                }

                return GetAirborneState();
            }
            int GetGroundedState()
            {
                if (_controller.Input.x == 0 && _controller.Speed.x == 0)
                {
                    return _controller.Spearless ? SpearlessIdle : Idle;
                }

                if (
                    (_controller.Input.x == 0 && _controller.Speed.x != 0)
                    || (_controller.Input.x > 0) != (_controller.Speed.x > 0)
                )
                {
                    return _controller.Spearless ? SpearlessSkid : Skid;
                }

                return _controller.Spearless ? SpearlessRun : Run;
            }

            int GetAirborneState()
            {
                if (_controller.Speed.y > 0)
                    return _controller.Spearless ? SpearlessJump : Jump;
                return _controller.Spearless ? SpearlessFall : Fall;
            }

            int LockState(int s, float t)
            {
                _lockedTill = Time.time + t;
                return s;
            }

            void ResetFlags()
            {
                _jumpTriggered = false;
                _landed = false;
            }
        }

        private void UnlockAnimationLock() => _lockedTill = 0f;

        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int SpearlessIdle = Animator.StringToHash("SpearlessIdle");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int SpearlessRun = Animator.StringToHash("SpearlessRun");
        private static readonly int Skid = Animator.StringToHash("Skid");
        private static readonly int SpearlessSkid = Animator.StringToHash("SpearlessSkid");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int SpearlessJump = Animator.StringToHash("SpearlessJump");
        private static readonly int Fall = Animator.StringToHash("Fall");
        private static readonly int SpearlessFall = Animator.StringToHash("SpearlessFall");
        private static readonly int Land = Animator.StringToHash("Land");
        private static readonly int SpearlessLand = Animator.StringToHash("SpearlessLand");
        private static readonly int Celebrate = Animator.StringToHash("ReclaimSpear");
        private static readonly int Dance = Animator.StringToHash("Dance");
        private static readonly int Aim = Animator.StringToHash("Aim");
        private static readonly int Throw = Animator.StringToHash("Throw");
        private static readonly int ThrowTrip = Animator.StringToHash("ThrowTrip");

        #endregion
    }
}
