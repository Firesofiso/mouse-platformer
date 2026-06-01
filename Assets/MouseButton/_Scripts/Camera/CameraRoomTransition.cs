using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomTransition : MonoBehaviour
{
    public enum TeleportMode { None, Full, AxisX, AxisY }

    public CameraRoom destination;
    [Tooltip("How to move the player on transition. Uses the child Checkpoint as the target position.")]
    public TeleportMode teleport = TeleportMode.None;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destination == null || !other.CompareTag("Player")) return;

        var sourceRoom = GetComponentInParent<CameraRoom>();
        if (sourceRoom != null
            && CameraController.Instance != null
            && CameraController.Instance.CurrentRoom != null
            && CameraController.Instance.CurrentRoom != sourceRoom)
            return;

        CameraController.Instance.PanTo(destination);

        var checkpoint = GetComponentInChildren<Checkpoint>(true);
        if (checkpoint == null) return;

        if (RespawnManager.Instance != null)
            RespawnManager.Instance.SetRespawnPoint(checkpoint);

        if (teleport != TeleportMode.None)
            TeleportPlayer(other.transform, checkpoint.transform.position);
    }

    void TeleportPlayer(Transform player, Vector3 target)
    {
        var pos = player.position;
        switch (teleport)
        {
            case TeleportMode.Full: pos.x = target.x; pos.y = target.y; break;
            case TeleportMode.AxisX: pos.x = target.x; break;
            case TeleportMode.AxisY: pos.y = target.y; break;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = pos;
        player.position = pos;
        Physics2D.SyncTransforms();

        if (GameManager.Instance != null && GameManager.Instance.cursorTransform != null)
            GameManager.Instance.cursorTransform.position = pos;
    }
}
