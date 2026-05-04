using System.Collections;
using TarodevController;
using UnityEngine;

// Root state for cutscene/scripted control. Drives movement via coroutine commands.
// Subclass and override Sequence() to author a scripted beat.
// Behavioral substates (e.g. Recover) still tick and filter input during the sequence.
public abstract class ScriptedState : State, IInputFilter
{
    FrameInput _scripted;
    Coroutine _sequence;

    public override void Enter()
    {
        _scripted = default;
        _sequence = _unit.StartCoroutine(RunSequence());
    }

    public override void Exit()
    {
        if (_sequence != null) _unit.StopCoroutine(_sequence);
        _scripted = default;
    }

    public void FilterInput(ref FrameInput input) => input = _scripted;

    IEnumerator RunSequence()
    {
        yield return Sequence();
        IsComplete = true;
    }

    // Override to author the scripted sequence
    protected abstract IEnumerator Sequence();

    // --- Command primitives ---

    protected IEnumerator MoveTo(Vector2 worldPos, float arrivalThreshold = 1f)
    {
        while (Vector2.Distance(_unit.transform.position, worldPos) > arrivalThreshold)
        {
            _scripted.Move = new Vector2(
                Mathf.Sign(worldPos.x - _unit.transform.position.x), 0);
            yield return null;
        }
        _scripted.Move = Vector2.zero;
    }

    protected IEnumerator Face(float directionX)
    {
        _scripted.Move = new Vector2(Mathf.Sign(directionX), 0);
        yield return null;
        _scripted.Move = Vector2.zero;
    }

    protected IEnumerator Jump()
    {
        _scripted.JumpDown = true;
        _scripted.JumpHeld = true;
        yield return new WaitForFixedUpdate();
        _scripted.JumpDown = false;
    }

    protected IEnumerator ReleaseJump()
    {
        _scripted.JumpHeld = false;
        yield return null;
    }

    protected IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    protected IEnumerator WaitFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
            yield return new WaitForFixedUpdate();
    }
}
