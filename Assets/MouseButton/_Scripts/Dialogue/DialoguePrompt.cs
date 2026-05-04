using UnityEngine;

// Wire InteractionTarget.onInteract -> DialoguePrompt.Trigger() in the inspector.
public class DialoguePrompt : MonoBehaviour
{
    [SerializeField] CutsceneSequence _sequence;
    [SerializeField] bool _oneShot = false;

    bool _consumed;

    public void Trigger()
    {
        if (_consumed || CutsceneManager.IsPlaying) return;
        if (_sequence == null || CutsceneManager.instance == null) return;

        CutsceneManager.instance.Play(_sequence);
        if (_oneShot) _consumed = true;
    }
}
