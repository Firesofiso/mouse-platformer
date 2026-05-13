using UnityEngine;
using UnityEngine.Rendering;

public class PixelDissolveController : MonoBehaviour
{
    public static PixelDissolveController Instance { get; private set; }

    [SerializeField] Material _material;
    [SerializeField] int      _refWidth    = 320;
    [SerializeField] int      _refHeight   = 180;
    [SerializeField] string   _sortingLayer = "UI";
    [SerializeField] int      _sortingOrder = 9999;

    MeshRenderer         _quad;
    MeshFilter           _filter;
    MaterialPropertyBlock _block;

    float         _threshold;
    float         _fadeTarget;
    float         _fadeSpeed;
    System.Action _fadeCallback;
    bool          _fading;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _block = new MaterialPropertyBlock();
        BuildQuad();
        _threshold = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void FadeOut(float duration, System.Action onComplete = null)
    {
        _threshold    = 0f;
        _fadeTarget   = 1f;
        _fadeSpeed    = duration > 0f ? 1f / duration : float.MaxValue;
        _fadeCallback = onComplete;
        _fading       = true;
    }

    public void FadeIn(float duration, System.Action onComplete = null)
    {
        _threshold    = 1f;
        _fadeTarget   = 0f;
        _fadeSpeed    = duration > 0f ? 1f / duration : float.MaxValue;
        _fadeCallback = onComplete;
        _fading       = true;
    }

    void LateUpdate()
    {
        if (_fading)
        {
            _threshold = Mathf.MoveTowards(_threshold, _fadeTarget, _fadeSpeed * Time.unscaledDeltaTime);
            if (Mathf.Approximately(_threshold, _fadeTarget))
            {
                _threshold = _fadeTarget;
                _fading    = false;
                System.Action cb = _fadeCallback;
                _fadeCallback = null;
                if (cb != null) cb();
            }
        }

        UpdateQuad();
    }

    void UpdateQuad()
    {
        Camera cam = Camera.main;
        if (cam == null || _quad == null) return;

        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        Vector3 camPos = cam.transform.position;

        _quad.transform.position = new Vector3(camPos.x, camPos.y, camPos.z + 50f);

        _filter.mesh.vertices = new Vector3[]
        {
            new Vector3(-w, -h, 0),
            new Vector3( w, -h, 0),
            new Vector3(-w,  h, 0),
            new Vector3( w,  h, 0),
        };

        _block.SetFloat("_Threshold", _threshold);
        _block.SetFloat("_RefWidth",  (float)_refWidth);
        _block.SetFloat("_RefHeight", (float)_refHeight);
        _quad.SetPropertyBlock(_block);
    }

    void BuildQuad()
    {
        Camera cam = Camera.main;
        float h = cam != null ? cam.orthographicSize : 90f;
        float w = cam != null ? h * cam.aspect       : 160f;

        var go = new GameObject("PixelDissolveQuad");
        go.transform.SetParent(transform, false);

        _filter = go.AddComponent<MeshFilter>();

        var mesh = new Mesh { name = "DissolveQuad" };
        mesh.vertices = new Vector3[]
        {
            new Vector3(-w, -h, 0),
            new Vector3( w, -h, 0),
            new Vector3(-w,  h, 0),
            new Vector3( w,  h, 0),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        _filter.mesh = mesh;

        _quad = go.AddComponent<MeshRenderer>();
        _quad.sharedMaterial    = _material;
        _quad.sortingLayerName  = _sortingLayer;
        _quad.sortingOrder      = _sortingOrder;
        _quad.shadowCastingMode = ShadowCastingMode.Off;
        _quad.receiveShadows    = false;
    }
}
