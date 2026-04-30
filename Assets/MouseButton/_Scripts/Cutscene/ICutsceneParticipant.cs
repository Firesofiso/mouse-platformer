using System.Collections;
using UnityEngine;

public interface ICutsceneParticipant
{
    string ParticipantId { get; }
    IEnumerator MoveTo(Vector2 worldPosition);
    void PlayEmote(string emoteId);
    void Stop();
}
