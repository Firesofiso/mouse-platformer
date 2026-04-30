using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Fadeable : MonoBehaviour
{
    [SerializeField] public float duration = 0.4f;

    private SpriteRenderer[] _sprites;
    private Tilemap[] _tilemaps;

    void Awake()
    {
        _sprites = GetComponentsInChildren<SpriteRenderer>();
        _tilemaps = GetComponentsInChildren<Tilemap>();
    }

    public IEnumerator FadeTo(float to) => FadeTo(to, duration);
    public IEnumerator FadeTo(float to, float duration)
    {
        float from = _sprites.Length > 0 ? _sprites[0].color.a : _tilemaps[0].color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    public void SetAlpha(float a)
    {
        foreach (var sr in _sprites) { var c = sr.color; c.a = a; sr.color = c; }
        foreach (var tm in _tilemaps) { var c = tm.color; c.a = a; tm.color = c; }
    }
}
