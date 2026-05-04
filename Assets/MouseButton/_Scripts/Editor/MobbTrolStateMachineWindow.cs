using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TarodevController;
using UnityEditor;
using UnityEngine;

public class MobbTrolStateMachineWindow : EditorWindow {
    private StatefulUnit _unit;
    private Vector2 _scroll;

    private readonly Dictionary<State, Rect> _nodeRects = new Dictionary<State, Rect>();
    private readonly List<(State from, State to)> _edges = new List<(State from, State to)>();
    private float _contentWidth;
    private float _contentHeight;

    [MenuItem("Window/MouseButton/MobbTrol State Machine")]
    public static void ShowWindow() {
        GetWindow<MobbTrolStateMachineWindow>("MobbTrol State Machine");
    }

    private void OnEnable() {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Selection.selectionChanged += OnSelectionChanged;
        OnSelectionChanged();
    }

    private void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange _) => Repaint();

    private void OnSelectionChanged() {
        if (_unit != null) return;
        var selected = Selection.activeGameObject;
        if (selected == null) return;
        _unit = selected.GetComponentInParent<MobbTrolUnit>() ?? selected.GetComponent<MobbTrolUnit>();
        Repaint();
    }

    private void OnGUI() {
        DrawToolbar();

        if (_unit == null) {
            EditorGUILayout.HelpBox("Assign a MobbTrolUnit (or select one in the hierarchy).", MessageType.Info);
            return;
        }

        if (_unit.InitialState == null) {
            EditorGUILayout.HelpBox("This unit has no InitialState assigned.", MessageType.Warning);
            return;
        }

        BuildGraph(_unit.InitialState);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        Rect canvasRect = GUILayoutUtility.GetRect(_contentWidth, _contentHeight);
        DrawGraph(canvasRect);
        EditorGUILayout.EndScrollView();

        DrawLegend();

        if (Application.isPlaying) Repaint();
    }

    private void DrawToolbar() {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
            _unit = (StatefulUnit)EditorGUILayout.ObjectField(_unit, typeof(StatefulUnit), true, GUILayout.Width(260));

            if (GUILayout.Button("Use Selected", EditorStyles.toolbarButton, GUILayout.Width(100))) {
                var selected = Selection.activeGameObject;
                if (selected != null)
                    _unit = selected.GetComponentInParent<MobbTrolUnit>() ?? selected.GetComponent<MobbTrolUnit>();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                Repaint();
        }
    }

    private void BuildGraph(State root) {
        _nodeRects.Clear();
        _edges.Clear();

        var levels = new Dictionary<State, int>();
        var queue = new Queue<State>();
        levels[root] = 0;
        queue.Enqueue(root);

        while (queue.Count > 0) {
            var state = queue.Dequeue();
            foreach (var next in GetStateReferences(state)) {
                _edges.Add((state, next));
                if (levels.ContainsKey(next)) continue;
                levels[next] = levels[state] + 1;
                queue.Enqueue(next);
            }
        }

        const float nodeWidth = 170f;
        const float nodeHeight = 52f;
        const float xGap = 40f;
        const float yGap = 18f;

        var byLevel = levels.GroupBy(kvp => kvp.Value).OrderBy(g => g.Key).ToList();
        float contentHeight = 0f;

        foreach (var group in byLevel) {
            int indexInColumn = 0;
            float x = 20f + group.Key * (nodeWidth + xGap);
            foreach (var kv in group.OrderBy(k => k.Key.GetType().Name)) {
                float y = 20f + indexInColumn * (nodeHeight + yGap);
                _nodeRects[kv.Key] = new Rect(x, y, nodeWidth, nodeHeight);
                indexInColumn++;
                contentHeight = Mathf.Max(contentHeight, y + nodeHeight + 20f);
            }
        }

        _contentWidth = (byLevel.Count + 1) * (nodeWidth + xGap);
        _contentHeight = contentHeight;
    }

    private void DrawGraph(Rect canvasRect) {
        var activeBranch = Application.isPlaying && _unit.RootState != null
            ? new HashSet<State>(_unit.RootState.GetActiveStateBranch())
            : new HashSet<State>();

        var offset = canvasRect.position;

        Handles.BeginGUI();

        foreach (var edge in _edges) {
            if (!_nodeRects.TryGetValue(edge.from, out var fromRect) || !_nodeRects.TryGetValue(edge.to, out var toRect))
                continue;

            bool hot = activeBranch.Contains(edge.from) && activeBranch.Contains(edge.to);
            Handles.color = hot ? new Color(1f, 0.55f, 0f) : new Color(0.6f, 0.6f, 0.6f);

            var from = new Vector3(offset.x + fromRect.xMax, offset.y + fromRect.center.y);
            var to = new Vector3(offset.x + toRect.xMin, offset.y + toRect.center.y);
            Handles.DrawAAPolyLine(hot ? 3f : 1.5f, from, to);
        }

        foreach (var kv in _nodeRects) {
            var state = kv.Key;
            var rectLocal = kv.Value;
            var drawRect = new Rect(offset.x + rectLocal.x, offset.y + rectLocal.y, rectLocal.width, rectLocal.height);

            bool isActive = activeBranch.Contains(state);
            var fill = isActive ? new Color(1f, 0.72f, 0.25f, 0.9f) : new Color(0.2f, 0.2f, 0.2f, 0.9f);
            var border = isActive ? new Color(1f, 0.4f, 0f) : new Color(0.4f, 0.4f, 0.4f);

            EditorGUI.DrawRect(drawRect, fill);
            Handles.color = border;
            Handles.DrawAAPolyLine(2f,
                new Vector3(drawRect.xMin, drawRect.yMin), new Vector3(drawRect.xMax, drawRect.yMin),
                new Vector3(drawRect.xMax, drawRect.yMax), new Vector3(drawRect.xMin, drawRect.yMax),
                new Vector3(drawRect.xMin, drawRect.yMin));

            var labelStyle = new GUIStyle(EditorStyles.boldLabel) {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = isActive ? Color.black : Color.white }
            };
            GUI.Label(drawRect, state.GetType().Name, labelStyle);
        }

        Handles.EndGUI();
    }

    private void DrawLegend() {
        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            Application.isPlaying
                ? "Orange nodes/edges show the active branch from RootState to current Substate."
                : "Enter Play mode to see live active-state highlighting.",
            MessageType.None);

        if (Application.isPlaying && _unit.RootState != null) {
            string branch = string.Join(" > ", _unit.RootState.GetActiveStateBranch().Select(s => s.GetType().Name));
            EditorGUILayout.LabelField("Active Branch", branch);
        }
    }

    private static IEnumerable<State> GetStateReferences(State owner) {
        if (owner == null) yield break;

        var fields = owner.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields) {
            if (!typeof(State).IsAssignableFrom(field.FieldType)) continue;
            if (field.Name == nameof(State.Substate)) continue;
            var value = field.GetValue(owner) as State;
            if (value != null) yield return value;
        }
    }
}
