using UnityEngine;
using TarodevController;

public class TopPlatformBehavior : MonoBehaviour
{
    private PlayerObject Player;

    private Collider2D _collider;

    private void Start()
    {
        _collider = GetComponent<Collider2D>();
        Player = PlayerObject.Instance;
    }

    private void Update()
    {
        Collider2D _player = Player.GetComponent<CapsuleCollider2D>();
        Collider2D _frame = GetComponent<BoxCollider2D>();

        float _playerFeet = _player.bounds.min.y;
        float _frameTop = _frame.bounds.max.y;
        float _frameBottom = _frame.bounds.min.y;

        if (Player.GetComponent<PlayerController>().IsOnWall || _playerFeet < _frameBottom) {
            // disable collider when player is on wall
            // or when player is below platform
            _collider.isTrigger = true;
        } else if (_playerFeet >= _frameTop) {
            // enable collider when above platform
            _collider.isTrigger = false;
        }

        if (GetComponent<ClickableElement>().isBeingClicked) {
            _collider.isTrigger = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        
    }
}
