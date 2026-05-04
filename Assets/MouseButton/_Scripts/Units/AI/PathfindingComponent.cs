using UnityEngine;

// Base for any component that runs perception logic on a unit each frame.
// Works on any StatefulUnit — not just AI. Assign Unit in the inspector.
public abstract class PathfindingComponent : MonoBehaviour
{
    [SerializeField] protected StatefulUnit Unit;

    protected void Awake()
    {
        if (Unit == null) Unit = GetComponentInParent<StatefulUnit>();
        Unit.Think += Think;
    }

    protected abstract void Think();
}
