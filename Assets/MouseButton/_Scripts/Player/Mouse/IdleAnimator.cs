using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IdleAnimator : MonoBehaviour
{
    private Animator _anim;
    private int _currentState;
    private float _idleStart;
    private int _wagInterval;
    private int _scritchInterval;

    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int IdleWag = Animator.StringToHash("IdleWag");
    private static readonly int IdleScritch = Animator.StringToHash("IdleScritch");
    private static readonly int Fall = Animator.StringToHash("Fall");

    void Awake() => _anim = GetComponent<Animator>();

    public void PlayFall()
    {
        enabled = false;
        _anim.Play(Fall, 0);
    }

    void Update()
    {
        var state = IdleCycle();
        if (state == _currentState) return;
        _anim.Play(state, 0);
        _currentState = state;
    }

    private int IdleCycle()
    {
        if (_idleStart == 0)
        {
            _idleStart = Time.time;
            _wagInterval = Random.Range(3, 5);
            _scritchInterval = Random.Range(10, 20);
        }
        else if (_idleStart + _scritchInterval < Time.time || _idleStart > Time.time)
        {
            if (_idleStart + _scritchInterval < Time.time)
            {
                _idleStart = Time.time + 1;
                _scritchInterval += Random.Range(10, 20);
                _wagInterval = 0;
            }
            return IdleScritch;
        }
        else if (_idleStart + _wagInterval < Time.time)
        {
            return IdleWag;
        }
        else
        {
            return Idle;
        }
        return _currentState;
    }
}
