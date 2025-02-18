using UnityEngine;
using System.Collections.Generic;

public class ObjectDeletionManager : MonoBehaviour
{
    [System.Serializable]
    public class ObjectDeletionRule
    {
        public GameObject targetObject; // Object to delete
        public List<string> requiredBools; // Bools that must be true
    }

    public List<ObjectDeletionRule> deletionRules = new List<ObjectDeletionRule>();

    private void Start()
    {
        LoadDeletedObjects();
        CheckAndDeleteObjects();
    }

    private void OnEnable()
    {
        BoolManager.Instance.OnBoolUpdated += CheckAndDeleteObjects; // Subscribe to bool changes
    }

    private void OnDisable()
    {
        if (BoolManager.Instance != null)
        {
            BoolManager.Instance.OnBoolUpdated -= CheckAndDeleteObjects;
        }
    }

    private void CheckAndDeleteObjects()
    {
        foreach (var rule in deletionRules)
        {
            if (rule.targetObject != null && !PlayerPrefs.HasKey(rule.targetObject.name))
            {
                bool allBoolsTrue = true;

                foreach (string boolKey in rule.requiredBools)
                {
                    if (!BoolManager.Instance.GetBool(boolKey))
                    {
                        allBoolsTrue = false;
                        break;
                    }
                }

                if (allBoolsTrue)
                {
                    Debug.Log($"Deleting object: {rule.targetObject.name}");
                    PlayerPrefs.SetInt(rule.targetObject.name, 1);
                    Destroy(rule.targetObject);
                }
            }
        }
    }

    private void LoadDeletedObjects()
    {
        foreach (var rule in deletionRules)
        {
            if (PlayerPrefs.GetInt(rule.targetObject.name, 0) == 1)
            {
                Debug.Log($"Object {rule.targetObject.name} was previously deleted.");
                Destroy(rule.targetObject);
            }
        }
    }
}
