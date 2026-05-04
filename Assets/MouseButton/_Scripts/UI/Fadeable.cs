using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Fadeable : MonoBehaviour
{
    [SerializeField] public float duration = 0.4f;

    private SpriteRenderer[] _sprites;
    private Tilemap[] _tilemaps;
    private TextMesh[] _textMeshes;

    void Awake() => Collect();

    void Collect()
    {
        _sprites    ??= GetComponentsInChildren<SpriteRenderer>();
        _tilemaps   ??= GetComponentsInChildren<Tilemap>();
        _textMeshes ??= GetComponentsInChildren<TextMesh>();
    }

    public IEnumerator FadeTo(float to) => FadeTo(to, duration);
    public IEnumerator FadeTo(float to, float duration)
    {
        Collect();

        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float fromSprite = _sprites.Length > 0 ? _sprites[0].color.a : to;
        float fromTilemap = _tilemaps.Length > 0 ? _tilemaps[0].color.a : to;
        float fromText = _textMeshes.Length > 0 ? _textMeshes[0].color.a : to;
        Debug.Log("fading Sprites from " + fromSprite + " to " + to);
        Debug.Log("fading Tilemaps from " + fromTilemap + " to " + to);
        Debug.Log("fading Text from " + fromText + " to " + to);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetSpriteAlpha(Mathf.Lerp(fromSprite, to, t));
            SetTilemapAlpha(Mathf.Lerp(fromTilemap, to, t));
            SetTextAlpha(Mathf.Lerp(fromText, to, t));
            yield return null;
        }
        SetAlpha(to);
    }

    public void SetAlpha(float a)
    {
        Collect();
        SetSpriteAlpha(a);
        SetTilemapAlpha(a);
        SetTextAlpha(a);
    }

    public void SetSpriteAlpha(float a)
    {
        Collect();
        foreach (var sr in _sprites)  { var c = sr.color; c.a = a; sr.color = c; }
    }

    public void SetTilemapAlpha(float a)
    {
        Collect();
        foreach (var tm in _tilemaps) { var c = tm.color; c.a = a; tm.color = c; }
    }

    public void SetTextAlpha(float a)
    {
        Collect();
        foreach (var t in _textMeshes) { var c = t.color; c.a = a; t.color = c; }
    }
}
