using UnityEngine;

[CreateAssetMenu(fileName = "UnitPhysicsStats", menuName = "Units/Physics Stats")]
public class UnitPhysicsStats : ScriptableObject
{
    public float FallAcceleration = 200f;
    public float MaxFallSpeed = 200f;
    public float GroundingForce = -1.5f;
}
