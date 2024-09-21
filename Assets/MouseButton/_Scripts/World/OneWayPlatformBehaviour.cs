using System.Collections.Generic;
using TarodevController;
using UnityEngine;

public class OneWayPlatformBehaviour : MonoBehaviour
{
    private PlayerObject Player;
    private PlayerController _playerController;
    private Collider2D _playerCollider;

    [SerializeField]
    private Collider2D _platform;

    [SerializeField]
    private Collider2D _passThroughDetection;
    readonly List<Collider2D> _passingThrough = new();

    private void Start()
    {
        Player = PlayerObject.Instance;
        _playerCollider = Player.GetComponent<CapsuleCollider2D>();
        _playerController = Player.GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (_playerController.IsOnWall)
        {
            // disable collider when player is on wall
            Physics2D.IgnoreCollision(_playerCollider, _platform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (
            _passingThrough.Contains(collision.collider)
            && !Physics2D.IsTouching(_platform, collision.collider)
        )
        {
            Physics2D.IgnoreCollision(collision.collider, _platform, false);
            _passingThrough.Remove(collision.collider);
        }
    }

    public void AllowObjectPassThrough(Collider2D other)
    {
        Physics2D.IgnoreCollision(other, _platform);
        if (!_passingThrough.Contains(other))
        {
            _passingThrough.Add(other);
        }
    }
}
