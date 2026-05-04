using System;

// Decoupled input bridge. InteractionManager calls Fire() to advance dialogue.
// DialogueBubbles subscribes to step through lines.
public static class DialogueInput
{
    public static event Action OnInteract;

    public static void Fire() => OnInteract?.Invoke();
}
