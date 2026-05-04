using UnityEngine;

public class ClickableElement : MonoBehaviour
{
    public enum Interactions { Drag }

    [SerializeField] Interactions interactionType;

    public Transform objectTransform;
    public Rigidbody2D cursorRb;

    [SerializeField] Collider2D _thisCollider;

    public static bool IsDragging { get; private set; }
    public static ClickableElement CurrentDrag { get; private set; }

    public bool isBeingClicked;

    Vector3 _offset;
    Transform _cursorTransform;
    int _cursorLayerMask;

    void Awake()
    {
        _cursorLayerMask = LayerMask.GetMask("cursor");
    }

    void OnEnable()
    {
        CursorController.OnClick += HandleClick;
        CursorController.OnRelease += HandleRelease;
    }

    void OnDisable()
    {
        CursorController.OnClick -= HandleClick;
        CursorController.OnRelease -= HandleRelease;
    }

    void Update()
    {
        if (isBeingClicked && interactionType == Interactions.Drag)
            objectTransform.position = CursorPos() + (Vector2)_offset;
    }

    Vector2 CursorPos()
    {
        if (cursorRb != null) return (Vector2)cursorRb.transform.position;
        if (_cursorTransform == null && CursorController.Instance != null)
            _cursorTransform = CursorController.Instance.transform;
        return _cursorTransform != null ? (Vector2)_cursorTransform.position : Vector2.zero;
    }

    void HandleClick()
    {
        if (!_thisCollider.bounds.Contains((Vector3)CursorPos())) return;
        isBeingClicked = true;
        IsDragging = true;
        CurrentDrag = this;
        _offset = (Vector2)objectTransform.position - CursorPos();
        SetCollisionWithPlayer(true);
    }

    void HandleRelease()
    {
        if (!isBeingClicked) return;
        isBeingClicked = false;
        IsDragging = false;
        CurrentDrag = null;
        objectTransform.position = objectTransform.position.Round(0);
        _offset = Vector3.zero;
        SetCollisionWithPlayer(false);
    }

    void SetCollisionWithPlayer(bool ignore)
    {
        var draggedColliders = objectTransform.GetComponentsInChildren<Collider2D>();
        var playerObj = Object.FindObjectOfType<PlayerObject>();
        var playerColliders = playerObj?.GetComponentsInChildren<Collider2D>();
        if (playerColliders == null) return;

        foreach (var dc in draggedColliders)
            foreach (var pc in playerColliders)
                Physics2D.IgnoreCollision(dc, pc, ignore);
    }
}
