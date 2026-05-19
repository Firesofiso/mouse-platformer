using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using MouseButton.World;

[CustomEditor(typeof(TilemapShadowController))]
public class TilemapShadowEditor : Editor
{
    static bool _painting;

    static readonly Color FillColor = new(0.2f, 0.9f, 0.2f, 0.3f);
    static readonly Color OutlineColor = new(0.2f, 0.9f, 0.2f, 0.85f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var controller = (TilemapShadowController)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Shadow Painter", EditorStyles.boldLabel);

        if (controller.TargetTilemap == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Target Tilemap to enable painting.",
                MessageType.Warning);
            return;
        }

        var style = new GUIStyle(GUI.skin.button);
        if (_painting)
            style.normal.textColor = Color.green;

        if (GUILayout.Button(_painting ? "Stop Painting (J)" : "Start Painting (J)", style))
            _painting = !_painting;

        if (_painting)
        {
            EditorGUILayout.HelpBox(
                $"Click tiles in Scene View to assign/unassign shadows.\n" +
                $"Green = shadow caster tile.\n" +
                $"Painting on: {controller.TargetTilemap.gameObject.name}",
                MessageType.Info);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Shadow Objects", EditorStyles.boldLabel);

        var existing = 0;
        for (int i = 0; i < controller.transform.childCount; i++)
            if (controller.transform.GetChild(i).GetComponent<ShadowCaster2D>() != null)
                existing++;
        EditorGUILayout.LabelField($"Shadow casters: {existing}");

        if (GUILayout.Button("Create Shadow Objects"))
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Create Shadow Objects");
            controller.DeleteShadowObjects();
            int created = controller.CreateShadowObjects();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log($"Created {created} merged shadow casters.");
        }

        if (existing > 0 && GUILayout.Button("Delete Shadow Objects"))
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Delete Shadow Objects");
            int deleted = controller.DeleteShadowObjects();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log($"Deleted {deleted} shadow casters.");
        }
    }

    void OnSceneGUI()
    {
        var controller = (TilemapShadowController)target;

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.J)
        {
            _painting = !_painting;
            Event.current.Use();
            Repaint();
        }

        if (!_painting) return;
        if (controller.TargetTilemap == null) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            var worldPos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
            worldPos.z = 0;

            var tm = controller.TargetTilemap;
            var cell = tm.WorldToCell(worldPos);

            if (tm.HasTile(cell))
            {
                Undo.RecordObject(controller, "Toggle Shadow Tile");

                if (controller.HasCell(tm, cell))
                    controller.RemoveCell(tm, cell);
                else
                    controller.AddCell(tm, cell);

                EditorUtility.SetDirty(controller);
            }

            Event.current.Use();
        }

        DrawAssignedTiles(controller);
        SceneView.RepaintAll();
    }

    void DrawAssignedTiles(TilemapShadowController controller)
    {
        var so = new SerializedObject(controller);
        var list = so.FindProperty("_shadowCells");
        if (list == null) return;

        for (int i = 0; i < list.arraySize; i++)
        {
            var entry = list.GetArrayElementAtIndex(i);
            var tmProp = entry.FindPropertyRelative("tilemap");
            var cellProp = entry.FindPropertyRelative("cell");

            if (tmProp.objectReferenceValue == null) continue;
            var tm = (Tilemap)tmProp.objectReferenceValue;
            var cell = cellProp.vector3IntValue;

            var worldCenter = tm.GetCellCenterWorld(cell);
            var size = tm.cellSize;

            Handles.DrawSolidRectangleWithOutline(
                new Rect(worldCenter.x - size.x / 2f, worldCenter.y - size.y / 2f, size.x, size.y),
                FillColor, OutlineColor);

            Handles.Label(worldCenter + Vector3.up * (size.y * 0.3f),
                "S", new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = OutlineColor },
                    fontSize = 10
                });
        }
    }
}
