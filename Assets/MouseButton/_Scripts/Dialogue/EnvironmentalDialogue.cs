using System;
using UnityEngine;

[RequireComponent(typeof(InteractionTarget))]
public class EnvironmentalDialogue : MonoBehaviour
{
    [Serializable]
    public struct Line
    {
        [TextArea] public string text;
        public DialogueStyle style;
        [Tooltip("0 = use style default")]
        public float charsPerSecond;
        public bool instant;
    }

    [SerializeField] Line[] _lines;
    [SerializeField] bool _loop;
    [SerializeField] float _autoCloseSeconds = 5f;

    InteractionTarget _target;
    int _index;
    bool _active;
    DialogueHandle _handle;

    void Awake()
    {
        _target = GetComponent<InteractionTarget>();
        _target.onInteract.AddListener(Advance);
    }

    void OnDestroy() => _target.onInteract.RemoveListener(Advance);

    void OnEnable()
    {
        InteractionManager.OnTargetLost += HandleDeselect;
        InteractionManager.OnTargetAcquired += HandleSelectionChanged;
    }

    void OnDisable()
    {
        InteractionManager.OnTargetLost -= HandleDeselect;
        InteractionManager.OnTargetAcquired -= HandleSelectionChanged;
        Close();
    }

    void Advance()
    {
        if (_lines == null || _lines.Length == 0) return;

        if (_handle != null && !_handle.IsOpen)
        {
            _index = 0;
            _active = false;
            _handle = null;
        }

        if (_index >= _lines.Length)
        {
            if (_loop) _index = 0;
            else { Close(); return; }
        }

        var line = _lines[_index];
        _index++;
        _active = true;
        _handle?.Close();
        _handle = DialogueBubbles.instance.Say(transform, line.text, line.style, _autoCloseSeconds);
        if (line.instant)
            DialogueBubbles.instance.CompleteReveal(transform);
        else if (line.charsPerSecond > 0f)
            DialogueBubbles.instance.SetSpeed(transform, line.charsPerSecond);
    }

    void HandleDeselect()
    {
        if (_active) Close();
    }

    void HandleSelectionChanged()
    {
        if (!_active) return;
        if (InteractionManager.Instance == null) return;
        if (InteractionManager.Instance.CurrentTarget != _target)
            Close();
    }

    void Close()
    {
        if (!_active) return;
        _handle?.Close();
        _handle = null;
        _active = false;
        _index = 0;
    }
}
