using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GroundedPhysicsBody : MonoBehaviour
{
    [SerializeField] UnitPhysicsStats _stats;

    public float SpeedX { get; set; }
    public float SpeedY { get; set; }
    public bool IsGrounded { get; private set; }

    Rigidbody2D _rb;
    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[4];

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        SpeedY = _rb.velocity.y;
        IsGrounded = CheckGrounded();

        if (IsGrounded && SpeedY <= 0f)
            SpeedY = _stats.GroundingForce;
        else
            SpeedY = Mathf.Max(SpeedY - _stats.FallAcceleration * Time.fixedDeltaTime, -_stats.MaxFallSpeed);

        _rb.velocity = new Vector2(SpeedX, SpeedY);
    }

    bool CheckGrounded()
    {
        int count = _rb.GetContacts(_contacts);
        for (int i = 0; i < count; i++)
            if (_contacts[i].normal.y > 0.5f) return true;
        return false;
    }
}
