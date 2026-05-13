using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour, ICutsceneParticipant
{
    public enum CursorMode { TrueCursor, Sidekick, FlyAway }

    public static CursorController Instance { get; private set; }
    public static event Action OnClick;
    public static event Action OnRelease;
    public static Vector2 CursorTargetPosition
    {
        get
        {
            if (Instance == null) return Vector2.zero;
            if (Instance.Mode == CursorMode.TrueCursor) return Instance._virtualCursorPos;
            return (Vector2)(Instance.sidekickTarget.transform.position + Instance._sidekickOffset);
        }
    }
    public static Vector2 CarryTargetPosition => Instance != null
        ? (Vector2)(Instance.sidekickTarget.transform.position + Instance._carryOffset)
        : Vector2.zero;
    public CursorMode Mode = CursorMode.TrueCursor;

    #region ICutsceneParticipant

    public string ParticipantId => "Cursor";
    public Transform Transform => transform;

    public IEnumerator MoveTo(Vector2 worldPosition)
    {
        while (Vector2.Distance(transform.position, worldPosition) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, worldPosition, speed * Time.deltaTime);
            yield return null;
        }
    }

    public void PlayEmote(string emoteId) => _cursorAnimator.PlayEmote(emoteId);

    public void FaceTowards(Vector2 worldPosition) => _cursorAnimator.FaceTowards(worldPosition);

    public void Stop() { }

    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (_cursorAnimator == null) _cursorAnimator = GetComponentInChildren<CursorAnimator>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        if (CutsceneManager.instance != null) CutsceneManager.instance.Register(this);
    }

    void OnDisable()
    {
        if (CutsceneManager.instance != null) CutsceneManager.instance.Unregister(this);
    }

    [SerializeField] GameObject sidekickTarget;
    [SerializeField] GameObject cursorVisual;

    [Header("True Cursor")]
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private Collider2D _boundsCollider;

    [Header("Sidekick")]
    public float speed = 1f;
    public int proximityThreshold = 6;
    public bool flipX = false;

    [Header("Fly Away")]
    [SerializeField] private CursorAnimator _cursorAnimator;
    [SerializeField] private CutsceneSequence _flyAwaySequence;

    [Header("Interaction")]
    [SerializeField] InteractionManager _interactionManager;
    [SerializeField] float _interactSmoothTime = 0.08f;

    [Header("Grab")]
    [SerializeField] CursorGrabber _grabber;
    [SerializeField] Vector3 _carryOffset = new Vector3(8, 3, 0);

    private SpriteRenderer _targetRenderer;

    [SerializeField]
    private Vector3 _sidekickOffset = new Vector3(8, 3, 0);
    private float _sidekickOffsetX = 8;
    private Vector2 _virtualCursorPos;
    private Vector3 _cursorVelocity;

    void Start()
    {
        _targetRenderer = sidekickTarget.GetComponentInChildren<SpriteRenderer>();
        SetMode(Mode);
    }

    void Update()
    {
        if (CursorGrabber.IsGrabbing && CursorGrabber.CurrentHeldTransform != null)
        {
            PinToHeldItem();
            HandleInput();
            return;
        }

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

    void PinToHeldItem()
    {
        transform.position = new Vector3(
            Mathf.Round(_grabber.GrabPoint.x),
            Mathf.Round(_grabber.GrabPoint.y), 0f);
        _cursorVelocity = Vector3.zero;
    }

    void FinalizePosition(Vector3 freePos)
    {
        if (InteractionManager.HasInteractionTarget)
        {
            var targetPos = _interactionManager.CurrentTarget.IconWorldPosition;
            var newPos = Vector3.SmoothDamp(transform.position, targetPos, ref _cursorVelocity, _interactSmoothTime);
            transform.position = newPos;
        }
        else
        {
            _cursorVelocity = Vector3.zero;
            transform.position = freePos;
        }
    }

    void HandleInput()
    {
        bool click   = Mode == CursorMode.TrueCursor ? Input.GetMouseButtonDown(0) : Input.GetKeyDown(KeyCode.M);
        bool release = Mode == CursorMode.TrueCursor ? Input.GetMouseButtonUp(0)   : Input.GetKeyUp(KeyCode.M);
        if (click)   OnClick?.Invoke();
        if (release) OnRelease?.Invoke();
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

        FinalizePosition(new Vector3(Mathf.Round(_virtualCursorPos.x), Mathf.Round(_virtualCursorPos.y), 0f));
        HandleInput();
    }

    private void UpdateSidekick()
    {
        var home = sidekickTarget.transform.position + _sidekickOffset;
        var distance = Vector3.Distance(transform.position, home);
        var nextPosition = Vector3.MoveTowards(transform.position, home, speed * Time.deltaTime * distance);

        if (distance < proximityThreshold)
        {
            if (flipX != _targetRenderer.flipX)
                nextPosition.x += flipX ? _sidekickOffsetX / 2 : -_sidekickOffsetX / 2;
            flipX = _targetRenderer.flipX;
        }
        else if (!flipX && nextPosition.x > home.x) flipX = true;
        else if (flipX && nextPosition.x < home.x) flipX = false;

        if (flipX && _sidekickOffset.x > -8)
        {
            if (_sidekickOffset.x > 0) _sidekickOffset.x = 0;
            _sidekickOffset.x = Mathf.MoveTowards(_sidekickOffset.x, -8, Time.deltaTime * 20);
        }
        else if (!flipX && _sidekickOffset.x < 8)
        {
            if (_sidekickOffset.x < 0) _sidekickOffset.x = 0;
            _sidekickOffset.x = Mathf.MoveTowards(_sidekickOffset.x, 8, Time.deltaTime * 20);
        }

        FinalizePosition(nextPosition);
        HandleInput();
    }

    private IEnumerator FlyAway()
    {
        if (_grabber != null && CursorGrabber.IsGrabbing)
            OnRelease?.Invoke();

        var sequence = _flyAwaySequence;
        if (sequence == null) sequence = BuildDefaultFlyAway();

        if (CutsceneManager.instance != null)
        {
            CutsceneManager.instance.Register(this);
            yield return CutsceneManager.instance.PlayAndReturn(sequence);
        }
        else
            Debug.LogWarning("CursorController: No CutsceneManager in scene — fly-away skipped.");
    }

    private static CutsceneSequence BuildDefaultFlyAway()
    {
        var seq = ScriptableObject.CreateInstance<CutsceneSequence>();
        seq.beats = new List<CutsceneBeat>
        {
            // Follow player from a distance (async — runs alongside emotes)
            new CutsceneBeat
            {
                type = BeatType.FollowTarget,
                speakerId = "Cursor",
                followTargetId = "Player",
                followOffset = new Vector3(12f, 10f, 0f),
                followSpeed = 5f,
                duration = 4.0f,
                async = true,
            },
            // Drift for a bit before emoting
            new CutsceneBeat
            {
                type = BeatType.Wait,
                duration = 2.0f,
            },
            // Surprise — realizing it's time to go
            new CutsceneBeat
            {
                type = BeatType.Emote,
                speakerId = "Cursor",
                emoteId = "surprise",
                duration = 0.6f,
            },
            // Frown — sad to leave
            new CutsceneBeat
            {
                type = BeatType.Emote,
                speakerId = "Cursor",
                emoteId = "frown",
                duration = 1.0f,
            },
            // Back to idle
            new CutsceneBeat
            {
                type = BeatType.Emote,
                speakerId = "Cursor",
                emoteId = "idle",
            },
            // Fly away
            new CutsceneBeat
            {
                type = BeatType.BezierMove,
                speakerId = "Cursor",
                bezierControlOffset = new Vector3(-6f, 12f, 0f),
                bezierEndOffset = new Vector3(30f, 40f, 0f),
                bezierEasePower = 2f,
                duration = 2.0f,
            },
            // Deactivate
            new CutsceneBeat
            {
                type = BeatType.SetActive,
                speakerId = "Cursor",
                activeState = false,
            },
        };
        return seq;
    }
}
