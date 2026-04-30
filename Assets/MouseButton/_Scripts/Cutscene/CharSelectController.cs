using System.Collections;
using TarodevController;
using UnityEngine;

public class CharSelectController : MonoBehaviour
{
    [SerializeField] MousePalette _palette;
    [SerializeField] UnitInput _playerInput;
    [SerializeField] PlayerController _playerController;
    [SerializeField] CursorController _cursor;
    [SerializeField] Fadeable _fadeable;
    [SerializeField] float _fadeDuration = 0.4f;
    [SerializeField] PlatformCharSelectGridManager _gridManager;

    WorldButton[] _buttons;

    void Start()
    {
        _playerInput.enabled = false;
        _playerController.IsInCharacterSelect = true;
        _cursor.SetMode(CursorController.CursorMode.TrueCursor);
        _buttons = GetComponentsInChildren<WorldButton>();
    }

    public void OnPrev() => _palette.Prev();
    public void OnNext() => _palette.Next();

    public void OnConfirm()
    {
        _playerController.transform.SetParent(null);
        _playerController.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        _playerController.IsInCharacterSelect = false;
        _playerInput.enabled = true;
        _cursor.SetMode(CursorController.CursorMode.FlyAway);
        StartCoroutine(ConfirmSequence());
    }

    private IEnumerator ConfirmSequence()
    {
        foreach (var btn in _buttons) btn.enabled = false;
        yield return _fadeable.FadeTo(0f, _fadeDuration);
        _gridManager.StartCenterSlot();
        gameObject.SetActive(false);
    }
}
