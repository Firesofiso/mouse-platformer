using UnityEngine;

[RequireComponent(typeof(CursorController))]
public class CursorGrabber : MonoBehaviour
{
    public static bool IsGrabbing => _instance != null && _instance._held != null;

    static CursorGrabber _instance;

    // Set each frame by CursorController.FinalizePosition — the cursor's free/home position.
    public Vector2 TargetPosition { get; set; }

    // Transform of the held item. CursorController pins its visual here while grabbing.
    public Transform HeldTransform { get; private set; }

    IGrabbable _held;
    Collider2D _collider;

    void Awake()
    {
        _instance = this;
        _collider = GetComponent<Collider2D>();
    }
    void OnDestroy() { if (_instance == this) _instance = null; }

    void OnEnable()
    {
        CursorController.OnClick += TryGrab;
        CursorController.OnRelease += Release;
    }

    void OnDisable()
    {
        CursorController.OnClick -= TryGrab;
        CursorController.OnRelease -= Release;
    }

    void TryGrab()
    {
        var results = new System.Collections.Generic.List<Collider2D>();
        Physics2D.OverlapCollider(_collider, new ContactFilter2D().NoFilter(), results);

        IGrabbable best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in results)
        {
            if (hit.gameObject == gameObject) continue;
            var g = hit.GetComponent<IGrabbable>();
            if (g == null) continue;
            float d = Vector2.Distance(transform.position, hit.transform.position);
            if (d < bestDist) { bestDist = d; best = g; }
        }

        if (best == null) return;

        _held = best;
        HeldTransform = (best as MonoBehaviour)?.transform;
        TargetPosition = (Vector2)transform.position;
        best.OnGrabbed(this);
    }

    void Release()
    {
        if (_held == null) return;
        _held.OnReleased(this);
        _held = null;
        HeldTransform = null;
    }

    void FixedUpdate()
    {
        _held?.WhileHeld(this);
    }
}
