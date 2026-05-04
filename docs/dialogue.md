# Dialogue

`_Scripts/Dialogue/`

`DialogueBubbles` is a singleton that manages per-speaker bubble lifecycle. Entry points:

- `Say(speaker, text, style, autoCloseSeconds)` — fire-and-forget, returns a `DialogueHandle`
- `SayAndWait(speaker, text, style, duration)` — coroutine, used by cutscenes

Styles are defined in `DialogueStyleConfig` (ScriptableObject). Each `StyleEntry` sets sprite, text color, font scale, chars-per-second, shake, and optional offset override.

`DialogueBubble` (prefab: `Assets/MouseButton/Prefabs/UI/DialogueBubble.prefab`) renders the bubble using a `SpriteRenderer` child ("Background") and a legacy `TextMesh` child ("Text"). `FitBubble()` sizes and positions the background sprite to wrap the text mesh bounds each frame.
