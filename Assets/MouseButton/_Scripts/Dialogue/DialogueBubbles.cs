using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueBubbles : MonoBehaviour
{
    public static DialogueBubbles instance { get; private set; }

    [SerializeField] DialogueBubble _bubblePrefab;
    [SerializeField] DialogueStyleConfig _styleConfig;
    [SerializeField] Vector2 _defaultOffset = new(0f, 1f);

    class Session
    {
        public DialogueBubble bubble;
        public DialogueHandle handle;
        public Coroutine autoClose;
    }

    readonly Dictionary<Transform, Session> _sessions = new();
    bool _advanceRequested;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void OnEnable() => DialogueInput.OnInteract += HandleInteract;
    void OnDisable() => DialogueInput.OnInteract -= HandleInteract;

    void HandleInteract() => _advanceRequested = true;

    // Fire-and-forget quip. Caller can hold the handle to close early.
    public DialogueHandle Say(Transform speaker, string text, DialogueStyle style = DialogueStyle.Default,
                              float autoCloseSeconds = 5f, Vector2? offset = null)
    {
        if (_sessions.TryGetValue(speaker, out var prev))
        {
            if (prev.autoClose != null) StopCoroutine(prev.autoClose);
            prev.handle?.Invalidate();
        }

        var styleEntry = _styleConfig != null ? _styleConfig.Get(style) : null;
        Vector2 effectiveOffset = styleEntry is { overrideOffset: true }
            ? styleEntry.bubbleOffset
            : offset ?? _defaultOffset;
        var bubble = GetOrCreate(speaker, effectiveOffset);
        bubble.Show(text, styleEntry);
        var handle = new DialogueHandle(this, speaker);

        var session = prev ?? new Session();
        session.bubble = bubble;
        session.handle = handle;
        session.autoClose = autoCloseSeconds > 0f
            ? StartCoroutine(AutoClose(speaker, autoCloseSeconds, handle))
            : null;
        _sessions[speaker] = session;

        return handle;
    }

    // Coroutine variant for cutscenes — completes when player advances or duration expires.
    public IEnumerator SayAndWait(Transform speaker, string text, DialogueStyle style,
                                  float duration, Vector2? offset = null)
    {
        var bubble = GetOrCreate(speaker, offset ?? _defaultOffset);
        bubble.Show(text, _styleConfig != null ? _styleConfig.Get(style) : null);
        _advanceRequested = false;

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
        else
        {
            // First press: complete reveal. Second press: advance.
            while (bubble.IsRevealing && !_advanceRequested) yield return null;
            if (_advanceRequested && bubble.IsRevealing)
            {
                bubble.CompleteReveal();
                _advanceRequested = false;
            }
            while (!_advanceRequested) yield return null;
            _advanceRequested = false;
        }

        Close(speaker);
    }

    public void CompleteReveal(Transform speaker)
    {
        if (_sessions.TryGetValue(speaker, out var session) && session.bubble != null)
            session.bubble.CompleteReveal();
    }

    public void SetSpeed(Transform speaker, float charsPerSecond)
    {
        if (_sessions.TryGetValue(speaker, out var session) && session.bubble != null)
            session.bubble.SetSpeed(charsPerSecond);
    }

    public void Close(Transform speaker)
    {
        if (speaker == null) return;
        if (!_sessions.TryGetValue(speaker, out var session)) return;
        if (session.autoClose != null) StopCoroutine(session.autoClose);
        if (session.bubble != null) session.bubble.Hide();
        session.handle?.Invalidate();
        _sessions.Remove(speaker);
    }

    DialogueBubble GetOrCreate(Transform speaker, Vector2 offset)
    {
        if (_sessions.TryGetValue(speaker, out var s) && s.bubble != null) return s.bubble;
        var b = Instantiate(_bubblePrefab);
        b.Bind(speaker, offset);
        return b;
    }

    IEnumerator AutoClose(Transform speaker, float seconds, DialogueHandle handle)
    {
        yield return new WaitForSeconds(seconds);
        if (handle.IsOpen) Close(speaker);
    }
}

public class DialogueHandle
{
    readonly DialogueBubbles _owner;
    readonly Transform _speaker;
    public bool IsOpen { get; private set; } = true;

    public DialogueHandle(DialogueBubbles owner, Transform speaker)
    {
        _owner = owner;
        _speaker = speaker;
    }

    public void Invalidate() => IsOpen = false;

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        _owner.Close(_speaker);
    }
}
