using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] float _fadeDuration = 0.4f;
    private Vector2 Stride => _cellSize + _gutter;

    [SerializeField] bool _showEditPreview = false;
    [SerializeField] int _updateEveryNFrames = 4;

    private readonly Dictionary<(int, int), Coroutine> _activeSlots = new();
    private readonly Queue<GameObject> _pool = new();
    private Camera _mainCam;
    private int _rangeFrame = -1;
    private int _cMinCol, _cMaxCol, _cMinRow, _cMaxRow;

#if UNITY_EDITOR
    private readonly Dictionary<(int, int), GameObject> _editInstances = new();
#endif

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { EditModeUpdate(); return; }
#endif
        if (_updateEveryNFrames > 1 && Time.frameCount % _updateEveryNFrames != 0) return;
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);
        for (int row = minRow; row <= maxRow; row++)
            for (int col = minCol; col <= maxCol; col++)
            {
                var key = (col, row);
                if (!_activeSlots.ContainsKey(key) && IsWithinDistance(col, row))
                    _activeSlots[key] = StartCoroutine(ManageSlot(col, row));
            }
    }

#if UNITY_EDITOR
    void EditModeUpdate()
    {
        if (!_showEditPreview || _prefab == null) { ClearEditInstances(); return; }
        GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow);

        var toRemove = new List<(int, int)>();
        foreach (var kv in _editInstances)
        {
            var (c, r) = kv.Key;
            if (c < minCol || c > maxCol || r < minRow || r > maxRow || !IsWithinDistance(c, r))
            {
                if (kv.Value != null) DestroyImmediate(kv.Value);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var k in toRemove) _editInstances.Remove(k);

        for (int row = minRow; row <= maxRow; row++)
            for (int col = minCol; col <= maxCol; col++)
            {
                var key = (col, row);
                if (!_editInstances.ContainsKey(key) && IsWithinDistance(col, row))
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(_prefab, transform);
                    inst.transform.position = SlotPosition(col, row);
                    if (_scale != 1f) inst.transform.localScale = _prefab.transform.localScale * _scale;
                    if (!string.IsNullOrEmpty(_layer)) SetLayerRecursive(inst, LayerMask.NameToLayer(_layer));
                    inst.hideFlags = HideFlags.DontSave;
                    _editInstances[key] = inst;
                }
            }
    }

    void OnDisable() => ClearEditInstances();

    void ClearEditInstances()
    {
        foreach (var kv in _editInstances)
            if (kv.Value != null) DestroyImmediate(kv.Value);
        _editInstances.Clear();
    }
#endif

    private GameObject SpawnFromPool(Vector3 pos)
    {
        GameObject go;
        if (_pool.Count > 0)
        {
            go = _pool.Dequeue();
            if (go == null) return SpawnFromPool(pos);
            go.transform.position = pos;
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(_prefab, pos, Quaternion.identity, transform);
            go.name = _prefab.name;
        }
        return go;
    }

    private void ReturnToPool(GameObject go)
    {
        if (go == null) return;
        var seq = go.GetComponentInChildren<BackgroundCharSelectSequence>(true);
        if (seq != null) seq.ResetForReuse();
        go.SetActive(false);
        _pool.Enqueue(go);
    }

    private IEnumerator ManageSlot(int col, int row)
    {
        bool firstSpawn = true;
        while (true)
        {
            var instance = SpawnFromPool(SlotPosition(col, row));
            if (!string.IsNullOrEmpty(_layer))
                SetLayerRecursive(instance, LayerMask.NameToLayer(_layer));
            if (_scale != 1f)
                instance.transform.localScale = _prefab.transform.localScale * _scale;
            var fadeables = instance.GetComponentsInChildren<Fadeable>();
            if (firstSpawn)
            {
                foreach (var f in fadeables) f.SetAlpha(1f);
            }
            else
            {
                foreach (var f in fadeables)
                {
                    f.SetAlpha(0f);
                    StartCoroutine(f.FadeTo(1f, _fadeDuration));
                }
            }
            var seq = instance.GetComponentInChildren<BackgroundCharSelectSequence>();
            if (seq != null)
            {
                if (firstSpawn) seq.StartAtRandomProgress();
                else seq.StartFresh();
            }
            firstSpawn = false;

            while (instance != null
                   && (seq == null || !seq.IsComplete)
                   && IsVisible(col, row))
                yield return null;

            bool scrolledOff = !IsVisible(col, row);
            if (scrolledOff)
            {
                if (seq != null) seq.StopActiveSequence();
                var fadeable = instance.GetComponentInChildren<Fadeable>();
                if (fadeable != null)
                    StartCoroutine(FadeOutAndPool(fadeable, instance, _fadeDuration));
                else
                    ReturnToPool(instance);
                break;
            }
            else
            {
                ReturnToPool(instance);
            }
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
        if (_maxDistance <= 0f) return true;
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam == null) return true;
        var slotPos = (Vector2)SlotPosition(col, row);
        var camPos  = (Vector2)_mainCam.transform.position;
        return Vector2.Distance(slotPos, camPos) <= _maxDistance;
    }

    private void GetVisibleRange(out int minCol, out int maxCol, out int minRow, out int maxRow)
    {
        if (_rangeFrame == Time.frameCount)
        {
            minCol = _cMinCol; maxCol = _cMaxCol; minRow = _cMinRow; maxRow = _cMaxRow;
            return;
        }
        if (_mainCam == null) _mainCam = Camera.main;
        if (_mainCam == null) { minCol = maxCol = minRow = maxRow = 0; return; }
        float h = _mainCam.orthographicSize;
        float w = h * _mainCam.aspect;
        var camPos = _mainCam.transform.position;
        var origin = (Vector2)transform.position;
        minCol = Mathf.FloorToInt((camPos.x - w - origin.x) / Stride.x);
        maxCol = Mathf.CeilToInt((camPos.x + w - origin.x) / Stride.x);
        minRow = Mathf.FloorToInt((camPos.y - h - origin.y) / Stride.y);
        maxRow = Mathf.CeilToInt((camPos.y + h - origin.y) / Stride.y);
        _rangeFrame = Time.frameCount;
        _cMinCol = minCol; _cMaxCol = maxCol; _cMinRow = minRow; _cMaxRow = maxRow;
    }

    private IEnumerator FadeOutAndPool(Fadeable fadeable, GameObject go, float dur)
    {
        yield return StartCoroutine(fadeable.FadeTo(0f, dur));
        ReturnToPool(go);
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

}
