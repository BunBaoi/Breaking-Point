using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscenes/CutsceneData")]
public class CutsceneData : ScriptableObject
{
    public string cutsceneID;
    public string cutsceneName;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(cutsceneID))
        {
            cutsceneID = Guid.NewGuid().ToString();
        }
    }
}
