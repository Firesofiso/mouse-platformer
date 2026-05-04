using UnityEngine;

// Smoke-test only — remove once real CutsceneSequence prompts are wired.
// Press E → bubble appears above this object.
public class SmokeDialogueTrigger : MonoBehaviour
{
    [SerializeField] string[] _lines = { "Squeak!", "Carrot cake?" };
    [SerializeField] DialogueStyle _style = DialogueStyle.Default;
    [SerializeField] float _autoClose = 3f;

    int _next;

    void OnEnable()  => DialogueInput.OnInteract += HandleInteract;
    void OnDisable() => DialogueInput.OnInteract -= HandleInteract;

    public void HandleInteract()
    {
        if (DialogueBubbles.instance == null) return;
        string line = _lines[_next % _lines.Length];
        _next++;
        DialogueBubbles.instance.Say(transform, line, _style, _autoClose);
    }
}
