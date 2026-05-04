using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GuineaPigPalette : MonoBehaviour
{
    Material _material;
    [SerializeField] int _currPalette = 0;

    static readonly Dictionary<string, string[]> _palettes = new Dictionary<string, string[]>
    {
        // fur light, fur mid, fur dark
        { "tortie",     new[] { "#d9a066", "#8f563b", "#45283c" } },
        { "dalmation",     new[] { "#ffffff", "#8f563b", "#45283c" } },
        { "dutch",     new[] { "#d9a066", "#8f563b", "#d9a066" } },
        { "solid",     new[] { "#ffffff", "#d6d6d6", "#ffffff" } },
    };

    static readonly int FurLightId = Shader.PropertyToID("_furColorLight");
    static readonly int FurMidId   = Shader.PropertyToID("_furColorMid");
    static readonly int FurDarkId  = Shader.PropertyToID("_furColorDark");

    public void Prev()
    {
        _currPalette = (_currPalette - 1 + _palettes.Count) % _palettes.Count;
        SwapPalette(_currPalette);
    }

    public void Next()
    {
        _currPalette = (_currPalette + 1) % _palettes.Count;
        SwapPalette(_currPalette);
    }

    void Start()
    {
        _material = GetComponent<Renderer>().material;
        SwapPalette(_currPalette);
    }

    void Update()
    {
        SwapPalette(_currPalette);
    }

    void SwapPalette(int p)
    {
        var palette = _palettes.ElementAt(p).Value;
        _material.SetColor(FurLightId, Parse(palette[0]));
        _material.SetColor(FurMidId,   Parse(palette[1]));
        _material.SetColor(FurDarkId,  Parse(palette[2]));
    }

    static Color Parse(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
