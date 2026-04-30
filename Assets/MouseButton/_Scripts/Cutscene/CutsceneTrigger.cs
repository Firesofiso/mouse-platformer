using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] CutsceneSequence _sequence;
    [SerializeField] bool _oneShot = true;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        if (CutsceneManager.IsPlaying) return;

        CutsceneManager.instance.Play(_sequence);
        if (_oneShot) gameObject.SetActive(false);
    }
}
