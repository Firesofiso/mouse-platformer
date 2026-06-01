using System;
using System.Collections;
using TarodevController;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [SerializeField] float _fadeOutDuration = 0.3f;
    [SerializeField] float _fadeInDuration = 0.4f;
    [SerializeField] float _holdBlackDuration = 0.15f;
    [SerializeField] Checkpoint _activeCheckpoint;

    bool _dying;

    public static event Action Died;
    public static event Action Respawned;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetRespawnPoint(Checkpoint checkpoint)
    {
        _activeCheckpoint = checkpoint;
    }

    public void Kill()
    {
        if (_dying) return;
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        _dying = true;

        var player = GameManager.Instance.playerTransform;
        var controller = player.GetComponent<MouseController>();

        controller.TakeAwayControl();
        player.gameObject.SetActive(false);

        Died?.Invoke();

        if (PixelDissolveController.Instance != null)
        {
            bool fadeOutDone = false;
            PixelDissolveController.Instance.FadeOut(_fadeOutDuration, () => fadeOutDone = true);
            while (!fadeOutDone) yield return null;
        }

        yield return new WaitForSecondsRealtime(_holdBlackDuration);

        var respawnPos = _activeCheckpoint.transform.position;
        respawnPos.z = player.position.z;
        controller._rb.velocity = Vector2.zero;
        controller._rb.position = respawnPos;
        player.position = respawnPos;
        Physics2D.SyncTransforms();

        var cursor = GameManager.Instance.cursorTransform;
        if (cursor != null)
            cursor.position = respawnPos;

        var checkpointRoom = _activeCheckpoint.GetRoom();
        bool willPan = checkpointRoom != null
            && CameraController.Instance != null
            && CameraController.Instance.CurrentRoom != checkpointRoom;

        if (checkpointRoom != null && CameraController.Instance != null)
        {
            if (willPan)
                CameraController.Instance.PanTo(checkpointRoom);
            else
                CameraController.Instance.SnapTo(checkpointRoom);
        }

        yield return null;

        player.gameObject.SetActive(true);
        controller.ReturnControl();
        Respawned?.Invoke();

        if (PixelDissolveController.Instance != null)
        {
            PixelDissolveController.Instance.FadeIn(_fadeInDuration);
            yield return new WaitForSecondsRealtime(_fadeInDuration + 0.05f);
        }

        while (CameraController.Instance != null && CameraController.Instance.IsPanning)
            yield return null;

        _dying = false;
    }
}
