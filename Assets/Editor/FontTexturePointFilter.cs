using UnityEditor;
using UnityEngine;

// Forces Point filtering on sttuborrn font texture after reimport and on every domain reload.
[InitializeOnLoad]
public class FontTexturePointFilter : AssetPostprocessor
{
    static FontTexturePointFilter() => ApplyPointFilter();

    static void OnPostprocessAllAssets(
        string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
            if (path.EndsWith("sttuborrn.ttf")) { ApplyPointFilter(); break; }
    }

    static void ApplyPointFilter()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/sttuborrn/sttuborrn.ttf");
        var tex = font?.material?.mainTexture;
        if (tex != null && tex.filterMode != FilterMode.Point)
            tex.filterMode = FilterMode.Point;
    }
}
