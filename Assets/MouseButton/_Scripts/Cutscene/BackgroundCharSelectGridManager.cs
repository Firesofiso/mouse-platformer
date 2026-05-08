using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundCharSelectGridManager : MonoBehaviour
{
    [SerializeField] GameObject _prefab;
    [SerializeField] Vector2 _cellSize = new Vector2(32f, 28f);
    [SerializeField] Vector2 _gutter = Vector2.zero;
    private Vector2 Stride => _cellSize + _gutter;

    private readonly Dictionary<(int, int), Coroutine> _activeSlots = new();

    void Update()
    {
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);
        for (int row = minRow; row <= maxRow; row++)
            for (int col = minCol; col <= maxCol; col++)
            {
                var key = (col, row);
                if (!_activeSlots.ContainsKey(key))
                    _activeSlots[key] = StartCoroutine(ManageSlot(col, row));
            }
    }

    private IEnumerator ManageSlot(int col, int row)
    {
        while (true)
        {
            var instance = Instantiate(_prefab, SlotPosition(col, row), Quaternion.identity, transform);
            yield return new WaitUntil(() => instance == null);
            if (!IsVisible(col, row)) break;
        }
        _activeSlots.Remove((col, row));
    }

    private bool IsVisible(int col, int row)
    {
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);
        return col >= minCol && col <= maxCol && row >= minRow && row <= maxRow;
    }

    private void GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow)
    {
        minCol = maxCol = minRow = maxRow = 0;
        if (Camera.main == null) return;
        var cam = Camera.main;
        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        var camPos = cam.transform.position;
        var origin = (Vector2)transform.position;
        minCol = Mathf.FloorToInt((camPos.x - w - origin.x) / Stride.x);
        maxCol = Mathf.CeilToInt((camPos.x + w - origin.x) / Stride.x);
        minRow = Mathf.FloorToInt((camPos.y - h - origin.y) / Stride.y);
        maxRow = Mathf.CeilToInt((camPos.y + h - origin.y) / Stride.y);
    }

    private Vector3 SlotPosition(int col, int row) =>
        transform.position + new Vector3(col * Stride.x, row * Stride.y, 0f);

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.15f);
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);
        for (int row = minRow; row <= maxRow; row++)
            for (int col = minCol; col <= maxCol; col++)
                Gizmos.DrawWireCube(SlotPosition(col, row), new Vector3(_cellSize.x, _cellSize.y, 0f));
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.05f);
        for (int row = minRow; row <= maxRow; row++)
            for (int col = minCol; col <= maxCol; col++)
                Gizmos.DrawWireCube(SlotPosition(col, row), new Vector3(Stride.x, Stride.y, 0f));
    }
}
