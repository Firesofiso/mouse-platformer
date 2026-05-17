using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TilemapFadeTrigger : MonoBehaviour
{
    [SerializeField] Fadeable _target;
    [SerializeField] float _alphaOnEnter = 0f;
    [SerializeField] float _alphaOnExit  = 1f;

    Coroutine _active;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(_alphaOnEnter);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(_alphaOnExit);
    }

    void Fade(float to)
    {
        if (_target == null) return;
        if (_active != null) _target.StopCoroutine(_active);
        _active = _target.StartCoroutine(_target.FadeTo(to));
    }
}
