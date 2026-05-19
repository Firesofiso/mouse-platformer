using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using MouseButton.World;

[CustomEditor(typeof(ElectricHazardController))]
public class ElectricHazardEditor : Editor
{
    static bool _painting;
    static PaintLayer _paintLayer = PaintLayer.Platform;

    enum PaintLayer { Platform, Decoration }

    static readonly Color PlatformColor = new(1f, 0.3f, 0.3f, 0.35f);
    static readonly Color DecorationColor = new(0.3f, 0.6f, 1f, 0.35f);
    static readonly Color PlatformOutline = new(1f, 0.3f, 0.3f, 0.9f);
    static readonly Color DecorationOutline = new(0.3f, 0.6f, 1f, 0.9f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Tile Painter", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            var style = new GUIStyle(GUI.skin.button);
            if (_painting)
                style.normal.textColor = Color.yellow;

            if (GUILayout.Button(_painting ? "Stop Painting (H)" : "Start Painting (H)", style))
                _painting = !_painting;
        }

        if (_painting)
        {
            _paintLayer = (PaintLayer)EditorGUILayout.EnumPopup("Paint Layer", _paintLayer);
            EditorGUILayout.HelpBox(
                "Click tiles in Scene View to assign/unassign.\n" +
                "Red = Platform (dangerous tiles)\n" +
                "Blue = Decoration (panel/wires)\n" +
                "Click assigned tile to remove it.",
                MessageType.Info);
        }
    }

    void OnSceneGUI()
    {
        var controller = (ElectricHazardController)target;

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.H)
        {
            _painting = !_painting;
            Event.current.Use();
            Repaint();
        }

        if (!_painting) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            var worldPos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
            worldPos.z = 0;

            var tilemaps = FindTilemapsInRoom(controller.transform, _paintLayer);
            foreach (var tm in tilemaps)
            {
                var cell = tm.WorldToCell(worldPos);
                if (!tm.HasTile(cell)) continue;

                Undo.RecordObject(controller, "Toggle Hazard Tile");

                if (controller.HasCell(tm, cell))
                {
                    controller.RemovePlatformCell(tm, cell);
                    controller.RemoveDecorationCell(tm, cell);
                }
                else if (_paintLayer == PaintLayer.Platform)
                {
                    controller.AddPlatformCell(tm, cell);
                }
                else
                {
                    controller.AddDecorationCell(tm, cell);
                }

                EditorUtility.SetDirty(controller);
                break;
            }

            Event.current.Use();
        }

        DrawAssignedTileHandles(controller);
        SceneView.RepaintAll();
    }

    void DrawAssignedTileHandles(ElectricHazardController controller)
    {
        var so = new SerializedObject(controller);

        DrawCellHandles(so.FindProperty("_platformCells"), PlatformColor, PlatformOutline, "P");
        DrawCellHandles(so.FindProperty("_decorationCells"), DecorationColor, DecorationOutline, "D");
    }

    void DrawCellHandles(SerializedProperty list, Color fill, Color outline, string label)
    {
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
                fill, outline);

            Handles.Label(worldCenter + Vector3.up * (size.y * 0.3f),
                label, new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = outline },
                    fontSize = 10
                });
        }
    }

    static Tilemap[] FindTilemapsInRoom(Transform hazardTransform, PaintLayer layer)
    {
        var room = hazardTransform.parent;
        var all = new System.Collections.Generic.List<Tilemap>();

        if (room != null)
            room.GetComponentsInChildren(true, all);
        if (all.Count == 0)
            all.AddRange(Object.FindObjectsOfType<Tilemap>());

        var targetName = layer == PaintLayer.Platform ? "Platforms" : "bgDecoration";
        var filtered = new System.Collections.Generic.List<Tilemap>();
        foreach (var tm in all)
            if (tm.gameObject.name == targetName)
                filtered.Add(tm);

        return filtered.Count > 0 ? filtered.ToArray() : all.ToArray();
    }
}
