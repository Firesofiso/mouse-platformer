using System;
using UnityEngine;

// Place on the Player. Selects the nearest InteractionTarget within radius each frame.
// The cursor icon flies to the target's iconAnchor via SmoothDamp.
// E key: advances dialogue when a cutscene is playing, otherwise triggers the selection.
[RequireComponent(typeof(Collider2D))]
public class InteractionManager : MonoBehaviour
{
    public static event Action OnTargetAcquired;
    public static event Action OnTargetLost;

    [SerializeField] float _radius = 2f;
    [SerializeField] LayerMask _interactableLayer;
    [SerializeField] Transform _cursor;

    [Header("Cursor Motion")]
    [SerializeField] float _smoothTime = 0.08f;

    [Header("Input")]
    [SerializeField] KeyCode _interactKey = KeyCode.E;

    InteractionTarget _current;
    Vector3 _cursorVelocity;

    void Update()
    {
        if (ClickableElement.IsDragging || CursorGrabber.IsGrabbing)
        {
            _current = null;
        }
        else
        {
            SelectNearest();
            MoveCursor();
        }

        if (Input.GetKeyDown(_interactKey))
            HandleInteractPress();
    }

    void SelectNearest()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _radius, _interactableLayer);
        var draggedRoot = ClickableElement.CurrentDrag?.objectTransform;

        InteractionTarget best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var target = hit.GetComponent<InteractionTarget>();
            if (target == null) continue;
            if (draggedRoot != null && target.transform.IsChildOf(draggedRoot)) continue;
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < bestDist) { bestDist = dist; best = target; }
        }

        if (best == _current) return;
        _current = best;
        if (_current != null) OnTargetAcquired?.Invoke();
        else OnTargetLost?.Invoke();
    }

    void MoveCursor()
    {
        if (_cursor == null || _current == null) return;

        _cursor.position = Vector3.SmoothDamp(_cursor.position, _current.IconWorldPosition, ref _cursorVelocity, _smoothTime);
    }

    void HandleInteractPress()
    {
        if (CutsceneManager.IsPlaying)
        {
            DialogueInput.Fire();
            return;
        }

        _current?.Trigger();
    }
}
