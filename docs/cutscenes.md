# Cutscenes

`_Scripts/Cutscene/`

`CutsceneManager` singleton runs `CutsceneSequence` assets (lists of `CutsceneBeat`). Participants register via `ICutsceneParticipant` using a string `ParticipantId`. Beat types: `Dialogue`, `Move`, `Emote`, `Stop`, `Wait`.
