using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class WorldButton : MonoBehaviour
{
    [SerializeField] Tilemap _tilemap;
    [SerializeField] TileBase _default;
    [SerializeField] TileBase _pressed;
    [SerializeField] Transform _cursorTarget;
    [SerializeField] float _hoverBrightness = 1.3f;
    [SerializeField] float _pressedOffset = 1f;
    [SerializeField] Color _tint = Color.white;

    public Vector3 CursorTarget
    {
        get
        {
            if (_cursorTarget != null) return _cursorTarget.position;
            var half = _tilemap.cellSize / 2f;
            return transform.position + new Vector3(half.x, half.y, 0f);
        }
    }

    public UnityEvent OnClick;

    private TilemapRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private bool _hovered;
    private Transform[] _children;
    private Vector3[] _childRestPositions;
    private Coroutine _pressRoutine;

    void OnValidate()
    {
        if (_tilemap == null) return;
        var r = _tilemap.GetComponent<TilemapRenderer>();
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor(ColorId, _tint);
        r.SetPropertyBlock(mpb);
    }

    void Awake()
    {
        _renderer = _tilemap.GetComponent<TilemapRenderer>();
        _mpb = new MaterialPropertyBlock();
        _mpb.SetColor(ColorId, _tint);
        _renderer.SetPropertyBlock(_mpb);

        _children = new Transform[transform.childCount];
        _childRestPositions = new Vector3[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            _children[i] = transform.GetChild(i);
            _childRestPositions[i] = _children[i].localPosition;
        }
    }

    void OnEnable()
    {
        CursorController.OnClick += HandleClick;
        CursorController.OnRelease += HandleRelease;
    }

    void OnDisable()
    {
        CursorController.OnClick -= HandleClick;
        CursorController.OnRelease -= HandleRelease;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Cursor")) return;
        _hovered = true;
        SetBrightness(_hoverBrightness);
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (!col.CompareTag("Cursor")) return;
        _hovered = false;
        SetBrightness(1f);
        _tilemap.SwapTile(_pressed, _default);
        ShiftChildren(0f);
    }

    public void SimulatePress()
    {
        if (_pressRoutine != null) StopCoroutine(_pressRoutine);
        _tilemap.SwapTile(_pressed, _default);
        ShiftChildren(0f);
        _pressRoutine = StartCoroutine(PressAnimation());
    }

    private IEnumerator PressAnimation()
    {
        _tilemap.SwapTile(_default, _pressed);
        ShiftChildren(-_pressedOffset);
        yield return new WaitForSeconds(0.12f);
        _tilemap.SwapTile(_pressed, _default);
        ShiftChildren(0f);
        _pressRoutine = null;
    }

    private void HandleClick()
    {
        if (!_hovered) return;
        _tilemap.SwapTile(_default, _pressed);
        ShiftChildren(-_pressedOffset);
    }

    private void HandleRelease()
    {
        if (!_hovered) return;
        _tilemap.SwapTile(_pressed, _default);
        ShiftChildren(0f);
        OnClick.Invoke();
    }

    private void ShiftChildren(float yOffset)
    {
        for (int i = 0; i < _children.Length; i++)
            _children[i].localPosition = _childRestPositions[i] + new Vector3(0f, yOffset, 0f);
    }

    private void SetBrightness(float value)
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(BrightnessId, value);
        _renderer.SetPropertyBlock(_mpb);
    }
}
