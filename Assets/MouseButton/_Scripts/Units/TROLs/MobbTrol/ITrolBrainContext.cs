using UnityEngine;

namespace TarodevController.Trol
{
    // Surface area that MobbTrol states require from their unit's controller.
    // States depend on this interface, not MobbTrolController directly, so future
    // trol variants can implement the contract without subclassing.
    public interface ITrolBrainContext
    {
        bool IsGrounded { get; }
        Vector2 Speed { get; }

        bool IsAiming { get; set; }
        void LaunchSpear();

        bool Spearless { get; }
        bool LastThrowTripped { get; }
        void SetRecovering();

        Transform SpearTransform { get; }
        Transform SelectClosestActiveSpear();
        void TriggerGrabSpear(Transform spearTransform);

        bool MustCelebrate { get; set; }
        bool IsDancing { get; }
        void SetDancing(int durationInSeconds);
    }
}
