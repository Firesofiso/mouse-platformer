using UnityEngine;

public class CursorAnimator : MonoBehaviour
{
    [SerializeField] private Collider2D _cursorCollider;
    private CursorController _cursor;
    [SerializeField] GameObject _visual;
    [SerializeField] float _flipOffsetX = 0f;
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
        // _visual.transform.localPosition = new Vector3(_cursor.flipX ? -_flipOffsetX : _flipOffsetX, 0, 0);
        // if (_cursorCollider != null)
        // {
        //     var off = _cursorCollider.offset;
        //     off.x = _cursor.flipX ? -Mathf.Abs(off.x) : Mathf.Abs(off.x);
        //     _cursorCollider.offset = off;
        // }
    }

    private void OnClick()           => _anim.SetBool(IsClicking, true);
    private void OnRelease()         => _anim.SetBool(IsClicking, false);
    private void OnTargetAcquired()  => _anim.SetBool(IsInteracting, true);
    private void OnTargetLost()      => _anim.SetBool(IsInteracting, false);

    public void PlayEmote(string emoteId)
    {
        Debug.Log($"[CursorAnimator] PlayEmote: '{emoteId}', anim={_anim != null}, go.active={gameObject.activeInHierarchy}");
        switch (emoteId)
        {
            case "ToSurprise": _anim.Play(ToSurprise); break;
            case "Frown":    _anim.Play(Frown);    break;
            case "Smile":    _anim.Play(ToSmile);    break;
            case "ResetToArrow":     _anim.Play(ResetToArrow); break;
            default: Debug.LogWarning($"[CursorAnimator] Unknown emote: '{emoteId}'"); break;
        }
    }

    public void FaceTowards(Vector2 worldPosition)
    {
        _cursor.flipX = worldPosition.x < _cursor.transform.position.x;
    }

    #region Cached Properties

    private static readonly int Arrow       = Animator.StringToHash("Idle");
    private static readonly int ResetToArrow = Animator.StringToHash("ResetToArrow");
    private static readonly int IsClicking  = Animator.StringToHash("IsClicking");
    private static readonly int IsInteracting = Animator.StringToHash("IsInteracting");

    private static readonly int Smile      = Animator.StringToHash("Smile");
    private static readonly int ToSmile    = Animator.StringToHash("ToSmile");
    private static readonly int Surprise   = Animator.StringToHash("Surprise");
    private static readonly int ToSurprise = Animator.StringToHash("ToSurprise");
    private static readonly int Frown      = Animator.StringToHash("Frown");
    private static readonly int ToFrown    = Animator.StringToHash("toFrown");
    private static readonly int Talk       = Animator.StringToHash("Talk");

    #endregion
}
