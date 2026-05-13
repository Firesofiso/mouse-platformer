using System;
using UnityEngine;

public enum BeatType
{
    Dialogue,       // show bubble, wait for player input
    Move,           // tell participant to walk to position
    Emote,          // trigger animation/emote on participant
    Wait,           // timed pause, no input
    Stop,           // halt participant movement
    FollowTarget,   // follow another participant at offset for duration
    BezierMove,     // quadratic bezier curve movement
    SetActive,      // enable/disable participant's GameObject
}

[Serializable]
public class CutsceneBeat
{
    // ── Common ────────────────────────────────────────────────────────
    public BeatType type;
    [Tooltip("Matches ICutsceneParticipant.ParticipantId")]
    public string speakerId;
    [Tooltip("Seconds.\n" +
        "Dialogue: 0 = wait for player input.\n" +
        "Emote: 0 = fire-and-forget, >0 = hold before next beat.\n" +
        "FollowTarget/BezierMove: how long the motion lasts.")]
    public float duration;
    [Tooltip("If true, this beat runs in the background — the sequence immediately continues to the next beat.")]
    public bool async;

    // ── Dialogue ──────────────────────────────────────────────────────
    [TextArea] public string text;
    public DialogueStyle style;
    [Tooltip("Speaker turns toward listenerId before the beat.")]
    public bool faceListener;
    [Tooltip("Speaker turns away from listenerId before the beat.")]
    public bool faceAwayFromListener;
    [Tooltip("Defaults to \"Player\" when faceListener is true.")]
    public string listenerId;

    // ── Emote ─────────────────────────────────────────────────────────
    [Tooltip("String passed to ICutsceneParticipant.PlayEmote.\n" +
        "Cursor examples: \"surprise\", \"frown\", \"smile\", \"idle\"")]
    public string emoteId;

    // ── Move ──────────────────────────────────────────────────────────
    public Vector2 targetPosition;

    // ── Follow Target ─────────────────────────────────────────────────
    [Header("Follow Target")]
    [Tooltip("ParticipantId of the target to follow (also used by BezierMove for relative mode).")]
    public string followTargetId;
    [Tooltip("Offset from the target's position. The follower moves toward target + offset.")]
    public Vector3 followOffset;
    [Tooltip("Movement speed. Actual speed = followSpeed × distance, so it's elastic — faster when far, gentle when close.")]
    public float followSpeed;

    // ── Bezier Move ───────────────────────────────────────────────────
    //
    //  Quadratic bezier: P(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
    //
    //  WITHOUT followTargetId (absolute mode):
    //    P0 = current position
    //    P1 = current position + controlOffset   ← the curve bends toward this
    //    P2 = current position + endOffset        ← where it ends up
    //    All offsets are relative to where the speaker is when the beat starts.
    //
    //  WITH followTargetId (relative mode):
    //    Each frame: position = target.position + bezier(t)
    //    P0 = starting offset from target (auto-calculated)
    //    P1 = P0 + controlOffset                  ← curve bends toward this offset from target
    //    P2 = endOffset                           ← final offset from target
    //    The speaker tracks the target while arcing away. If the target falls,
    //    the speaker falls with it.
    //
    //  easePower controls acceleration:
    //    1 = linear, 2 = ease-in (default), higher = slower start then fast finish.
    //
    [Header("Bezier Move")]
    [Tooltip("Bends the curve. Think of it as a magnet pulling the path sideways.\n" +
        "Absolute: offset from start position.\n" +
        "Relative: offset from starting target-offset.")]
    public Vector3 bezierControlOffset;
    [Tooltip("Where the curve ends.\n" +
        "Absolute: offset from start position.\n" +
        "Relative: final offset from target (e.g. (60,80) = far above-right of target).")]
    public Vector3 bezierEndOffset;
    [Range(1f, 5f)]
    [Tooltip("1 = linear, 2 = ease-in (slow start), higher = more dramatic acceleration.")]
    public float bezierEasePower = 2f;

    // ── Set Active ────────────────────────────────────────────────────
    [Header("Set Active")]
    public bool activeState;
}
