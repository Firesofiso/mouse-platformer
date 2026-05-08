using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomTransition : MonoBehaviour
{
    public CameraRoom destination;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destination == null || !other.CompareTag("Player")) return;
        CameraController.Instance.PanTo(destination);
    }
}
