using System.Collections.Generic;
using UnityEngine;

public class CutsceneCinematicTrigger : MonoBehaviour
{
    [SerializeField] private string boolName = "";
    [SerializeField] private CinematicSequence triggerCinematic;

    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>();
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>();

    private void Update()
    {
        Debug.Log("Checking cinematic conditions...");

        if (CanStartCinematic() && !BoolManager.Instance.GetBool(boolName))
        {
            Debug.Log("Conditions met and cinematic not yet triggered. Triggering now.");
            TriggerCinematic();
        }
        else
        {
            if (BoolManager.Instance.GetBool(boolName))
            {
                Debug.Log("Cinematic already triggered before.");
            }
            else
            {
                Debug.Log("Conditions not met yet for cinematic.");
            }
        }
    }

    private bool CanStartCinematic()
    {
        // Check if all required bool conditions are met (true or false based on lists)
        foreach (string boolKey in requiredBoolKeysTrue)
        {
            if (!BoolManager.Instance.GetBool(boolKey))
            {
                Debug.Log($"Condition failed: {boolKey} is not true.");
                return false;
            }
        }

        foreach (string boolKey in requiredBoolKeysFalse)
        {
            if (BoolManager.Instance.GetBool(boolKey))
            {
                Debug.Log($"Condition failed: {boolKey} is not false.");
                return false;
            }
        }

        Debug.Log("All conditions passed.");
        return true;
    }

    public void TriggerCinematic()
    {
        if (triggerCinematic != null)
        {
            Debug.Log("Triggering cinematic: " + triggerCinematic.name);
            triggerCinematic.StartCinematic();
        }
        else
        {
            Debug.LogWarning("No cinematic assigned to trigger!");
        }

        BoolManager.Instance.SetBool(boolName, true);
        Debug.Log($"Bool '{boolName}' set to true to prevent retriggering.");
    }
}

