using System.Collections.Generic;
using UnityEngine;

public class RoomGraph : MonoBehaviour
{
    public static RoomGraph Instance { get; private set; }

    private Dictionary<CameraRoom, HashSet<CameraRoom>> _adjacency = new();
    private CameraRoom[] _allRooms;

    private void Awake()
    {
        Instance = this;
        BuildGraph();
    }

    private void OnEnable()
    {
        CameraController.RoomChanged += OnRoomChanged;
    }

    private void OnDisable()
    {
        CameraController.RoomChanged -= OnRoomChanged;
    }

    private void BuildGraph()
    {
        _allRooms = FindObjectsOfType<CameraRoom>(true);

        foreach (var room in _allRooms)
            _adjacency[room] = new HashSet<CameraRoom>();

        var transitions = FindObjectsOfType<CameraRoomTransition>(true);
        foreach (var t in transitions)
        {
            if (t.destination == null) continue;

            var sourceRoom = t.GetComponentInParent<CameraRoom>(true);
            if (sourceRoom == null) continue;

            _adjacency[sourceRoom].Add(t.destination);
            _adjacency[t.destination].Add(sourceRoom);
        }
    }

    public HashSet<CameraRoom> GetNeighbors(CameraRoom room)
    {
        return _adjacency.TryGetValue(room, out var neighbors) ? neighbors : new HashSet<CameraRoom>();
    }

    public HashSet<CameraRoom> GetWithinHops(CameraRoom origin, int maxHops)
    {
        var result = new HashSet<CameraRoom> { origin };
        var frontier = new Queue<(CameraRoom room, int depth)>();
        frontier.Enqueue((origin, 0));

        while (frontier.Count > 0)
        {
            var (current, depth) = frontier.Dequeue();
            if (depth >= maxHops) continue;

            foreach (var neighbor in GetNeighbors(current))
            {
                if (result.Add(neighbor))
                    frontier.Enqueue((neighbor, depth + 1));
            }
        }

        return result;
    }

    private void OnRoomChanged(CameraRoom previous, CameraRoom next)
    {
        var keep = GetWithinHops(next, 1);

        foreach (var room in _allRooms)
        {
            bool shouldBeActive = keep.Contains(room);
            if (room.gameObject.activeSelf != shouldBeActive)
                room.gameObject.SetActive(shouldBeActive);
        }
    }
}
