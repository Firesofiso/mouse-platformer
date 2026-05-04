using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Style Config")]
public class DialogueStyleConfig : ScriptableObject
{
    [Serializable]
    public class StyleEntry
    {
        public DialogueStyle style;
        public Sprite bubbleSprite;
        public Color textColor = Color.black;
        public float fontScale = 1f;
        public float charsPerSecond = 30f;
        public bool shake;
        public float shakeAmplitude = 0.05f;
        [Space]
        public bool overrideOffset;
        public Vector2 bubbleOffset = new(0f, 1f);
    }

    public List<StyleEntry> entries = new();

    public StyleEntry Get(DialogueStyle s)
    {
        foreach (var e in entries) if (e.style == s) return e;
        return entries.Count > 0 ? entries[0] : null;
    }
}
