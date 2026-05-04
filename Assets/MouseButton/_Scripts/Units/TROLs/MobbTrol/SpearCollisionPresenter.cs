using UnityEngine;

namespace TarodevController.Trol
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public class SpearCollisionPresenter : MonoBehaviour
    {
        [SerializeField] private MobbTrolController _controller;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private PlatformEffector2D _spearEffector;

        [SerializeField] private int _spearColOffsetY;

        private PolygonCollider2D _spearCollider;

        private void Awake()
        {
            _spearCollider = GetComponent<PolygonCollider2D>();

            if (_controller == null)
                _controller = GetComponentInParent<MobbTrolController>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInParent<SpriteRenderer>();

            if (_spearEffector == null)
                _spearEffector = GetComponent<PlatformEffector2D>();
        }

        private void Update()
        {
            if (_controller == null || _spriteRenderer == null || _spearCollider == null) return;

            float offsetXHalf = _controller.IsAiming ? -1f : 4.5f;
            float newOffsetX = _spriteRenderer.flipX ? 4.5f + offsetXHalf : 4.5f - offsetXHalf;
            _spearCollider.offset = new Vector2(newOffsetX, _spearColOffsetY);
            _spearCollider.enabled = !_controller.Spearless;

            if (_spearEffector != null)
                _spearEffector.rotationalOffset = _controller.IsAiming
                    ? (_spriteRenderer.flipX ? 45 : -45)
                    : 0;
        }
    }
}
