using TarodevController.Trol;
using UnityEngine;

namespace TarodevController.Trol
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpritePresenter : MonoBehaviour
    {
        [SerializeField] private MobbTrolController _controller;

        private SpriteRenderer _renderer;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        private void Update()
        {
            UpdateFlip();
        }

        private void UpdateFlip()
        {
            if (_controller.IsAiming && _controller._dest?.target != null)
                _renderer.flipX = _controller._dest.target.position.x < _controller._rb.position.x;
            if (_controller.Input.x != 0)
                _renderer.flipX = _controller.Input.x < 0;
        }
    }
}
