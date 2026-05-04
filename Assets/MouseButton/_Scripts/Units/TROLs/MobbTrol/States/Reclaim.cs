using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Reclaim : PathfinderState
{
    private static readonly int SpearlessIdle = Animator.StringToHash("SpearlessIdle");
    private static readonly int SpearlessRun = Animator.StringToHash("SpearlessRun");
    private static readonly int SpearlessJump = Animator.StringToHash("SpearlessJump");
    private static readonly int SpearlessFall = Animator.StringToHash("SpearlessFall");
    private static readonly int SpearlessLand = Animator.StringToHash("SpearlessLand");

    private const float LandAnimDuration = 0.1f;
    private bool _wasGrounded;
    private float _landedAt = -1f;

    [SerializeField] float _grabDistance = 10f;

    private ITrolBrainContext Trol => ((MobbTrolUnit)_unit).Trol;

    public override void Enter()
    {
        RetargetClosestSpear();
        Brain.StartTraversing();
        _wasGrounded = Trol.IsGrounded;
        _landedAt = -1f;
    }

    public override void Do()
    {
        RetargetClosestSpear();
        UpdateMotionAnimation();

        if (Trol.SpearTransform == null) return;
        float dist = Vector2.Distance(_unit.Rb.position, Trol.SpearTransform.position);
        if (dist < _grabDistance)
            Trol.TriggerGrabSpear(Trol.SpearTransform);
    }

    private void RetargetClosestSpear()
    {
        var closestSpear = Trol.SelectClosestActiveSpear();
        if (closestSpear == null) return;
        Brain.UpdateTarget(closestSpear);
    }

    private void UpdateMotionAnimation() {
        var c = Trol;
        bool grounded = c.IsGrounded;
        if (!_wasGrounded && grounded) _landedAt = Time.time;
        _wasGrounded = grounded;

        if (Time.time < _landedAt + LandAnimDuration)
            PlayAnimation(SpearlessLand);
        else if (!grounded)
            PlayAnimation(c.Speed.y > 0 ? SpearlessJump : SpearlessFall);
        else if (c.Speed.x == 0)
            PlayAnimation(SpearlessIdle);
        else
            PlayAnimation(SpearlessRun);
    }

    public override void Exit()
    {
        Brain.StopTraversing();
        Brain.RestorePrimaryTarget();
    }
}
