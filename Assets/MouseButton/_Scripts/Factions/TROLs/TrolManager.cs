using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tarodev.Trol {
    public class TrolManager : MonoBehaviour {
        internal List<ITrolUnit> activeTrols = new List<ITrolUnit>();
        internal List<TrolSpear> activeSpears = new List<TrolSpear>();
    }

    internal class TrolProperties {
        private int bounceFactor;
    }

    internal interface ITrolUnit {

    }
}