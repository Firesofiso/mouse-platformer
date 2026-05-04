using TarodevController;
using TarodevController.Trol;
using UnityEngine;

namespace TarodevController
{
    public class MobbTrolUnit : StatefulUnit
    {
        [SerializeField] private PathfindingBrain PathfindingBrain;
        public override PathfindingBrain Brain => PathfindingBrain;
        public override TargetDetectionSensor Sensor => PathfindingBrain?.Sensor;

        public MobbTrolController controller;
        public ITrolBrainContext Trol => controller;
    }
}

/*
State Hierarchy

Armed (AI)  /  ArmedPlayer (player)
├─ Patrol (wander)
├─ Chase (path to target)          [AI only — PathfinderState]
├─ Aim (stand still, prepare)      [shared — State + IInputFilter]
└─ Throw (release spear)           [shared]
Spearless
├─ Recover (stand up after throw)
├─ Reclaim (path to spear)
├─ Cheer (spear recovered)
└─ Dance (post-cheer)

*/
