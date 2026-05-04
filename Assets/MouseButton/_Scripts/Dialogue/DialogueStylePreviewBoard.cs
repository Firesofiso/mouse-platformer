using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DialogueStylePreviewBoard : MonoBehaviour
{
    [SerializeField] DialogueBubble _bubblePrefab;
    [SerializeField] DialogueStyleConfig _styleConfig;
    [SerializeField] string _sampleText = "Squeak!";
    [SerializeField] float _spacing = 12f;

    readonly List<DialogueBubble> _previews = new();

    void OnEnable() => Rebuild();
    void OnDisable() => Teardown();
    void OnDestroy() => Teardown();

#if UNITY_EDITOR
    void OnValidate() =>
        EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
#endif

    void Rebuild()
    {
        Teardown();
        if (_bubblePrefab == null || _styleConfig == null) return;

        for (int i = 0; i < _styleConfig.entries.Count; i++)
        {
            var entry = _styleConfig.entries[i];
            var go = Instantiate(_bubblePrefab.gameObject, transform);
            go.name = $"Preview_{entry.style}";
            go.transform.localPosition = new Vector3(0f, i * _spacing, 0f);

            var bubble = go.GetComponent<DialogueBubble>();
            bubble.Show(_sampleText, entry);
            bubble.CompleteReveal();
            _previews.Add(bubble);
        }
    }

    void Teardown()
    {
        foreach (var b in _previews)
            if (b != null) DestroyImmediate(b.gameObject);
        _previews.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
}
