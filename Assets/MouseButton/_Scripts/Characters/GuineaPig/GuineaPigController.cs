using UnityEngine;

public class GuineaPigController : MonoBehaviour
{
    [SerializeField] GroundedPhysicsBody _body;
    [SerializeField] CapsuleCollider2D _col;
    [SerializeField] float _maxSpeed = 40f;
    [SerializeField] float _acceleration = 120f;
    [SerializeField] SpriteRenderer _renderer;
    [SerializeField] Animator _animator;

    [Header("Idle Loop")]
    [SerializeField] Vector2 _idleDuration = new Vector2(1f, 3f);
    [SerializeField] Vector2 _roamDuration = new Vector2(2f, 5f);

    [Header("Ledge Detection")]
    [Tooltip("How far ahead of the collider edge to cast the ledge-detection ray")]
    [SerializeField] float _ledgeLookahead = 3f;
    [Tooltip("Downward raycast distance to consider ground present")]
    [SerializeField] float _ledgeDropDistance = 10f;

    enum State { Idle, Roam }
    State _state;
    float _stateTimer;
    int _facing = 1;
    float _currentSpeed;
    int _groundMask;

    static readonly int IdleHash = Animator.StringToHash("Idle");
    static readonly int RunHash = Animator.StringToHash("Run");

    void Start()
    {
        _groundMask = LayerMask.GetMask("Ground", "one-way", "climbable");
        EnterIdle();
    }

    void FixedUpdate()
    {
        if (!_renderer.isVisible)
        {
            ApplySpeed(0f);
            return;
        }

        _stateTimer -= Time.fixedDeltaTime;

        switch (_state)
        {
            case State.Idle:
                if (_stateTimer <= 0f) { EnterRoam(); break; }
                ApplySpeed(0f);
                break;

            case State.Roam:
                if (_stateTimer <= 0f) { EnterIdle(); ApplySpeed(0f); break; }
                if (_body.IsGrounded && IsLedgeAhead()) _facing = -_facing;
                ApplySpeed(_facing * _maxSpeed);
                break;
        }
    }

    void ApplySpeed(float target)
    {
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, _acceleration * Time.fixedDeltaTime);
        _body.SpeedX = _currentSpeed;

        float runSpeed = _maxSpeed > 0f ? Mathf.Abs(_currentSpeed) / _maxSpeed : 0f;
        _animator.SetFloat("RunSpeed", Mathf.Max(runSpeed, 0.1f));

        if (_renderer != null && Mathf.Abs(_currentSpeed) > 0.01f)
            _renderer.flipX = _currentSpeed < 0f;
    }

    void EnterIdle()
    {
        _state = State.Idle;
        _stateTimer = Random.Range(_idleDuration.x, _idleDuration.y);
        _animator.Play(IdleHash);
    }

    void EnterRoam()
    {
        _state = State.Roam;
        _stateTimer = Random.Range(_roamDuration.x, _roamDuration.y);
        _facing = Random.value < 0.5f ? -1 : 1;
        _animator.Play(RunHash);
    }

    bool IsLedgeAhead()
    {
        if (_col == null) return false;
        var b = _col.bounds;
        float edgeX = _facing > 0 ? b.max.x : b.min.x;
        var origin = new Vector2(edgeX + _facing * _ledgeLookahead, b.min.y);
        var hit = Physics2D.Raycast(origin, Vector2.down, _ledgeDropDistance, _groundMask);
        Debug.DrawRay(origin, Vector2.down * _ledgeDropDistance,
            hit.collider == null ? Color.red : Color.green);
        return hit.collider == null;
    }
}
