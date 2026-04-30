using System;
using System.Collections;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    public enum CursorMode { TrueCursor, Sidekick, FlyAway }

    public static event Action OnClick;
    public static event Action OnRelease;
    public CursorMode Mode = CursorMode.TrueCursor;

    [SerializeField] GameObject target;
    [SerializeField] GameObject cursorVisual;

    [Header("True Cursor")]
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private Collider2D _boundsCollider;

    [Header("Sidekick")]
    public float speed = 1f;
    public int proximityThreshold = 6;
    public bool flipX = false;

    [Header("Fly Away")]
    [SerializeField] private float flyAwayDuration = 1.0f;
    [SerializeField] private Vector3 flyAwayEndOffset = new Vector3(18f, 28f, 0f);
    [SerializeField] private Vector3 flyAwayControlOffset = new Vector3(4f, -10f, 0f);
    [SerializeField] [Range(1f, 5f)] private float flyAwayEasePower = 2f;

    private SpriteRenderer _targetRenderer;

    private Vector3 _offset = new Vector3(8, 3, 0);
    private float _offsetX = 8;
    private Vector2 _virtualCursorPos;

    void Start()
    {
        _targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        SetMode(Mode);
    }

    void Update()
    {
        switch (Mode)
        {
            case CursorMode.TrueCursor: UpdateTrueCursor(); break;
            case CursorMode.Sidekick: UpdateSidekick(); break;
        }
    }

    public void SetMode(CursorMode mode)
    {
        Mode = mode;
        if (mode == CursorMode.TrueCursor)
        {
            var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _virtualCursorPos = new Vector2(wp.x, wp.y);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (mode == CursorMode.FlyAway) StartCoroutine(FlyAway());
    }

    private void UpdateTrueCursor()
    {
        float worldPerPixel = Camera.main.orthographicSize * 2f / Screen.height;
        _virtualCursorPos.x += Input.GetAxisRaw("Mouse X") * worldPerPixel * _sensitivity;
        _virtualCursorPos.y += Input.GetAxisRaw("Mouse Y") * worldPerPixel * _sensitivity;

        if (_boundsCollider != null)
        {
            var b = _boundsCollider.bounds;
            _virtualCursorPos.x = Mathf.Clamp(_virtualCursorPos.x, b.min.x, b.max.x);
            _virtualCursorPos.y = Mathf.Clamp(_virtualCursorPos.y, b.min.y, b.max.y);
        }

        transform.position = new Vector3(Mathf.Round(_virtualCursorPos.x), Mathf.Round(_virtualCursorPos.y), 0f);

        if (Input.GetMouseButtonDown(0)) OnClick?.Invoke();
        if (Input.GetMouseButtonUp(0)) OnRelease?.Invoke();
    }

    private void UpdateSidekick()
    {
        var targetPosition = target.transform.position;
        var distance = Vector3.Distance(transform.position, targetPosition + _offset);
        var nextPosition = Vector3.MoveTowards(transform.position, targetPosition + _offset, speed * Time.deltaTime * distance);

        if (distance < proximityThreshold)
        {
            if (flipX != _targetRenderer.flipX)
                nextPosition.x += flipX ? _offsetX / 2 : -_offsetX / 2;
            flipX = _targetRenderer.flipX;
        }
        else if (!flipX && nextPosition.x > target.transform.position.x + _offset.x) flipX = true;
        else if (flipX && nextPosition.x < target.transform.position.x + _offset.x) flipX = false;

        if (flipX && _offset.x > -8)
        {
            if (_offset.x > 0) _offset.x = 0;
            _offset.x = Mathf.MoveTowards(_offset.x, -8, Time.deltaTime * 20);
        }
        else if (!flipX && _offset.x < 8)
        {
            if (_offset.x < 0) _offset.x = 0;
            _offset.x = Mathf.MoveTowards(_offset.x, 8, Time.deltaTime * 20);
        }

        transform.position = nextPosition;
    }

    private IEnumerator FlyAway()
    {
        yield return MotionUtils.BezierMove(transform, flyAwayControlOffset, flyAwayEndOffset, flyAwayDuration, flyAwayEasePower);
        gameObject.SetActive(false);
    }
}
