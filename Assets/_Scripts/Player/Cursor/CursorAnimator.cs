using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorAnimator : MonoBehaviour
{
    [SerializeField] private CapsuleCollider2D _cursorCollider;
    private CursorController _cursor;
    [SerializeField] GameObject _visual;
    private SpriteRenderer _renderer;
    // private string currentEmote = {
    //     {1: "smiling"},
    //     {2: "surprised"},
    //     {3: "frowning"}
    // };

    private void Awake() {
        _cursor = GetComponentInParent<CursorController>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandleSpriteFlipping();
        // HandleAnimations();
    }

    private void HandleSpriteFlipping() {
        _renderer.flipX = _cursor.flipX;
        if (_cursor.flipX) {
            _visual.transform.localPosition = new Vector3(0.3f, 0, 0);
        } else {
            _visual.transform.localPosition = new Vector3(-0.3f, 0, 0);
        }
    }

    // private void HandleAnimations() {
    //     var state = GetState();
    //     if (state == _currentState) return;
        
    //     //_anim.Play(state, 0); //_anim.CrossFade(state, 0, 0);
    //     _currentState = state;

    //     int GetState() {
    //         if (currentEmote) {
    //             return currentEmote;
    //         }
    //         return Idle;
    //     }
    // }

    #region Cached Properties

        private int _currentState;

        private static readonly int Arrow = Animator.StringToHash("Idle");
        private static readonly int ResetToArrow = Animator.StringToHash("ResetToArrow");

        private static readonly int Smile = Animator.StringToHash("Smile");
        private static readonly int ToSmile = Animator.StringToHash("ToSmile");
        private static readonly int Surprise = Animator.StringToHash("Surprise");
        private static readonly int ToSurprise = Animator.StringToHash("ToSurprise");
        private static readonly int Frown = Animator.StringToHash("Frown");
        private static readonly int ToFrown = Animator.StringToHash("ToFrown");

        private static readonly int Talk = Animator.StringToHash("Talk");

        #endregion
}
