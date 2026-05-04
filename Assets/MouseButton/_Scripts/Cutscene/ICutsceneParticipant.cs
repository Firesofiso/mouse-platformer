using System.Collections;
using UnityEngine;

public interface ICutsceneParticipant
{
    string ParticipantId { get; }
    Transform Transform { get; }
    IEnumerator MoveTo(Vector2 worldPosition);
    void PlayEmote(string emoteId);
    void FaceTowards(Vector2 worldPosition);
    void Stop();
}
