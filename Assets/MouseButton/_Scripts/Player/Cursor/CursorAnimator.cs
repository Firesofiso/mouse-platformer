using UnityEngine;

public class CursorAnimator : MonoBehaviour
{
    [SerializeField] private CapsuleCollider2D _cursorCollider;
    private CursorController _cursor;
    [SerializeField] GameObject _visual;
    private SpriteRenderer _renderer;
    private Animator _anim;

    private void Awake()
    {
        _cursor = GetComponentInParent<CursorController>();
        _renderer = GetComponent<SpriteRenderer>();
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        CursorController.OnClick += OnClick;
        CursorController.OnRelease += OnRelease;
        InteractionManager.OnTargetAcquired += OnTargetAcquired;
        InteractionManager.OnTargetLost += OnTargetLost;
    }

    private void OnDisable()
    {
        CursorController.OnClick -= OnClick;
        CursorController.OnRelease -= OnRelease;
        InteractionManager.OnTargetAcquired -= OnTargetAcquired;
        InteractionManager.OnTargetLost -= OnTargetLost;
    }

    void Update()
    {
        HandleSpriteFlipping();
    }

    private void HandleSpriteFlipping()
    {
        _renderer.flipX = _cursor.flipX;
        _visual.transform.localPosition = _cursor.flipX ? new Vector3(-0, 0, 0) : new Vector3(0, 0, 0);
    }

    private void OnClick()           => _anim.SetBool(IsClicking, true);
    private void OnRelease()         => _anim.SetBool(IsClicking, false);
    private void OnTargetAcquired()  => _anim.SetBool(IsInteracting, true);
    private void OnTargetLost()      => _anim.SetBool(IsInteracting, false);

    #region Cached Properties

    private static readonly int Arrow       = Animator.StringToHash("Idle");
    private static readonly int ResetToArrow = Animator.StringToHash("resetToArrow");
    private static readonly int IsClicking  = Animator.StringToHash("IsClicking");
    private static readonly int IsInteracting = Animator.StringToHash("IsInteracting");

    private static readonly int Smile      = Animator.StringToHash("Smile");
    private static readonly int ToSmile    = Animator.StringToHash("toSmile");
    private static readonly int Surprise   = Animator.StringToHash("Surprise");
    private static readonly int ToSurprise = Animator.StringToHash("toSurprise");
    private static readonly int Frown      = Animator.StringToHash("Frown");
    private static readonly int ToFrown    = Animator.StringToHash("toFrown");
    private static readonly int Talk       = Animator.StringToHash("Talk");

    #endregion
}
