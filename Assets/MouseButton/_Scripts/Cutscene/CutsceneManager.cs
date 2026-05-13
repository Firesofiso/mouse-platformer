using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager instance { get; private set; }
    public static bool IsPlaying { get; private set; }

    private readonly Dictionary<string, ICutsceneParticipant> _participants = new();
    private bool _waitingForInput;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
    }

    void Start()
    {
        // Pick up any participants whose OnEnable fired before our Awake
        foreach (var p in FindObjectsOfType<MonoBehaviour>())
            if (p is ICutsceneParticipant participant && !_participants.ContainsKey(participant.ParticipantId))
                Register(participant);
    }

    public void Register(ICutsceneParticipant participant) =>
        _participants[participant.ParticipantId] = participant;

    public void Unregister(ICutsceneParticipant participant) =>
        _participants.Remove(participant.ParticipantId);

    public void Play(CutsceneSequence sequence) =>
        StartCoroutine(RunSequence(sequence));

    public Coroutine PlayAndReturn(CutsceneSequence sequence) =>
        StartCoroutine(RunSequence(sequence));

    public void AdvanceDialogue() => _waitingForInput = false;

    private IEnumerator RunSequence(CutsceneSequence sequence)
    {
        IsPlaying = true;

        foreach (var beat in sequence.beats)
        {
            if (beat.async)
                StartCoroutine(RunBeat(beat));
            else
                yield return RunBeat(beat);
        }

        IsPlaying = false;
    }

    private IEnumerator RunBeat(CutsceneBeat beat)
    {
        var participant = Resolve(beat.speakerId);

        // Face toward or away from listener (applies to any beat type)
        if (participant != null && (beat.faceListener || beat.faceAwayFromListener))
        {
            var listener = Resolve(string.IsNullOrEmpty(beat.listenerId) ? "Player" : beat.listenerId);
            if (listener != null)
            {
                if (beat.faceAwayFromListener)
                {
                    // Mirror the listener position across the participant
                    var away = participant.Transform.position * 2f - listener.Transform.position;
                    participant.FaceTowards(away);
                }
                else
                    participant.FaceTowards(listener.Transform.position);
            }
        }

        switch (beat.type)
        {
            case BeatType.Dialogue:
                if (DialogueBubbles.instance != null && participant != null)
                {
                    yield return DialogueBubbles.instance.SayAndWait(
                        participant.Transform, beat.text, beat.style, beat.duration);
                }
                else
                {
                    Debug.Log($"[{beat.speakerId}]: {beat.text}");
                    if (beat.duration <= 0)
                    {
                        _waitingForInput = true;
                        yield return new WaitUntil(() => !_waitingForInput);
                    }
                    else yield return new WaitForSeconds(beat.duration);
                }
                break;

            case BeatType.Move:
                if (participant == null) break;
                if (beat.duration <= 0)
                    StartCoroutine(participant.MoveTo(beat.targetPosition));
                else
                    yield return participant.MoveTo(beat.targetPosition);
                break;

            case BeatType.Emote:
                participant?.PlayEmote(beat.emoteId);
                if (beat.duration > 0) yield return new WaitForSeconds(beat.duration);
                break;

            case BeatType.Stop:
                participant?.Stop();
                break;

            case BeatType.Wait:
                yield return new WaitForSeconds(beat.duration);
                break;

            case BeatType.FollowTarget:
                if (participant == null) break;
                var target = Resolve(beat.followTargetId);
                if (target == null) break;
                yield return FollowTargetRoutine(participant.Transform, target.Transform,
                    beat.followOffset, beat.followSpeed, beat.duration);
                break;

            case BeatType.BezierMove:
                if (participant == null) break;
                if (!string.IsNullOrEmpty(beat.followTargetId))
                {
                    var bezierTarget = Resolve(beat.followTargetId);
                    if (bezierTarget != null)
                    {
                        // Offsets are relative to the target — cursor tracks the player while arcing away
                        var startOffset = participant.Transform.position - bezierTarget.Transform.position;
                        yield return BezierMoveRelative(participant.Transform, bezierTarget.Transform,
                            startOffset, beat.bezierControlOffset, beat.bezierEndOffset,
                            beat.duration, beat.bezierEasePower);
                        break;
                    }
                }
                yield return MotionUtils.BezierMove(participant.Transform,
                    beat.bezierControlOffset, beat.bezierEndOffset,
                    beat.duration, beat.bezierEasePower);
                break;

            case BeatType.SetActive:
                if (participant != null)
                    participant.Transform.gameObject.SetActive(beat.activeState);
                break;
        }
    }

    private static IEnumerator FollowTargetRoutine(Transform follower, Transform target,
        Vector3 offset, float speed, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var home = target.position + offset;
            var distance = Vector3.Distance(follower.position, home);
            follower.position = Vector3.MoveTowards(follower.position, home, speed * distance * Time.deltaTime);
            yield return null;
        }
    }

    private static IEnumerator BezierMoveRelative(Transform mover, Transform anchor,
        Vector3 startOffset, Vector3 controlOffset, Vector3 endOffset,
        float duration, float easePower)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float e = Mathf.Pow(t, easePower);
            float u = 1f - e;
            var offset = u * u * startOffset + 2f * u * e * (startOffset + controlOffset) + e * e * endOffset;
            mover.position = anchor.position + offset;
            yield return null;
        }
    }

    private ICutsceneParticipant Resolve(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _participants.TryGetValue(id, out var p);
        return p;
    }
}
