using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class DialogueBubble : MonoBehaviour
{
    [SerializeField] SpriteRenderer _bubble;
    [SerializeField] TextMesh _text;
    [SerializeField] int _textSortingOrderOffset = 1;
    [SerializeField] float _scaleDuration = 0.1f;
    [SerializeField] Vector2 _padding = new(0.2f, 0.15f);
    [SerializeField] Vector2 _offset = new(0f, 1f);
    [SerializeField] Vector2 _offsetFromSpeaker;
    [SerializeField] float _pixelsPerUnit = 16f;

    [Header("Pointer")]
    [SerializeField] Material _pointerMaterial;
    [SerializeField] float _pointerBaseHalfWidth = 3f;
    [SerializeField] float _pointerTipY = 0.5f;
    [SerializeField] float _leashDistance = 8f;

    Transform _speaker;
    DialogueStyleConfig.StyleEntry _styleEntry = new();
    Coroutine _scaleCoroutine;
    float _shakePhase;
    string _full;
    float _revealTimer;
    int _revealedChars;
    Vector2 _pinnedPos;
    Vector2 _currentBubblePos;

    MeshFilter _pointerFilter;
    MeshRenderer _pointerRenderer;
    Mesh _pointerMesh;

    public bool IsRevealing => _revealedChars < (_full?.Length ?? 0);

    void Awake()
    {
        if (_bubble == null) _bubble = GetComponentInChildren<SpriteRenderer>();
        if (_text == null) _text = GetComponentInChildren<TextMesh>();
        InitPointer();
        ApplyRendererSorting();
    }

    void OnValidate()
    {
        if (_bubble == null) _bubble = GetComponentInChildren<SpriteRenderer>();
        if (_text == null) _text = GetComponentInChildren<TextMesh>();
        ApplyRendererSorting();
    }

    void InitPointer()
    {
        var existing = transform.Find("Pointer");
        var go = existing != null ? existing.gameObject : new GameObject("Pointer");
        if (existing == null) go.transform.SetParent(transform, false);

        _pointerFilter = go.GetComponent<MeshFilter>() ?? go.AddComponent<MeshFilter>();
        _pointerRenderer = go.GetComponent<MeshRenderer>() ?? go.AddComponent<MeshRenderer>();

        if (_pointerMaterial != null)
            _pointerRenderer.sharedMaterial = _pointerMaterial;

        _pointerMesh = new Mesh { name = "BubblePointer" };
        _pointerMesh.vertices = new Vector3[3];
        _pointerMesh.triangles = new[] { 0, 1, 2 };
        _pointerFilter.mesh = _pointerMesh;
    }

    public void Bind(Transform speaker, Vector2 offsetOverride)
    {
        _speaker = speaker;
        _offsetFromSpeaker = offsetOverride;
    }

    public void Show(string text, DialogueStyleConfig.StyleEntry styleEntry)
    {
        gameObject.SetActive(true);
        float snap = 1f / _pixelsPerUnit;
        Vector2 rawPos = _speaker != null
            ? (Vector2)_speaker.position + _offsetFromSpeaker
            : (Vector2)transform.position;
        _pinnedPos = new Vector2(
            Mathf.Round(rawPos.x / snap) * snap,
            Mathf.Round(rawPos.y / snap) * snap);
        _currentBubblePos = _pinnedPos;
        _styleEntry = styleEntry;
        _full = text ?? "";
        _revealedChars = 0;
        _revealTimer = 0f;
        _text.text = "";

        if (styleEntry != null)
        {
            if (styleEntry.bubbleSprite != null) _bubble.sprite = styleEntry.bubbleSprite;
            _text.color = styleEntry.textColor;
            _text.transform.localScale = ToFontScale(Vector3.one);
        }

        ApplyRendererSorting();
        FitBubble();

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        transform.localScale = Vector3.zero;
        _scaleCoroutine = StartCoroutine(ScaleTo(Vector3.one));
    }

    public void CompleteReveal()
    {
        _revealedChars = _full.Length;
        _text.text = _full;
        FitBubble();
    }

    public void Hide()
    {
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScaleOutAndDestroy());
    }

    IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / _scaleDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.LerpUnclamped(start, target, ease);
            yield return null;
        }
    }

    IEnumerator ScaleOutAndDestroy()
    {
        yield return ScaleTo(Vector3.zero);
        Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) { FitBubble(); return; }

        if (_speaker != null && _bubble != null)
        {
            Vector3 boundsOffset = _bubble.bounds.center - transform.position;
            var currentBounds = new Bounds((Vector3)(Vector2)_currentBubblePos + boundsOffset, _bubble.bounds.size);
            Vector3 speakerPos3 = new(_speaker.position.x, _speaker.position.y, 0f);
            Vector2 toSpeaker = speakerPos3 - currentBounds.ClosestPoint(speakerPos3);
            float dist = toSpeaker.magnitude;
            if (dist > _leashDistance)
                _currentBubblePos += toSpeaker - toSpeaker.normalized * _leashDistance;
            _currentBubblePos.y = _speaker.position.y + _offsetFromSpeaker.y;
        }
        transform.position = _currentBubblePos;

        if (IsRevealing && _styleEntry != null && _styleEntry.charsPerSecond > 0f)
        {
            _revealTimer += Time.deltaTime * _styleEntry.charsPerSecond;
            int target = Mathf.Min(_full.Length, Mathf.FloorToInt(_revealTimer));
            if (target != _revealedChars)
            {
                _revealedChars = target;
                _text.text = _full.Substring(0, _revealedChars);
                FitBubble();
            }
        }

        if (_styleEntry != null && _styleEntry.shake)
        {
            _shakePhase += Time.deltaTime * 30f;
            var jitter = new Vector3(Mathf.Sin(_shakePhase * 1.7f), Mathf.Cos(_shakePhase * 2.3f), 0f)
                         * ToFontScale(_styleEntry.shakeAmplitude);
            _text.transform.localPosition = jitter;
        }

        if (_speaker != null && _bubble != null && _pointerMesh != null && transform.localScale.sqrMagnitude > 0.001f)
            UpdatePointer(_bubble.bounds, _speaker.position);
    }

    void UpdatePointer(Bounds b, Vector3 speakerWorldPos)
    {
        speakerWorldPos.z = 0f;

        // Base slides along the bottom edge, clamped so both points stay within [min.x, max.x].
        float anchorX = Mathf.Clamp(speakerWorldPos.x, b.min.x + _pointerBaseHalfWidth, b.max.x - _pointerBaseHalfWidth);
        Vector3 baseA = new(Mathf.Min(anchorX + _pointerBaseHalfWidth, b.max.x), b.min.y, 0f);
        Vector3 baseB = new(Mathf.Max(anchorX - _pointerBaseHalfWidth, b.min.x), b.min.y, 0f);
        Vector3 tip = new(
            Mathf.Clamp(speakerWorldPos.x, b.min.x, b.max.x),
            b.min.y - _pointerTipY,
            0f);

        var w2l = _pointerFilter.transform.worldToLocalMatrix;
        _pointerMesh.vertices = new[]
        {
            w2l.MultiplyPoint3x4(baseA),
            w2l.MultiplyPoint3x4(baseB),
            w2l.MultiplyPoint3x4(tip),
        };
        _pointerMesh.RecalculateNormals();
        _pointerMesh.RecalculateBounds();
    }

    void FitBubble()
    {
        if (_bubble == null || _text == null) return;
        var mr = _text.GetComponent<MeshRenderer>();
        if (mr == null) return;
        var size = mr.bounds.size;
        if (size.x < 0.001f) return;

        float snap = 1f / _pixelsPerUnit;
        float fitW = Mathf.Round((size.x + ToFontScale(_padding.x)) / snap) * snap;
        float fitH = Mathf.Round((size.y + ToFontScale(_padding.y)) / snap) * snap;

        // Center-left pivot: x = left edge of text, y = vertical center of text bounds
        var localMin = transform.InverseTransformPoint(mr.bounds.min);
        float localLeftX   = localMin.x;
        var bubblePos = new Vector3(
            Mathf.Round(localLeftX / snap) * snap + ToFontScale(_offset.x),
            Mathf.Round(localMin.y / snap) * snap + ToFontScale(_offset.y),
            0f);

        if (_bubble.sprite != null)
        {
            var native = _bubble.sprite.bounds.size;
            _bubble.transform.localScale = new Vector3(fitW / native.x, fitH / native.y, 1f);
            _bubble.transform.localPosition = bubblePos;
        }
        else
        {
            _bubble.size = new Vector2(fitW, fitH);
            _bubble.transform.localPosition = bubblePos;
        }
    }

    void ApplyRendererSorting()
    {
        if (_bubble == null || _text == null) return;

        var textRenderer = _text.GetComponent<MeshRenderer>();
        if (textRenderer == null) return;

        textRenderer.sortingLayerID = _bubble.sortingLayerID;
        textRenderer.sortingOrder = _bubble.sortingOrder + _textSortingOrderOffset;

        if (_pointerRenderer != null)
        {
            _pointerRenderer.sortingLayerID = _bubble.sortingLayerID;
            _pointerRenderer.sortingOrder = _bubble.sortingOrder;
        }
    }

    private float ToFontScale(float f)
    {
        return f * _styleEntry.fontScale;
    }

    private Vector3 ToFontScale(Vector3 f)
    {
        return f * _styleEntry.fontScale;
    }
}
