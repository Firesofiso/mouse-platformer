using UnityEditor;
using UnityEngine;

public static class SnapRoomsEditor
{
    [MenuItem("Tools/Snap Selected Rooms to Grid")]
    static void SnapSelected()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
        {
            var room = go.GetComponent<CameraRoom>();
            if (room == null) continue;

            var t = go.transform;
            var pos = t.position;
            var snapped = new Vector3(
                Mathf.Round(pos.x / CameraRoom.RoomWidth)  * CameraRoom.RoomWidth,
                Mathf.Round(pos.y / CameraRoom.RoomHeight) * CameraRoom.RoomHeight,
                pos.z
            );

            if (snapped == pos) continue;

            Undo.RecordObject(t, "Snap Room to Grid");
            t.position = snapped;
            count++;
        }

        if (count == 0)
            Debug.Log("Snap Rooms: nothing to snap (select one or more CameraRoom GameObjects).");
        else
            Debug.Log($"Snap Rooms: snapped {count} room(s).");
    }

    [MenuItem("Tools/Snap Selected Rooms to Grid", true)]
    static bool SnapSelectedValidate() => Selection.gameObjects.Length > 0;
}
