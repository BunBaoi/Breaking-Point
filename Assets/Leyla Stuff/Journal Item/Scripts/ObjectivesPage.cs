using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Objectives Page", menuName = "Journal/Objectives Page")]
public class ObjectivesPage : JournalPage
{
    [System.Serializable]
    public class Objective
    {
        public string text;
        public List<string> boolKeys = new List<string>(); // Multiple bool keys
    }

    public List<Objective> objectives = new List<Objective>();

    public List<bool> GetChecklistStatus()
    {
        List<bool> checklistStatus = new List<bool>();

        foreach (var objective in objectives)
        {
            bool isCompleted = true; // Assume it's completed unless proven otherwise

            foreach (var boolKey in objective.boolKeys)
            {
                if (!BoolManager.Instance.GetBool(boolKey)) // If any boolKey is false, mark as incomplete
                {
                    isCompleted = false;
                    break; // Stop checking, as we already know it's incomplete
                }
            }

            checklistStatus.Add(isCompleted);
        }
        return checklistStatus;
    }
}
