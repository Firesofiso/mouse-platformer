using UnityEngine;
using UnityEngine.Tilemaps;

public class BackgroundObject : MonoBehaviour
{
    [Header("Parallax")]
    [Range(0f, 1f)] public float factor = 0.3f;

    [Header("Fog Material")]
    public Material fogMaterial;

    [Header("Fog")]
    public Color fogColor = Color.white;
    [Range(0f, 1f)] public float fogStrength = 0f;

    [Header("Blur")]
    [Range(0f, 8f)] public float blurStrength = 0f;

    [Header("Atmosphere")]
    [Range(0f, 1f)] public float desatAmount = 0f;
    [Range(0f, 1f)] public float contrastAmount = 0f;

    private Vector2 _lastCamPos;
    private static readonly int FogColorId       = Shader.PropertyToID("_FogColor");
    private static readonly int FogStrengthId    = Shader.PropertyToID("_FogStrength");
    private static readonly int BlurStrengthId   = Shader.PropertyToID("_BlurStrength");
    private static readonly int DesatAmountId    = Shader.PropertyToID("_DesatAmount");
    private static readonly int ContrastAmountId = Shader.PropertyToID("_ContrastAmount");

    void Start()
    {
        if (Camera.main != null)
            _lastCamPos = Camera.main.transform.position;
        ApplyFog();
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;
        var camPos = (Vector2)Camera.main.transform.position;
        transform.position += (Vector3)((camPos - _lastCamPos) * factor);
        _lastCamPos = camPos;
    }

    void OnTransformChildrenChanged() => ApplyFog();

    void ApplyFog()
    {
        if (fogMaterial == null) return;

        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(FogColorId, fogColor);
        mpb.SetFloat(FogStrengthId, fogStrength);
        mpb.SetFloat(BlurStrengthId, blurStrength);
        mpb.SetFloat(DesatAmountId, desatAmount);
        mpb.SetFloat(ContrastAmountId, contrastAmount);

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sharedMaterial = fogMaterial;
            sr.SetPropertyBlock(mpb);
        }

        foreach (var tr in GetComponentsInChildren<TilemapRenderer>())
        {
            tr.sharedMaterial = fogMaterial;
            tr.SetPropertyBlock(mpb);
        }
    }
}
