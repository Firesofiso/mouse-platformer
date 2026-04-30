using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ButtonLabel))]
public class ButtonLabelEditor : Editor
{
    private static readonly Dictionary<ButtonLabel.Font, Sprite[]> _fontSprites = new();

    private static int CharToIndex(char c)
    {
        if (c >= 'A' && c <= 'Z') return c - 'A';
        if (c >= 'a' && c <= 'z') return 26 + (c - 'a');
        if (c >= '1' && c <= '9') return 52 + (c - '1');
        if (c == '0') return 61;
        if (c == '!') return 62;
        if (c == '.') return 63;
        if (c == ',') return 64;
        if (c == '#') return 65;
        if (c == '$') return 66;
        if (c == '%') return 67;
        if (c == '&') return 68;
        if (c == '*') return 69;
        if (c == '(') return 70;
        if (c == ')') return 71;
        return -1;
    }

    private static string FontPath(ButtonLabel.Font font) => font switch
    {
        ButtonLabel.Font.Sttuborrn => "Assets/Fonts/sttuborrn/sttuborrn.png",
        ButtonLabel.Font.Odbball   => "Assets/Fonts/odbball/odbball.png",
        _ => ""
    };

    private static string FontPrefix(ButtonLabel.Font font) => font switch
    {
        ButtonLabel.Font.Sttuborrn => "sttuborrn",
        ButtonLabel.Font.Odbball   => "odbball",
        _ => ""
    };

    private static Sprite GetSprite(char c, ButtonLabel.Font font)
    {
        if (!_fontSprites.ContainsKey(font))
            _fontSprites[font] = AssetDatabase.LoadAllAssetsAtPath(FontPath(font))
                .OfType<Sprite>().ToArray();

        int index = CharToIndex(c);
        if (index < 0) return null;
        return _fontSprites[font].FirstOrDefault(s => s.name == $"{FontPrefix(font)}_{index}");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Apply")) Apply((ButtonLabel)target);
    }

    private static void Apply(ButtonLabel label)
    {
        var existingLabel = label.transform.Find("Label");
        if (existingLabel != null) DestroyImmediate(existingLabel.gameObject);

        float gap = label.gap;
        var letters = new List<(Sprite sprite, float width)>();
        float totalWidth = 0;

        foreach (char c in label.text)
        {
            var sprite = GetSprite(c, label.font);
            if (sprite == null) { totalWidth += gap; continue; }
            float width = sprite.rect.width / sprite.pixelsPerUnit;
            letters.Add((sprite, width));
            totalWidth += width + gap;
        }
        if (letters.Count > 0) totalWidth -= gap;

        var container = new GameObject("Label");
        container.transform.SetParent(label.transform, false);
        container.transform.localPosition = label.labelPosition;

        float x = Mathf.Floor(-totalWidth / 2f);
        foreach (var (sprite, width) in letters)
        {
            var go = new GameObject(sprite.name);
            go.transform.SetParent(container.transform, false);
            go.transform.localPosition = new Vector3(Mathf.Floor(x), 0, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            x += width + gap;
        }

        EditorUtility.SetDirty(label.gameObject);
    }
}
