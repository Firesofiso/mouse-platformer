using UnityEditor;
using UnityEngine;

public static class CheckpointTestSpawn
{
    const string Key = "MouseButton.ActiveCheckpoint";

    public static Checkpoint GetActive()
    {
        var stored = SessionState.GetString(Key, null);
        if (string.IsNullOrEmpty(stored)) return null;
        if (!GlobalObjectId.TryParse(stored, out var id)) return null;
        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Checkpoint;
    }

    public static void SetActive(Checkpoint cp)
    {
        if (cp == null) { Clear(); return; }
        var id = GlobalObjectId.GetGlobalObjectIdSlow(cp);
        SessionState.SetString(Key, id.ToString());
        SceneView.RepaintAll();
    }

    public static void Clear()
    {
        SessionState.EraseString(Key);
        SceneView.RepaintAll();
    }
}
