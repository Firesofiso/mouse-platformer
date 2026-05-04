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

    public void Register(ICutsceneParticipant participant) =>
        _participants[participant.ParticipantId] = participant;

    public void Unregister(ICutsceneParticipant participant) =>
        _participants.Remove(participant.ParticipantId);

    public void Play(CutsceneSequence sequence) =>
        StartCoroutine(RunSequence(sequence));

    public void AdvanceDialogue() => _waitingForInput = false;

    private IEnumerator RunSequence(CutsceneSequence sequence)
    {
        IsPlaying = true;

        foreach (var beat in sequence.beats)
            yield return RunBeat(beat);

        IsPlaying = false;
    }

    private IEnumerator RunBeat(CutsceneBeat beat)
    {
        var participant = Resolve(beat.speakerId);

        switch (beat.type)
        {
            case BeatType.Dialogue:
                if (beat.faceListener && participant != null)
                {
                    var listener = Resolve(string.IsNullOrEmpty(beat.listenerId) ? "Player" : beat.listenerId);
                    if (listener != null) participant.FaceTowards(listener.Transform.position);
                }

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
        }
    }

    private ICutsceneParticipant Resolve(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _participants.TryGetValue(id, out var p);
        return p;
    }
}
