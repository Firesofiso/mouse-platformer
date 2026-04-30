using UnityEngine;

public class GuineaPigController : MonoBehaviour
{
    [SerializeField] GroundedPhysicsBody _body;
    [SerializeField] float _maxSpeed = 40f;
    [SerializeField] float _acceleration = 120f;
    [SerializeField] SpriteRenderer _renderer;
    [SerializeField] Animator _animator;

    float _currentSpeed;

    void FixedUpdate()
    {
        float target = _renderer.isVisible ? _maxSpeed : 0f;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, target, _acceleration * Time.fixedDeltaTime);
        _body.SpeedX = _currentSpeed;

        float runSpeed = _maxSpeed > 0f ? _currentSpeed / _maxSpeed : 0f;
        _animator.SetFloat("RunSpeed", Mathf.Max(runSpeed, 0.1f));
    }
}
