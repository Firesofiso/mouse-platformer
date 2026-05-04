using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Chase : PathfinderState
{
    private static readonly int Run = Animator.StringToHash("Run");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int Fall = Animator.StringToHash("Fall");
    private static readonly int Land = Animator.StringToHash("Land");
    private static readonly int IdleAnim = Animator.StringToHash("Idle");

    private const float LandAnimDuration = 0.1f;
    private bool _wasGrounded;
    private float _landedAt = -1f;

    private ITrolBrainContext Trol => ((MobbTrolUnit)_unit).Trol;
    private SightlineSensor Vision => (SightlineSensor)Sensor;

    public override void Enter() {
        Brain.StartGenerating();
        Brain.StartTraversing();
        _wasGrounded = Trol.IsGrounded;
        _landedAt = -1f;
    }

    public override void Do() {
        UpdateMotionAnimation();

        if (Vision.IsAwareOfTargetPosition) {
            // continue chasing
        } else if (Vision.TargetPermanenceNotElapsed) {
            // pursue
        }
    }

    private void UpdateMotionAnimation() {
        var c = Trol;
        bool grounded = c.IsGrounded;
        if (!_wasGrounded && grounded) _landedAt = Time.time;
        _wasGrounded = grounded;

        if (Time.time < _landedAt + LandAnimDuration)
            PlayAnimation(Land);
        else if (!grounded)
            PlayAnimation(c.Speed.y > 0 ? Jump : Fall);
        else if (c.Speed.x == 0)
            PlayAnimation(IdleAnim);
        else
            PlayAnimation(Run);
    }
}
