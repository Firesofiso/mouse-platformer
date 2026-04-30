using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreInitialOverlaps : MonoBehaviour
{
    private Collider2D _col;
    readonly List<Collider2D> _ignoring = new();

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (_col != null) _col.enabled = false;
    }

    private void Start()
    {
        if (_col == null) return;
        _col.enabled = true;
        var buffer = new Collider2D[16];
        int count = Physics2D.OverlapCollider(_col, new ContactFilter2D().NoFilter(), buffer);
        for (int i = 0; i < count; i++)
            StartIgnoring(buffer[i]);
    }

    private void StartIgnoring(Collider2D other)
    {
        if (_ignoring.Contains(other)) return;
        _ignoring.Add(other);
        Physics2D.IgnoreCollision(other, _col);
        StartCoroutine(ReenableWhenClear(other));
    }

    private IEnumerator ReenableWhenClear(Collider2D other)
    {
        var buffer = new Collider2D[16];
        var filter = new ContactFilter2D().NoFilter();
        bool overlapping = true;
        while (other != null && overlapping)
        {
            yield return null;
            overlapping = false;
            int count = Physics2D.OverlapCollider(_col, filter, buffer);
            for (int i = 0; i < count; i++)
                if (buffer[i] == other) { overlapping = true; break; }
        }
        if (other != null)
            Physics2D.IgnoreCollision(other, _col, false);
        _ignoring.Remove(other);
    }
}
