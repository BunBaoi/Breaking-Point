using System.Collections.Generic;
using UnityEngine;

public class CutsceneTracker : MonoBehaviour
{
    public static CutsceneTracker Instance;

    private HashSet<string> completedCutscenes = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void MarkCutsceneAsCompleted(string id)
    {
        completedCutscenes.Add(id);
    }

    public List<string> GetCompletedCutsceneIDs()
    {
        return new List<string>(completedCutscenes);
    }

    public bool IsCutsceneCompleted(string id)
    {
        return completedCutscenes.Contains(id);
    }
}
