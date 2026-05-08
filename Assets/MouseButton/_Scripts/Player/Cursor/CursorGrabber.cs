using UnityEngine;

[RequireComponent(typeof(CursorController))]
public class CursorGrabber : MonoBehaviour
{
    public static bool IsGrabbing => _instance != null && _instance._held != null;
    public static Transform CurrentHeldTransform => _instance?._held != null ? _instance.HeldTransform : null;

    static CursorGrabber _instance;

    public Transform HeldTransform { get; private set; }
    public Vector2 GrabOffset { get; private set; }
    public Vector2 GrabPoint => HeldTransform != null
        ? (Vector2)HeldTransform.position + GrabOffset
        : CursorController.CursorTargetPosition;

    [SerializeField] Collider2D[] _collisionIgnoreTargets;

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
        HeldTransform = best.GrabAnchor;
        GrabOffset = HeldTransform != null
            ? (Vector2)transform.position - (Vector2)HeldTransform.position
            : Vector2.zero;

        best.OnGrabbed(BuildContext());
    }

    void Release()
    {
        if (_held == null) return;
        _held.OnReleased(BuildContext());
        _held = null;
        HeldTransform = null;
        GrabOffset = Vector2.zero;
    }

    void LateUpdate()
    {
        _held?.WhileHeld(BuildContext());
    }

    GrabContext BuildContext() => new GrabContext
    {
        HomePosition = CursorController.CarryTargetPosition,
        CursorPosition = transform.position,
        IgnoreColliders = _collisionIgnoreTargets
    };
}
