using UnityEngine;

public class PixelCloudDisruptorManager : MonoBehaviour
{
    private static PixelCloudDisruptorManager _instance;
    private static readonly Vector4[] _buf = new Vector4[4];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("PixelCloudDisruptorManager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<PixelCloudDisruptorManager>();
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        int count = 0;
        if (cam != null)
        {
            var all = PixelCloudDisruptor.All;
            int n = Mathf.Min(all.Count, 4);
            for (int i = 0; i < n; i++)
            {
                var d = all[i];
                if (d == null) continue;
                Vector3 vp = cam.WorldToViewportPoint(d.transform.position);
                _buf[count++] = new Vector4(vp.x, vp.y, d.radius, d.strength);
            }
        }
        // Zero unused slots
        for (int i = count; i < 4; i++) _buf[i] = Vector4.zero;
        Shader.SetGlobalInt("_DisruptorCount", count);
        Shader.SetGlobalVectorArray("_Disruptors", _buf);
    }
}
