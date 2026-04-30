using UnityEngine;

public class ClickableElement : MonoBehaviour
{
    public enum Interactions { Drag }

    [SerializeField] private readonly Interactions interactionType;

    public Transform objectTransform;
    public Rigidbody2D cursorRb;

    [SerializeField] Collider2D _thisCollider;

    public bool isBeingClicked = false;
    private Vector3 offset;
    private Transform _cursorTransform;

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

    private Vector2 CursorPos()
    {
        if (cursorRb != null) return cursorRb.position;
        if (_cursorTransform == null)
        {
            var cc = FindObjectOfType<CursorController>();
            if (cc != null) _cursorTransform = cc.transform;
        }
        return _cursorTransform != null ? (Vector2)_cursorTransform.position : Vector2.zero;
    }

    private void HandleClick()
    {
        if (_thisCollider.IsTouchingLayers(LayerMask.GetMask("cursor")))
            OnClicked();
    }

    private void HandleRelease()
    {
        if (isBeingClicked) OnClickReleased();
    }

    void Update()
    {
        if (isBeingClicked) WhileClicked();
    }

    void OnClicked()
    {
        isBeingClicked = true;
        offset = (Vector2)objectTransform.position - CursorPos();
    }

    void WhileClicked()
    {
        if (interactionType == Interactions.Drag)
            objectTransform.position = CursorPos() + (Vector2)offset;
    }

    void OnClickReleased()
    {
        isBeingClicked = false;
        objectTransform.position = ExtensionMethods.Round(objectTransform.position, 0);
        offset = Vector3.zero;
    }
}
