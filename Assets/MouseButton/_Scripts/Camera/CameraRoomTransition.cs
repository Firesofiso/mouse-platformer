using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomTransition : MonoBehaviour
{
    public CameraRoom destination;
    [SerializeField] Transform _respawnPoint;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destination == null || !other.CompareTag("Player")) return;
        CameraController.Instance.PanTo(destination);

        if (_respawnPoint != null && RespawnManager.Instance != null)
            RespawnManager.Instance.SetRespawnPoint(_respawnPoint);
    }
}
