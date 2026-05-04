using System.Collections.Generic;
using TarodevController;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GroundedPhysicsBody : MonoBehaviour
{
    [SerializeField] ScriptableStats _stats;

    public float SpeedX { get; set; }
    public float SpeedY { get; set; }
    public bool IsGrounded { get; private set; }
    public bool OverrideGravity { get; set; }

    public void OverrideVelocity(Vector2 velocity)
    {
        SpeedX = velocity.x;
        SpeedY = velocity.y;
        OverrideGravity = true;
    }

    Rigidbody2D _rb;
    readonly List<IPhysicsModifier> _modifiers = new List<IPhysicsModifier>();
    static readonly ContactPoint2D[] _contacts = new ContactPoint2D[4];

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void AddModifier(IPhysicsModifier m) => _modifiers.Add(m);
    public void RemoveModifier(IPhysicsModifier m) => _modifiers.Remove(m);

    void FixedUpdate()
    {
        IsGrounded = CheckGrounded();

        if (!OverrideGravity)
        {
            SpeedY = _rb.velocity.y;
            if (IsGrounded && SpeedY <= 0f)
            {
                SpeedY = _stats.GroundingForce;
            }
            else
            {
                float gravity = _stats.FallAcceleration;
                float maxFall = _stats.MaxFallSpeed;
                foreach (var m in _modifiers)
                {
                    gravity = m.ModifyGravity(gravity);
                    maxFall = m.ModifyMaxFall(maxFall);
                }
                SpeedY = Mathf.Max(SpeedY - gravity * Time.fixedDeltaTime, -maxFall);
            }
        }

        _rb.velocity = new Vector2(SpeedX, SpeedY);
        OverrideGravity = false;
    }

    bool CheckGrounded()
    {
        int count = _rb.GetContacts(_contacts);
        for (int i = 0; i < count; i++)
            if (_contacts[i].normal.y > 0.5f) return true;
        return false;
    }
}
