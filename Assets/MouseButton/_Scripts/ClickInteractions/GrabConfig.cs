using UnityEngine;

[CreateAssetMenu(fileName = "GrabConfig", menuName = "Mouse Platformer/Grab Config")]
public class GrabConfig : ScriptableObject
{
    [Tooltip("Max force the cursor can apply. Determines what it can actually move.")]
    public float maxForce = 100f;

    [Tooltip("Spring oscillation frequency. Higher = snappier pull.")]
    public float frequency = 2f;

    [Tooltip("0 = bouncy, 1 = critically damped.")]
    [Range(0f, 1f)]
    public float dampingRatio = 0.7f;
}
