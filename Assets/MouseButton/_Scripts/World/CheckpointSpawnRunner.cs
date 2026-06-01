#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

internal class CheckpointSpawnRunner : MonoBehaviour
{
    Checkpoint _checkpoint;

    public static void Run(Checkpoint cp)
    {
        var go = new GameObject("[CheckpointSpawnRunner]");
        go.hideFlags = HideFlags.HideAndDontSave;
        var r = go.AddComponent<CheckpointSpawnRunner>();
        r._checkpoint = cp;
    }

    IEnumerator Start()
    {
        yield return null;

        if (_checkpoint == null) { Destroy(gameObject); yield break; }

        var room = _checkpoint.GetRoom();
        if (room != null && CameraController.Instance != null)
            CameraController.Instance.SnapTo(room);

        var pos = _checkpoint.transform.position;
        var player = GameManager.Instance != null ? GameManager.Instance.playerTransform : null;
        if (player != null)
        {
            var p = pos;
            p.z = player.position.z;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.position = p;
            }
            player.position = p;
            Physics2D.SyncTransforms();

            var cursor = GameManager.Instance.cursorTransform;
            if (cursor != null)
                cursor.position = p;
        }

        if (RespawnManager.Instance != null)
            RespawnManager.Instance.SetRespawnPoint(_checkpoint);

        Destroy(gameObject);
    }
}
#endif
