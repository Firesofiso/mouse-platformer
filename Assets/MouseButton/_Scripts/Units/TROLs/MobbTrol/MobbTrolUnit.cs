using System;
using System.Collections.Generic;
using Pathfinding;
using TarodevController;
using TarodevController.Trol;
using UnityEngine;

namespace TarodevController
{
    public class MobbTrolUnit : StatefulUnit
    {
        [SerializeField] private PathfindingBrain PathfindingBrain;
        public override PathfindingBrain Brain => PathfindingBrain;

        public MobbTrolController controller;
    }
}

/*
State Hierarchy

Armed
├─ Patrol (wander)
├─ Chase (path to target)
├─ Pursue (path to last seen target position)
├─ Aim (stand still and prepare attack)
└─ Throw (release spear projectile)
Spearless
├─ Recover (stand up after throw)
├─ Reclaim (path to spear)
├─ Cheer (spear recovered)
└─ Dance (post-cheer, etc.)

*/