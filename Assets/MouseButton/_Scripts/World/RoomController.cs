using UnityEngine;

[RequireComponent(typeof(CameraRoom))]
public class RoomController : MonoBehaviour
{
    private CameraRoom _room;

    private void Awake()
    {
        _room = GetComponent<CameraRoom>();
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
        // TODO: re-initialize room state (reset enemies, pickups, etc.)
    }

    private void OnExit(CameraRoom to)
    {
        // TODO: cleanup, disable active effects, etc.
    }
}
