using UnityEngine;

[ExecuteAlways]
public class ButtonLabel : MonoBehaviour
{
    [SerializeField] TextMesh _textMesh;
    [SerializeField] string _text;

    private void OnValidate()
    {
        if (_textMesh != null)
            _textMesh.text = _text;
    }
}
