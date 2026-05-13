using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Fadeable : MonoBehaviour
{
    [SerializeField] public float duration = 0.4f;
    [SerializeField] AnimationCurve _ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private SpriteRenderer[] _sprites;
    private Tilemap[]        _tilemaps;
    private TextMesh[]       _textMeshes;

    void Awake() => Collect();

    void Collect()
    {
        _sprites    ??= GetComponentsInChildren<SpriteRenderer>();
        _tilemaps   ??= GetComponentsInChildren<Tilemap>();
        _textMeshes ??= GetComponentsInChildren<TextMesh>();
    }

    // ── Public API ────────────────────────────────────────────────────

    public IEnumerator FadeTo(float to)              => FadeTo(to, duration);
    public IEnumerator FadeTo(float to, float dur)
    {
        Collect();
        if (dur <= 0f) { SetAlpha(to); yield break; }

        float fromSprite  = _sprites.Length    > 0 ? _sprites[0].color.a    : to;
        float fromTilemap = _tilemaps.Length   > 0 ? _tilemaps[0].color.a   : to;
        float fromText    = _textMeshes.Length > 0 ? _textMeshes[0].color.a : to;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = _ease.Evaluate(Mathf.Clamp01(elapsed / dur));
            SetSpriteAlpha (Mathf.Lerp(fromSprite,  to, t));
            SetTilemapAlpha(Mathf.Lerp(fromTilemap, to, t));
            SetTextAlpha   (Mathf.Lerp(fromText,    to, t));
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

    // ── Alpha helpers ─────────────────────────────────────────────────

    public void SetSpriteAlpha(float a)
    {
        Collect();
        foreach (var sr in _sprites)   { if (sr == null) continue; var c = sr.color; c.a = a; sr.color = c; }
    }

    public void SetTilemapAlpha(float a)
    {
        Collect();
        foreach (var tm in _tilemaps)  { if (tm == null) continue; var c = tm.color; c.a = a; tm.color = c; }
    }

    public void SetTextAlpha(float a)
    {
        Collect();
        foreach (var t in _textMeshes) { if (t == null) continue; var c = t.color; c.a = a; t.color = c; }
    }
}
