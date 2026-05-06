using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FreeGrabbable : MonoBehaviour, IGrabbable
{
    [SerializeField] GrabConfig _config;
    [SerializeField] Transform _moveTarget;
    public GrabConfig Config => _config;

    Transform MoveTarget => _moveTarget != null ? _moveTarget : transform;

    Rigidbody2D _rb;
    TargetJoint2D _joint;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void OnGrabbed(CursorGrabber grabber)
    {
        grabber.HeldTransform = _config.mode == GrabConfig.GrabMode.Physics ? MoveTarget : null;

        if (_config.mode == GrabConfig.GrabMode.Physics)
        {
            _joint = gameObject.AddComponent<TargetJoint2D>();
            _joint.autoConfigureTarget = false;
            _joint.target = grabber.SidekickHomePosition;
            _joint.maxForce = _config.maxForce;
            _joint.frequency = _config.frequency;
            _joint.dampingRatio = _config.dampingRatio;
        }

        SetPlayerCollision(true);
    }

    public void OnReleased(CursorGrabber grabber)
    {
        if (_config.mode == GrabConfig.GrabMode.Physics)
        {
            if (_joint != null) { _joint.enabled = false; Destroy(_joint); _joint = null; }
        }
        else
        {
            var p = MoveTarget.position;
            MoveTarget.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), p.z);
        }

        SetPlayerCollision(false);
    }

    public void WhileHeld(CursorGrabber grabber)
    {
        if (_config.mode == GrabConfig.GrabMode.Physics)
        {
            if (_joint == null) return;
            var target = grabber.SidekickHomePosition;
            if (!_config.cursorCanLift)
                target.y = Mathf.Min(target.y, _rb.position.y);
            _joint.target = target;
        }
        else
        {
            var target = grabber.SidekickHomePosition - grabber.HeldCursorOffset;
            MoveTarget.position = new Vector3(Mathf.Round(target.x), Mathf.Round(target.y), MoveTarget.position.z);
        }
    }

    void SetPlayerCollision(bool ignore)
    {
        var myColliders = GetComponentsInChildren<Collider2D>();
        var player = Object.FindObjectOfType<PlayerObject>();
        var playerColliders = player?.GetComponentsInChildren<Collider2D>();
        if (playerColliders == null) return;

        foreach (var mc in myColliders)
            foreach (var pc in playerColliders)
                Physics2D.IgnoreCollision(mc, pc, ignore);
    }
}
