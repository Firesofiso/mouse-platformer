using UnityEngine;

public interface IGrabbable
{
    Transform GrabAnchor { get; }
    void OnGrabbed(GrabContext ctx);
    void OnReleased(GrabContext ctx);
    void WhileHeld(GrabContext ctx);
}
