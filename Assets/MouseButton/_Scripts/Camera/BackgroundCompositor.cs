using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class BackgroundCompositor : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public RenderTexture renderTexture;
        public Material      material;
        public string        sortingLayer = "Default";
        public int           sortingOrder = -100;
        [HideInInspector] public MeshRenderer quad;
    }

    public Layer[] layers;

    Camera _cam;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        BuildQuads();
    }

    void OnDisable()
    {
        foreach (var layer in layers)
        {
            if (layer.quad != null)
                Destroy(layer.quad.gameObject);
            layer.quad = null;
        }
    }

    void LateUpdate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        float h = _cam.orthographicSize;
        float w = h * _cam.aspect;

        var verts = new Vector3[]
        {
            new Vector3(-w, -h, 0),
            new Vector3( w, -h, 0),
            new Vector3(-w,  h, 0),
            new Vector3( w,  h, 0),
        };

        foreach (var layer in layers)
        {
            if (layer.quad == null) continue;
            var pos = transform.position;
            layer.quad.transform.position = new Vector3(pos.x, pos.y, pos.z + 100f);
            layer.quad.GetComponent<MeshFilter>().mesh.vertices = verts;
        }
    }

    void BuildQuads()
    {
        foreach (var layer in layers)
        {
            if (layer.renderTexture == null || layer.material == null) continue;

            var go = new GameObject($"BGQuad_{layer.sortingOrder}");
            go.transform.SetParent(transform, false);

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = BuildQuadMesh();

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = layer.material;
            mr.sortingLayerName = layer.sortingLayer;
            mr.sortingOrder = layer.sortingOrder;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            layer.material.mainTexture = layer.renderTexture;
            layer.quad = mr;
        }
    }

    Mesh BuildQuadMesh()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        float h = _cam.orthographicSize;
        float w = h * _cam.aspect;

        var mesh = new Mesh { name = "BGQuad" };
        mesh.vertices = new Vector3[]
        {
            new Vector3(-w, -h, 0),
            new Vector3( w, -h, 0),
            new Vector3(-w,  h, 0),
            new Vector3( w,  h, 0),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
