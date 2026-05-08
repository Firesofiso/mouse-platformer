using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TargetJoint2D))]
public class PhysicsGrabbable : MonoBehaviour, IGrabbable
{
    [SerializeField] PhysicsGrabConfig _config;

    public Transform GrabAnchor => transform;

    Rigidbody2D _rb;
    TargetJoint2D _joint;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _joint = GetComponent<TargetJoint2D>();
        _joint.autoConfigureTarget = false;
        _joint.enabled = false;
    }

    public void OnGrabbed(GrabContext ctx)
    {
        _joint.maxForce = _config.maxForce;
        _joint.frequency = _config.frequency;
        _joint.dampingRatio = _config.dampingRatio;
        _joint.target = ctx.HomePosition;
        _joint.enabled = true;
        ToggleCollisions(ctx.IgnoreColliders, true);
    }

    public void WhileHeld(GrabContext ctx)
    {
        var target = ctx.HomePosition;
        if (!_config.cursorCanLift)
            target.y = Mathf.Min(target.y, _rb.position.y);
        _joint.target = target;


    }

    public void OnReleased(GrabContext ctx)
    {
        _joint.enabled = false;
        ToggleCollisions(ctx.IgnoreColliders, false);
    }

    void ToggleCollisions(Collider2D[] others, bool ignore)
    {
        if (others == null) return;
        var mine = GetComponentsInChildren<Collider2D>();
        foreach (var mc in mine)
            foreach (var oc in others)
                Physics2D.IgnoreCollision(mc, oc, ignore);
    }
}
