public interface IGrabbable
{
    GrabConfig Config { get; }
    void OnGrabbed(CursorGrabber grabber);
    void OnReleased(CursorGrabber grabber);
    void WhileHeld(CursorGrabber grabber);
}
