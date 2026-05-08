using UnityEngine;

public class CameraRoom : MonoBehaviour
{
    public const int RoomWidth = 320;
    public const int RoomHeight = 180;

    [Min(1)] public int widthInRooms = 1;
    [Min(1)] public int heightInRooms = 1;

    public Vector2 Size => new Vector2(widthInRooms * RoomWidth, heightInRooms * RoomHeight);
    public Bounds Bounds => new Bounds(transform.position, new Vector3(Size.x, Size.y, float.MaxValue));

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(Size.x, Size.y, 0f));
    }
#endif
}
