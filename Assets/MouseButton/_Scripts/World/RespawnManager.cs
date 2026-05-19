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
    [SerializeField] Transform _activeRespawnPoint;

    bool _dying;

    public static event Action Died;
    public static event Action Respawned;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetRespawnPoint(Transform point)
    {
        _activeRespawnPoint = point;
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

        var respawnPos = _activeRespawnPoint.position;
        respawnPos.z = player.position.z;
        controller._rb.velocity = Vector2.zero;
        controller._rb.position = respawnPos;
        player.position = respawnPos;
        Physics2D.SyncTransforms();

        var cursor = GameManager.Instance.cursorTransform;
        if (cursor != null)
            cursor.position = respawnPos;

        if (CameraController.Instance != null && CameraController.Instance.CurrentRoom != null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var room = CameraController.Instance.CurrentRoom;
                cam.transform.position = new Vector3(
                    Mathf.Round(room.transform.position.x),
                    Mathf.Round(room.transform.position.y),
                    cam.transform.position.z);
            }
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

        _dying = false;
    }
}
