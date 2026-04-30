using System.Collections;
using UnityEngine;

public class BackgroundCharSelectSequence : MonoBehaviour
{
    [SerializeField] Fadeable _window;
    [SerializeField] Fadeable _cursor;
    [SerializeField] MousePalette _palette;
    [SerializeField] WorldButton[] _selectionButtons;
    [SerializeField] WorldButton _confirmButton;
    [SerializeField] IdleAnimator _character;
    [SerializeField] Rigidbody2D _characterBody;

    [Header("Timing")]
    [SerializeField] float _cursorFadeIn = 0.3f;
    [SerializeField] float _windowFadeOut = 0.4f;
    [SerializeField] float _pressPauseMin = 0.2f;
    [SerializeField] float _pressPauseMax = 0.6f;
    [SerializeField] float _cursorMoveSpeedMin = 7f;
    [SerializeField] float _cursorMoveSpeedMax = 16f;

    [Header("Cursor Flyaway")]
    [SerializeField] float _flyAwayDuration = 1.0f;
    [SerializeField] Vector3 _flyAwayEndOffset = new Vector3(18f, 28f, 0f);
    [SerializeField] Vector3 _flyAwayEndVariance = new Vector3(10f, 6f, 0f);
    [SerializeField] Vector3 _flyAwayControlOffset = new Vector3(4f, -10f, 0f);
    [SerializeField] Vector3 _flyAwayControlVariance = new Vector3(6f, 4f, 0f);
    [SerializeField] [Range(1f, 5f)] float _flyAwayEasePower = 2f;

    void OnEnable() => StartCoroutine(Sequence());

    private IEnumerator Sequence()
    {
        yield return null;
        _cursor.SetAlpha(0f);
        int r = Random.Range(0, 11);
        yield return _cursor.FadeTo(1f, _cursorFadeIn);

        if (r > 0)
        {
            int idx = Random.Range(0, _selectionButtons.Length);
            var button = _selectionButtons[idx];
            yield return MoveCursorTo(ButtonCenter(button));
            for (int i = 0; i < r; i++)
            {
                button.SimulatePress();
                if (idx == 0) _palette.Prev(); else _palette.Next();
                yield return new WaitForSeconds(Random.Range(_pressPauseMin, _pressPauseMax));
            }
        }

        yield return MoveCursorTo(ButtonCenter(_confirmButton));
        _confirmButton.SimulatePress();

        _character.transform.SetParent(transform.parent);
        _characterBody.bodyType = RigidbodyType2D.Dynamic;
        _character.PlayFall();

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        var flyaway = StartCoroutine(MotionUtils.BezierMove(_cursor.transform, Randomize(_flyAwayControlOffset, _flyAwayControlVariance), Randomize(_flyAwayEndOffset, _flyAwayEndVariance), _flyAwayDuration, _flyAwayEasePower));
        var windowFade = StartCoroutine(_window.FadeTo(0f, _windowFadeOut));
        var cursorFade = StartCoroutine(_cursor.FadeTo(0f, _flyAwayDuration));
        var characterFade = StartCoroutine(_character.GetComponent<Fadeable>().FadeTo(0f, _windowFadeOut));
        yield return flyaway;
        Cursor.visible = false;
        yield return windowFade;
        yield return cursorFade;
        yield return characterFade;

        Destroy(_character.gameObject);
        Destroy(gameObject);
    }

    private static Vector3 Randomize(Vector3 offset, Vector3 variance) => offset + new Vector3(
        Random.Range(-variance.x, variance.x),
        Random.Range(-variance.y, variance.y), 0f);

    private static Vector3 ButtonCenter(WorldButton button) =>
        button.CursorTarget + new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);

    private IEnumerator MoveCursorTo(Vector3 target)
    {
        target.z = _cursor.transform.position.z;
        float speed = Random.Range(_cursorMoveSpeedMin, _cursorMoveSpeedMax);
        while (Vector3.Distance(_cursor.transform.position, target) > 0.05f)
        {
            _cursor.transform.position = Vector3.MoveTowards(
                _cursor.transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        _cursor.transform.position = target;
    }
}
