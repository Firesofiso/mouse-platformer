using UnityEngine;

public class DirectGrabbable : MonoBehaviour, IGrabbable
{
    [SerializeField] Transform _moveTarget;

    Transform MoveTarget => _moveTarget != null ? _moveTarget : transform;

    Vector2 _grabOffset;

    public Transform GrabAnchor => null;

    public void OnGrabbed(GrabContext ctx)
    {
        _grabOffset = (Vector2)MoveTarget.position - ctx.CursorPosition;
        ToggleCollisions(ctx.IgnoreColliders, true);
    }

    public void WhileHeld(GrabContext ctx)
    {
        var target = ctx.CursorPosition + _grabOffset;
        var t = MoveTarget;
        t.position = new Vector3(Mathf.Round(target.x), Mathf.Round(target.y), t.position.z);
    }

    public void OnReleased(GrabContext ctx)
    {
        var t = MoveTarget;
        var p = t.position;
        t.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), p.z);
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
