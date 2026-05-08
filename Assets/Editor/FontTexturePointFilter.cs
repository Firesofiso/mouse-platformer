using UnityEditor;
using UnityEngine;

// Forces Point filtering on pixel font textures after reimport and on every domain reload.
[InitializeOnLoad]
public class FontTexturePointFilter : AssetPostprocessor
{
    static readonly string[] FontPaths =
    {
        "Assets/Fonts/sttuborrn/sttuborrn.ttf",
        "Assets/Fonts/odbball/oodbbaal.ttf",
    };

    static FontTexturePointFilter() => ApplyPointFilter();

    static void OnPostprocessAllAssets(
        string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
            foreach (var fp in FontPaths)
                if (path.EndsWith(fp)) { ApplyPointFilter(); return; }
    }

    static void ApplyPointFilter()
    {
        foreach (var fp in FontPaths)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(fp);
            var tex = font?.material?.mainTexture;
            if (tex != null && tex.filterMode != FilterMode.Point)
                tex.filterMode = FilterMode.Point;
        }
    }
}
