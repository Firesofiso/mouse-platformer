using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FreeGrabbable : MonoBehaviour, IGrabbable
{
    [SerializeField] GrabConfig _config;
    public GrabConfig Config => _config;

    Rigidbody2D _rb;
    TargetJoint2D _joint;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void OnGrabbed(CursorGrabber grabber)
    {
        _joint = gameObject.AddComponent<TargetJoint2D>();
        _joint.autoConfigureTarget = false;
        _joint.target = grabber.TargetPosition;
        _joint.maxForce = _config.maxForce;
        _joint.frequency = _config.frequency;
        _joint.dampingRatio = _config.dampingRatio;

        SetPlayerCollision(true);
    }

    public void OnReleased(CursorGrabber grabber)
    {
        Destroy(_joint);
        _joint = null;
        SetPlayerCollision(false);
    }

    public void WhileHeld(CursorGrabber grabber)
    {
        if (_joint == null) return;
        _joint.target = grabber.TargetPosition;
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
