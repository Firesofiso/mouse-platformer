using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class OneWayPlatformBehaviour : MonoBehaviour
{
    private Collider2D _platform;
    readonly List<Collider2D> _passingThrough = new();

    private void Start()
    {
        _platform = GetComponent<CompositeCollider2D>() ?? (Collider2D)GetComponent<TilemapCollider2D>();
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
        var buffer = new Collider2D[16];
        var filter = new ContactFilter2D().NoFilter();
        bool overlapping = true;
        while (other != null && overlapping)
        {
            yield return null;
            overlapping = false;
            int count = Physics2D.OverlapCollider(_platform, filter, buffer);
            for (int i = 0; i < count; i++)
                if (buffer[i] == other) { overlapping = true; break; }
        }
        if (other != null)
            Physics2D.IgnoreCollision(other, _platform, false);
        _passingThrough.Remove(other);
    }
}
