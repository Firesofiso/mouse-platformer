using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundCharSelectGridManager : MonoBehaviour
{
    [SerializeField] GameObject _prefab;
    [SerializeField] string _layer = ""; // leave empty to use prefab's layer
    [SerializeField] float _scale = 1f;
    [SerializeField] float _maxRowOffsetX = 0f;
    [SerializeField] Vector2 _maxCellOffset = Vector2.zero;
    [SerializeField] Vector2 _cellSize = new Vector2(32f, 28f);
    [SerializeField] Vector2 _gutter = Vector2.zero;
    [SerializeField] float _maxDistance = 0f; // 0 = unlimited
    private Vector2 Stride => _cellSize + _gutter;

    private readonly Dictionary<(int, int), Coroutine> _activeSlots = new();

    void Update()
    {
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);
        for (int row = minRow; row <= maxRow; row++)
            for (int col = minCol; col <= maxCol; col++)
            {
                var key = (col, row);
                if (!_activeSlots.ContainsKey(key) && IsWithinDistance(col, row))
                    _activeSlots[key] = StartCoroutine(ManageSlot(col, row));
            }
    }

    private IEnumerator ManageSlot(int col, int row)
    {
        while (true)
        {
            var instance = Instantiate(_prefab, SlotPosition(col, row), Quaternion.identity, transform);
            if (!string.IsNullOrEmpty(_layer))
                SetLayerRecursive(instance, LayerMask.NameToLayer(_layer));
            if (_scale != 1f)
                instance.transform.localScale = _prefab.transform.localScale * _scale;
            yield return new WaitUntil(() => instance == null || !IsVisible(col, row));
            if (instance != null) Destroy(instance);
            if (!IsVisible(col, row)) break;
        }
        _activeSlots.Remove((col, row));
    }

    private bool IsVisible(int col, int row)
    {
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);
        if (col < minCol || col > maxCol || row < minRow || row > maxRow) return false;
        return IsWithinDistance(col, row);
    }

    private bool IsWithinDistance(int col, int row)
    {
        if (_maxDistance <= 0f || Camera.main == null) return true;
        var slotPos = (Vector2)SlotPosition(col, row);
        var camPos  = (Vector2)Camera.main.transform.position;
        return Vector2.Distance(slotPos, camPos) <= _maxDistance;
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

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private Vector3 SlotPosition(int col, int row)
    {
        float rowX   = SeededRandom(row,                  _maxRowOffsetX);
        float cellX  = SeededRandom(col * 10000 + row,    _maxCellOffset.x);
        float cellY  = SeededRandom(col * 10000 + row + 50000, _maxCellOffset.y);
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
