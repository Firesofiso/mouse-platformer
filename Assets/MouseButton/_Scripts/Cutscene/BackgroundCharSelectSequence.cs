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

    public bool IsComplete { get; private set; }

    private Transform _charOriginalParent;
    private Vector3 _charOriginalLocalPos;
    private Quaternion _charOriginalLocalRot;
    private RigidbodyType2D _charOriginalBodyType;
    private Vector3 _cursorOriginalLocalPos;
    private Fadeable[] _allFadeables;
    private Collider2D[] _allColliders;

    void Awake()
    {
        if (_character != null)
        {
            _charOriginalParent = _character.transform.parent;
            _charOriginalLocalPos = _character.transform.localPosition;
            _charOriginalLocalRot = _character.transform.localRotation;
        }
        if (_characterBody != null) _charOriginalBodyType = _characterBody.bodyType;
        if (_cursor != null) _cursorOriginalLocalPos = _cursor.transform.localPosition;
        _allFadeables = GetComponentsInChildren<Fadeable>(true);
        _allColliders = GetComponentsInChildren<Collider2D>(true);
    }

    public void StopActiveSequence() => StopAllCoroutines();

    public void ResetForReuse()
    {
        StopAllCoroutines();
        IsComplete = false;

        if (_character != null)
        {
            _character.transform.SetParent(_charOriginalParent, false);
            _character.transform.localPosition = _charOriginalLocalPos;
            _character.transform.localRotation = _charOriginalLocalRot;
        }
        if (_characterBody != null)
        {
            _characterBody.bodyType = _charOriginalBodyType;
            _characterBody.velocity = Vector2.zero;
            _characterBody.angularVelocity = 0f;
        }
        if (_cursor != null) _cursor.transform.localPosition = _cursorOriginalLocalPos;

        if (_allFadeables != null)
            foreach (var f in _allFadeables) if (f != null) f.SetAlpha(0f);
        if (_allColliders != null)
            foreach (var c in _allColliders) if (c != null) c.enabled = true;
    }

    public void StartFresh() { ResetForReuse(); StartCoroutine(FullSequence()); }

    public void StartAtRandomProgress()
    {
        float r = Random.value;
        if (r < 0.25f)
            StartCoroutine(FullSequence());
        else if (r < 0.75f)
            StartCoroutine(SequenceFromSelect());
        else if (r < 0.90f)
            StartCoroutine(SequenceFromConfirm());
        else
            StartCoroutine(SequenceFromFlyAway());
    }

    private IEnumerator FullSequence()
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

        yield return ConfirmAndFlyAway();
    }

    private IEnumerator SequenceFromSelect()
    {
        yield return null;
        _cursor.SetAlpha(1f);
        int idx = Random.Range(0, _selectionButtons.Length);
        var button = _selectionButtons[idx];
        var pos = ButtonCenter(button);
        pos.z = _cursor.transform.position.z;
        _cursor.transform.position = pos;
        int presses = Random.Range(0, 9);
        for (int i = 0; i < presses; i++)
        {
            button.SimulatePress();
            if (idx == 0) _palette.Prev(); else _palette.Next();
            yield return new WaitForSeconds(Random.Range(_pressPauseMin, _pressPauseMax));
        }
        yield return ConfirmAndFlyAway();
    }

    private IEnumerator SequenceFromConfirm()
    {
        yield return null;
        _cursor.SetAlpha(1f);
        int presses = Random.Range(0, 11);
        for (int i = 0; i < presses; i++) _palette.Next();
        var pos = ButtonCenter(_selectionButtons[Random.Range(0, _selectionButtons.Length)]);
        pos.z = _cursor.transform.position.z;
        _cursor.transform.position = pos;
        yield return ConfirmAndFlyAway();
    }

    private IEnumerator SequenceFromFlyAway()
    {
        yield return null;
        _cursor.SetAlpha(1f);
        int presses = Random.Range(0, 11);
        for (int i = 0; i < presses; i++) _palette.Next();
        var pos = ButtonCenter(_confirmButton);
        pos.z = _cursor.transform.position.z;
        _cursor.transform.position = pos;
        _confirmButton.SimulatePress();
        yield return FlyAwayPhase();
    }

    private IEnumerator ConfirmAndFlyAway()
    {
        yield return MoveCursorTo(ButtonCenter(_confirmButton));
        _confirmButton.SimulatePress();
        yield return FlyAwayPhase();
    }

    private IEnumerator FlyAwayPhase()
    {
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

        IsComplete = true;
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
