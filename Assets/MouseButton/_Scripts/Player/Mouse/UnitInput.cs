using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TarodevController {
    public class UnitInput : MonoBehaviour {
        public FrameInput FrameInput { get; private set; }
        [SerializeField] public bool isPlayerUnit = false;

        private void Update() => FrameInput = Gather();

#if ENABLE_INPUT_SYSTEM && isPlayerUnit
        private PlayerInputActions _actions;
        private InputAction _move, _jump, _drop, _dash, _attack, _click;

        private void Awake() {
            _actions = new PlayerInputActions();
            _move = _actions.Player.Move;
            _jump = _actions.Player.Jump;
            _drop = _actions.Player.Drop;
            _dash = _actions.Player.Dash;
            _attack = _actions.Player.Attack;
            _click = _action.Player.Click;
        }

        private void OnEnable() => _actions.Enable();

        private void OnDisable() => _actions.Disable();

        private FrameInput Gather() {
            return new FrameInput {
                JumpDown = _jump.WasPressedThisFrame(),
                JumpHeld = _jump.IsPressed(),
                DropDown = _drop.WasPressedThisFrame(),
                DashDown = _dash.WasPressedThisFrame(),
                AttackDown = _attack.WasPressedThisFrame(),
                ClickDown = _click.wasPressedThisFrame(),
                Move = _move.ReadValue<Vector2>()
            };
        }

#elif ENABLE_LEGACY_INPUT_MANAGER
        private FrameInput Gather() {
            return new FrameInput {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                DropDown = (Input.GetButtonDown("Jump") && Input.GetAxisRaw("Vertical") < 0) || (Input.GetKeyDown(KeyCode.C) && Input.GetAxisRaw("Vertical") < 0),
                DashDown = Input.GetKeyDown(KeyCode.X),
                AttackDown = Input.GetKeyDown(KeyCode.Z),
                ClickDown = Input.GetKeyDown(KeyCode.M),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            };
        }
#endif
    }

    public struct FrameInput {
        public Vector2 Move;
        public bool JumpDown;
        public bool JumpHeld;
        public bool DropDown;
        public bool DashDown;
        public bool AttackDown;
        public bool ClickDown;
    }
}