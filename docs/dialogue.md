# Dialogue

`_Scripts/Dialogue/`

`DialogueBubbles` is a singleton that manages per-speaker bubble lifecycle. Entry points:

- `Say(speaker, text, style, autoCloseSeconds)` — fire-and-forget, returns a `DialogueHandle`
- `SayAndWait(speaker, text, style, duration)` — coroutine, used by cutscenes

Styles are defined in `DialogueStyleConfig` (ScriptableObject). Each style controls appearance and timing — add new styles there, not in code.
