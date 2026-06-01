using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional. Overrides the room resolved from the parent transition or room hierarchy.")]
    [SerializeField] CameraRoom _roomOverride;

    public CameraRoom GetRoom()
    {
        if (_roomOverride != null) return _roomOverride;
        var transition = GetComponentInParent<CameraRoomTransition>(true);
        if (transition != null && transition.destination != null)
            return transition.destination;
        return GetComponentInParent<CameraRoom>(true);
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnPlayStart()
    {
        var active = CheckpointTestSpawn.GetActive();
        if (active != null)
            CheckpointSpawnRunner.Run(active);
    }

    void OnDrawGizmos()
    {
        bool isActiveTest = CheckpointTestSpawn.GetActive() == this;
        var color = isActiveTest ? Color.green : new Color(1f, 1f, 0f, 0.5f);

        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, 8f);

        if (isActiveTest)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawSphere(transform.position, 8f);
        }

        Gizmos.color = color;
        Gizmos.DrawLine(transform.position + Vector3.up * 12f, transform.position);
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(-3f, 4f, 0f));
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(3f, 4f, 0f));
    }
#endif
}
