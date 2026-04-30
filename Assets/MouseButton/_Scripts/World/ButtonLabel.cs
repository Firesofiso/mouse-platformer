using UnityEngine;

public class ButtonLabel : MonoBehaviour
{
    public enum Font { Sttuborrn, Odbball }

    [SerializeField] public Font font;
    [SerializeField] public string text;
    [SerializeField] public float gap = 1f;
    [SerializeField] public Vector3 labelPosition;
}
