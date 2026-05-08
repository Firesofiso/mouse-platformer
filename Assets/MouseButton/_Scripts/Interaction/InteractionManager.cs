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

    [Header("Input")]
    [SerializeField] KeyCode _interactKey = KeyCode.E;

    InteractionTarget _current;

    public static bool HasInteractionTarget => _instance != null && _instance._current != null;
    public InteractionTarget CurrentTarget => _current;

    static InteractionManager _instance;
    void Awake() => _instance = this;
    void OnDestroy() { if (_instance == this) _instance = null; }

    void Update()
    {
        if (CursorGrabber.IsGrabbing)
        {
            _current = null;
        }
        else
        {
            SelectNearest();
        }

        if (Input.GetKeyDown(_interactKey))
            HandleInteractPress();
    }

    void SelectNearest()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, _radius, _interactableLayer);
        var heldRoot = CursorGrabber.CurrentHeldTransform;

        InteractionTarget best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var target = hit.GetComponent<InteractionTarget>();
            if (target == null) continue;
            if (heldRoot != null && target.transform.IsChildOf(heldRoot)) continue;
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < bestDist) { bestDist = dist; best = target; }
        }

        if (best == _current) return;
        _current = best;
        if (_current != null) OnTargetAcquired?.Invoke();
        else OnTargetLost?.Invoke();
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
