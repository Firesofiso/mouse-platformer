using System;
using TarodevController;
using TarodevController.Trol;
using UnityEngine;

public class Chase : PathfinderState
{
    SightlineSensor Vision => (SightlineSensor)Sensor;

    public override void Do() {
        if (Vision.IsAwareOfTargetPosition) {
            // continue chasing
        } else if (Vision.TargetPermanenceNotElapsed) {
            // pursue
        }
    }

    public override void Enter() {
        Brain.StartGenerating();
        Brain.StartTraversing();
    }
}
