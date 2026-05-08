using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BackgroundLayerCamera : MonoBehaviour
{
    public RenderTexture target;
    [Range(0f, 1f)] public float parallaxFactor = 0.5f;

    Camera _cam;
    Camera _main;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0, 0, 0, 0);
        _cam.orthographic = true;
        _cam.targetTexture = target;
        _cam.depth = -10;
    }

    void Start()
    {
        _main = Camera.main;
        if (_main != null)
        {
            _cam.orthographicSize = _main.orthographicSize;
            _cam.aspect = _main.aspect;
            SyncPosition();
        }
    }

    void LateUpdate()
    {
        if (_main == null) _main = Camera.main;
        if (_main != null) SyncPosition();
    }

    void SyncPosition()
    {
        _cam.orthographicSize = _main.orthographicSize;
        _cam.aspect = _main.aspect;
        var mp = _main.transform.position;
        transform.position = new Vector3(mp.x, mp.y, -100f);
    }
}
