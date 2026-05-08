using UnityEngine;

public class BackgroundObject : MonoBehaviour
{
    [Range(0f, 1f)] public float parallaxFactor = 0.3f;

    Vector2 _lastCamPos;

    void Start()
    {
        if (Camera.main != null)
            _lastCamPos = Camera.main.transform.position;
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;
        var camPos = (Vector2)Camera.main.transform.position;
        transform.position += (Vector3)((camPos - _lastCamPos) * parallaxFactor);
        _lastCamPos = camPos;
    }
}
