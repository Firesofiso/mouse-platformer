using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrolManager : MonoBehaviour
{
    public List<TrolUnit> activeTrols = new List<TrolUnit>();
    public List<TrolSpear> activeSpears = new List<TrolSpear>();
}

public interface TrolUnit {
    
}