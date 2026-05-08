using UnityEngine;

[CreateAssetMenu(fileName = "PhysicsGrabConfig", menuName = "Mouse Platformer/Physics Grab Config")]
public class PhysicsGrabConfig : ScriptableObject
{
    [Tooltip("Max force the cursor can apply. Determines what it can actually move.")]
    public float maxForce = 100f;

    [Tooltip("Spring oscillation frequency. Higher = snappier pull.")]
    public float frequency = 2f;

    [Tooltip("0 = bouncy, 1 = critically damped.")]
    [Range(0f, 1f)]
    public float dampingRatio = 0.7f;

    [Tooltip("If false, the cursor cannot raise the object's y while grabbing.")]
    public bool cursorCanLift = true;
}
