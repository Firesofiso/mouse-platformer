using UnityEngine;


public class Idle : PathfinderState {

    public override void Do() { }

    public override void Enter() {
        Brain.ClearPath();
    }
}