using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public static event Action<CameraRoom, CameraRoom> RoomChanged;

    public CameraRoom startRoom;

    public float panSpeed = 600f;
    public float minPanDuration = 0.2f;
    public float maxPanDuration = 0.8f;

    private CameraRoom _currentRoom;
    private bool _isPanning;
    private Vector3 _panTarget;
    private float _resolvedPanSpeed;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (startRoom != null)
            SnapTo(startRoom);
    }

    public CameraRoom CurrentRoom => _currentRoom;
    public bool IsPanning => _isPanning;

    public void PanTo(CameraRoom room)
    {
        Debug.Log("panning!!!");
        if (_isPanning || room == _currentRoom) return;

        var previous = _currentRoom;
        _currentRoom = room;
        RoomChanged?.Invoke(previous, room);
        _panTarget = new Vector3(
            Mathf.Round(room.transform.position.x),
            Mathf.Round(room.transform.position.y),
            transform.position.z);

        float distance = Vector2.Distance(transform.position, _panTarget);
        float rawDuration = distance / panSpeed;
        float clampedDuration = Mathf.Clamp(rawDuration, minPanDuration, maxPanDuration);
        _resolvedPanSpeed = distance / clampedDuration;

        _isPanning = true;
        Time.timeScale = 0f;
    }

    public void SnapTo(CameraRoom room)
    {
        var previous = _currentRoom;
        _currentRoom = room;
        RoomChanged?.Invoke(previous, room);
        transform.position = new Vector3(
            Mathf.Round(room.transform.position.x),
            Mathf.Round(room.transform.position.y),
            transform.position.z);
    }

    private void Update()
    {
        if (!_isPanning) return;
        Debug.Log("is panning");

        transform.position = Vector3.MoveTowards(transform.position, _panTarget, _resolvedPanSpeed * Time.unscaledDeltaTime);

        if (Vector3.Distance(transform.position, _panTarget) < 0.01f)
        {
            transform.position = _panTarget;
            _isPanning = false;
            Time.timeScale = 1f;
        }
    }
}
