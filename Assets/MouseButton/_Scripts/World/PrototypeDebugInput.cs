#if UNITY_EDITOR
using UnityEngine;

internal class PrototypeDebugInput : MonoBehaviour
{
    const KeyCode KillKey = KeyCode.K;
    const KeyCode TeleportKey = KeyCode.T;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        var go = new GameObject("[PrototypeDebugInput]");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<PrototypeDebugInput>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KillKey))
            Kill();
        if (Input.GetKeyDown(TeleportKey))
            TeleportToMouse();
    }

    static void Kill()
    {
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.Kill();
    }

    static void TeleportToMouse()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var player = GameManager.Instance != null ? GameManager.Instance.playerTransform : null;
        if (player == null) return;

        var mouse = Input.mousePosition;
        var world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
        world.z = player.position.z;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.position = world;
        }
        player.position = world;
        Physics2D.SyncTransforms();

        var cursor = GameManager.Instance.cursorTransform;
        if (cursor != null)
            cursor.position = world;
    }
}
#endif
