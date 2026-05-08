using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformCharSelectGridManager : MonoBehaviour
{
    [SerializeField] GameObject _prefab;
    [SerializeField] int _columns = 30;
    [SerializeField] int _rows = 50;
    [SerializeField] Vector2 _cellSize = new Vector2(64f, 56f);
    [SerializeField] Vector2 _gutter = Vector2.zero;
    [SerializeField] Vector2 _gridOffset = new Vector2(0f, -4f);
    [SerializeField] float _maxRowOffsetX = 0f;
    [SerializeField] Vector2 _maxCellOffset = Vector2.zero;

    private Vector2 Stride => _cellSize + _gutter;

    private readonly Dictionary<(int, int), Coroutine> _activeSlots = new();
    private int _centerCol, _centerRow;

    void Start()
    {
        _centerCol = 0;
        _centerRow = 0;
    }

    void Update()
    {
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);
        for (int row = minRow; row <= maxRow; row++)
            for (int col = minCol; col <= maxCol; col++)
            {
                var key = (col, row);
                bool isCenter = col == _centerCol && row == _centerRow;
                if (!isCenter && !_activeSlots.ContainsKey(key))
                    _activeSlots[key] = StartCoroutine(ManageSlot(col, row));
            }
    }

    public void StartCenterSlot() =>
        _activeSlots[(0, 0)] = StartCoroutine(ManageSlot(0, 0));

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

    private Vector3 SlotPosition(int col, int row)
    {
        float rowX  = SeededRandom(row,                  _maxRowOffsetX);
        float cellX = SeededRandom(col * 10000 + row,    _maxCellOffset.x);
        float cellY = SeededRandom(col * 10000 + row + 50000, _maxCellOffset.y);
        return transform.position + new Vector3(col * Stride.x + rowX + cellX, row * Stride.y + cellY, 0f);
    }

    private static float SeededRandom(int seed, float range)
    {
        if (range == 0f) return 0f;
        var r = new System.Random(seed);
        return ((float)r.NextDouble() * 2f - 1f) * range;
    }

    void OnDrawGizmos()
    {
        var gizmoShift = new Vector3(_gridOffset.x, _gridOffset.y, 0f);
        for (int row = 0; row < _rows; row++)
            for (int col = 0; col < _columns; col++)
            {
                var pos = SlotPosition(col, row) + gizmoShift;
                bool isCenter = col == _columns / 2 && row == _rows / 2;
                Gizmos.color = isCenter ? new Color(1f, 1f, 0f, 0.3f) : new Color(1f, 1f, 1f, 0.15f);
                Gizmos.DrawCube(pos, new Vector3(_cellSize.x, _cellSize.y, 0f));
                Gizmos.color = isCenter ? new Color(1f, 1f, 0f, 0.8f) : new Color(1f, 1f, 1f, 0.5f);
                Gizmos.DrawWireCube(pos, new Vector3(Stride.x, Stride.y, 0f));
            }
    }
}
