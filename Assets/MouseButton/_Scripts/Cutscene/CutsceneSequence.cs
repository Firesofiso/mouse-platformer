using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cutscene/Sequence")]
public class CutsceneSequence : ScriptableObject
{
    public List<CutsceneBeat> beats = new();
}
