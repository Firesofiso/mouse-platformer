using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Checkpoint))]
public class CheckpointEditor : Editor
{
    SerializedProperty _roomOverrideProp;

    void OnEnable()
    {
        _roomOverrideProp = serializedObject.FindProperty("_roomOverride");
    }

    public override void OnInspectorGUI()
    {
        var checkpoint = (Checkpoint)target;

        serializedObject.Update();
        EditorGUILayout.PropertyField(_roomOverrideProp);
        serializedObject.ApplyModifiedProperties();

        var room = checkpoint.GetRoom();
        var resolvedLabel = _roomOverrideProp.objectReferenceValue != null
            ? "Resolved Room (override)"
            : "Resolved Room (from hierarchy)";

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(resolvedLabel, room, typeof(CameraRoom), true);
        EditorGUI.EndDisabledGroup();

        if (room == null)
        {
            EditorGUILayout.HelpBox(
                "No room resolved. Set Room Override, or place under a CameraRoomTransition / CameraRoom.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        var active = CheckpointTestSpawn.GetActive();
        bool isActive = active == checkpoint;

        if (isActive)
        {
            EditorGUILayout.HelpBox("Active test spawn — player spawns here on Play. (Per-session, not saved to scene.)", MessageType.Info);
            if (GUILayout.Button("Clear Test Spawn"))
                CheckpointTestSpawn.Clear();
        }
        else
        {
            if (GUILayout.Button("Set as Test Spawn"))
                CheckpointTestSpawn.SetActive(checkpoint);
        }
    }
}
