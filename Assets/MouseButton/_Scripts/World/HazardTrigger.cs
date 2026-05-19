using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HazardTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.Kill();
    }
}
