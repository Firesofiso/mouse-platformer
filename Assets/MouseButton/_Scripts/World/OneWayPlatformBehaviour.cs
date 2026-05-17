using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlatformEffector2D))]
public class OneWayPlatformBehaviour : MonoBehaviour
{
    private Collider2D _platform;
    readonly List<Collider2D> _passingThrough = new();

#if UNITY_EDITOR
    private void Reset()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        var tilemap = GetComponent<TilemapCollider2D>();
        if (tilemap != null)
        {
            tilemap.usedByComposite = true;
            tilemap.usedByEffector = true;
        }

        var composite = GetComponent<CompositeCollider2D>();
        if (composite != null)
        {
            composite.usedByEffector = true;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
        }

        var effector = GetComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 90f;
    }
#endif

    private void Start()
    {
        Collider2D c = GetComponent<CompositeCollider2D>();
        if (c == null) c = GetComponent<TilemapCollider2D>();
        if (c == null) c = GetComponent<BoxCollider2D>();
        if (c == null) c = GetComponent<PolygonCollider2D>();
        _platform = c;
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
        while (other != null && other.bounds.max.y > _platform.bounds.min.y)
            yield return null;
        if (other != null)
            Physics2D.IgnoreCollision(other, _platform, false);
        _passingThrough.Remove(other);
    }
}
