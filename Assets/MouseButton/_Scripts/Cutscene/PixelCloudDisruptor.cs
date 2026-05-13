using UnityEngine;

/// <summary>
/// Attach to any GameObject to make it push dissolving pixel-cloud sprites away.
/// Registered globally; PixelCloudDisruptorManager pushes positions to the shader.
/// </summary>
public class PixelCloudDisruptor : MonoBehaviour
{
    [SerializeField] public float radius   = 0.08f;  // screen UV units (~8% of screen width)
    [SerializeField] public float strength = 1.0f;

    public static readonly System.Collections.Generic.List<PixelCloudDisruptor> All
        = new System.Collections.Generic.List<PixelCloudDisruptor>();

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);
}
