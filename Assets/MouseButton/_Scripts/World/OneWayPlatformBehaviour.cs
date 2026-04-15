using System.Collections;
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

        // DEBUG: log all current overlapping colliders
        var results = new Collider2D[10];
        int count = Physics2D.OverlapCollider(_platform, new ContactFilter2D().NoFilter(), results);
    }

    public void AllowObjectPassThrough(Collider2D other)
    {
        if (!_passingThrough.Contains(other))
        {
            _passingThrough.Add(other);
            StartCoroutine(ReenableCollisionAfterFall(other));
        }

        Physics2D.IgnoreCollision(other, _platform);
    }

    private IEnumerator ReenableCollisionAfterFall(Collider2D other)
    {
        // Wait until player is below platform
        while (other.bounds.max.y > _platform.bounds.min.y)
        {
            yield return null;
        }

        Physics2D.IgnoreCollision(other, _platform, false);
        _passingThrough.Remove(other);
    }
}
