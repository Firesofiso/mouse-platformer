using UnityEngine;
using TarodevController;

public class TopPlatformBehavior : MonoBehaviour
{
    private PlayerObject Player;

    private Collider2D _collider;
    Collider2D _player;
    Collider2D _frame;
    float _playerFeet;
    float frameTop;
    float frameBottom;

    private void Start()
    {
        _collider = GetComponent<Collider2D>();
        Player = PlayerObject.Instance;
        _player = Player.GetComponent<CapsuleCollider2D>();
        _frame = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        _playerFeet = _player.bounds.min.y;
        frameTop = _frame.bounds.max.y;
        frameBottom = _frame.bounds.min.y;
        if (GetComponent<ClickableElement>().isBeingClicked) {
            Physics2D.IgnoreCollision(_player, _collider);
        } else if (Player.GetComponent<PlayerController>().IsOnWall || _playerFeet < frameBottom) {
            // disable collider when player is on wall
            // or when player is below platform
            Physics2D.IgnoreCollision(_player, _collider);
        } else if (_playerFeet >= frameTop) {
            // enable collider when above platform
            Physics2D.IgnoreCollision(_player, _collider, false);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        float otherBottom = other.collider.bounds.min.y;
        frameTop = _frame.bounds.max.y;
        frameBottom = _frame.bounds.min.y;
        if (otherBottom < frameBottom) {
            // enable collider when above platform  
            Physics2D.IgnoreCollision(other.collider, _collider);
        } else if (otherBottom >= frameTop) {
            // enable collider when above platform
            Physics2D.IgnoreCollision(other.collider, _collider, false);
        }
    }
}
