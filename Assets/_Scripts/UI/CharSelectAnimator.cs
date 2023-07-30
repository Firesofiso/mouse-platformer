using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharSelectAnimator : MonoBehaviour
{
    [SerializeField] Animator _anim;
    private float _isIdle = 0;
    private int _wagInterval;
    private int _scritchInterval;
    // Start is called before the first frame update
    void Start()
    {
        _anim.Play("Idle");
    }

    // Update is called once per frame
    void Update()
    {
        if (_isIdle == 0) {
            _isIdle = Time.time;
            _wagInterval = Random.Range(3, 5);
            _scritchInterval = Random.Range(10, 20);
        } else if (_isIdle + _scritchInterval < Time.time || _isIdle > Time.time) {
            if (_isIdle + _scritchInterval < Time.time) {
                _isIdle = Time.time + 1;
                _scritchInterval += Random.Range(10, 20);
                _wagInterval = 0;
            }
            _anim.Play("IdleScritch");
        } else if (_isIdle + _wagInterval < Time.time) {
            _anim.Play("IdleWag");
        } else {
            _anim.Play("Idle");
        }
    }
}
