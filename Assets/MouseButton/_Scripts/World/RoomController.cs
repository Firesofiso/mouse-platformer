using UnityEngine;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(CameraRoom))]
public class RoomController : MonoBehaviour
{
    private CameraRoom _room;
    private Light2D[] _lights;
    private ShadowCaster2D[] _shadowCasters;

    private bool _active;

    private void Awake()
    {
        _room = GetComponent<CameraRoom>();
        CacheComponents();
        SetLightingActive(false);
    }

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        CacheComponents();
        SetLightingActive(_active);
    }

    private void CacheComponents()
    {
        _lights = GetComponentsInChildren<Light2D>(true);
        _shadowCasters = GetComponentsInChildren<ShadowCaster2D>(true);
    }

    private void OnEnable()
    {
        CameraController.RoomChanged += HandleRoomChanged;
    }

    private void OnDisable()
    {
        CameraController.RoomChanged -= HandleRoomChanged;
    }

    private void HandleRoomChanged(CameraRoom previous, CameraRoom next)
    {
        if (next == _room)
            OnEnter(previous);
        else if (previous == _room)
            OnExit(next);
    }

    private void OnEnter(CameraRoom from)
    {
        _active = true;
        SetLightingActive(true);
    }

    private void OnExit(CameraRoom to)
    {
        _active = false;
        SetLightingActive(false);
    }

    private void SetLightingActive(bool active)
    {
        foreach (var light in _lights)
            if (light != null) light.enabled = active;
        foreach (var caster in _shadowCasters)
            if (caster != null) caster.enabled = active;
    }
}
