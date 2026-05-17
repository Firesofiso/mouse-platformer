using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

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

        // Transition placeholder
        var transition = new GameObject("To -");
        transition.transform.SetParent(root.transform, false);
        transition.AddComponent<BoxCollider2D>();
        transition.AddComponent<CameraRoomTransition>();

        CreateTilemapChild(root, "bgBackdrop", "Background", 0, 0);
        CreateTilemapChild(root, "bgDecoration", "Background", 10, 0);

        var platforms = CreateTilemapChild(root, "Platforms", "Cursor", 0, LayerMask.NameToLayer("Ground"));
        platforms.AddComponent<TilemapCollider2D>();

        CreateTilemapChild(root, "fgDecoration", "Background", 10, 0);

        var fgFadeable = CreateTilemapChild(root, "fgFadeable", "Foreground", 0, 0);
        fgFadeable.AddComponent<Fadeable>();
        fgFadeable.AddComponent<BoxCollider2D>();
        fgFadeable.AddComponent<TilemapFadeTrigger>();

        Selection.activeGameObject = root;
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
}
