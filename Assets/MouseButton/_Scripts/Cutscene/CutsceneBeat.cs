using System;
using UnityEngine;

public enum BeatType
{
    Dialogue,   // show bubble, wait for player input
    Move,       // tell participant to walk to position
    Emote,      // trigger animation/emote on participant
    Wait,       // timed pause, no input
    Stop,       // halt participant movement
}

[Serializable]
public class CutsceneBeat
{
    public BeatType type;
    [Tooltip("Matches ICutsceneParticipant.ParticipantId")]
    public string speakerId;
    [TextArea] public string text;
    public DialogueStyle style;
    [Tooltip("If true, speaker turns toward listenerId before the bubble shows.")]
    public bool faceListener;
    [Tooltip("Defaults to \"Player\" when faceListener is true.")]
    public string listenerId;
    public string emoteId;
    public Vector2 targetPosition;
    [Tooltip("Seconds. For Dialogue beats: 0 = wait for player input. For others: 0 = fire-and-forget.")]
    public float duration;
}
