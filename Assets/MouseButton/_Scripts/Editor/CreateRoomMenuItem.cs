using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using MouseButton.World;

public static class CreateRoomMenuItem
{
    [MenuItem("GameObject/2D Object/New Room", false, 10)]
    static void CreateNewRoom()
    {
        var root = new GameObject("New Room");
        Undo.RegisterCreatedObjectUndo(root, "Create New Room");

        var grid = root.AddComponent<Grid>();
        grid.cellSize = new Vector3(8f, 8f, 0f);
        root.AddComponent<CameraRoom>();
        root.AddComponent<RoomController>();

        CreateTilemapChild(root, "bgBackdrop", "Background", -100, 0);
        CreateTilemapChild(root, "bgDecoration", "Background", 10, 0);

        var platforms = CreateTilemapChild(root, "Platforms", "Terrain", 0, LayerMask.NameToLayer("Ground"));
        platforms.AddComponent<TilemapCollider2D>();

        CreateTilemapChild(root, "fgDecoration", "Background", 10, 0);

        CreateEmptyChild(root, "Fadeables");
        CreateEmptyChild(root, "Hazards");

        var decorShadows = CreateEmptyChild(root, "DecorationShadows");
        decorShadows.AddComponent<TilemapShadowController>();

        CreateEmptyChild(root, "Transitions");

        Selection.activeGameObject = root;
    }

    [MenuItem("GameObject/2D Object/New Transition", false, 11)]
    static void CreateNewTransition()
    {
        var parent = Selection.activeGameObject;

        if (parent == null || parent.GetComponentInParent<CameraRoom>() == null)
        {
            EditorUtility.DisplayDialog("New Transition",
                "Select a GameObject inside a room (e.g. the Transitions container).", "OK");
            return;
        }

        var transition = new GameObject("To_");
        Undo.RegisterCreatedObjectUndo(transition, "Create Transition");
        transition.transform.SetParent(parent.transform, false);

        var collider = transition.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(32f, 180f);
        transition.AddComponent<CameraRoomTransition>();

        var checkpoint = new GameObject("Checkpoint");
        checkpoint.transform.SetParent(transition.transform, false);
        checkpoint.AddComponent<Checkpoint>();

        Selection.activeGameObject = transition;
    }

    static GameObject CreateTilemapChild(GameObject parent, string name, string sortingLayer, int sortingOrder, int layer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.layer = layer;
        go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    static GameObject CreateEmptyChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }
}
