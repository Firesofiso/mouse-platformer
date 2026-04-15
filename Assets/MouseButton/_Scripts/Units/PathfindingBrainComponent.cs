using Pathfinding;
using UnityEngine;

public abstract class PathfindingBrainComponent : MonoBehaviour
{
    public PathfindingBrain Brain;
    public StatefulUnit Unit => Brain.unit;

    protected void Awake()
    {
        Brain.Think += Think;
    }

    protected abstract void Think();
}

public abstract class TargetDetectionSensor : PathfindingBrainComponent
{
    [SerializeField]
    internal int _detectionRange = 200;
    internal float _perceivedDistanceToTarget = float.PositiveInfinity;
    public float PerceivedDistanceToTarget
    {
        get => _perceivedDistanceToTarget;
    }
    public bool isAwareOfTargetPosition
    {
        get => _perceivedDistanceToTarget != float.PositiveInfinity;
    }
}