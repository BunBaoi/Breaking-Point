using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BoolManager : MonoBehaviour
{
    public static BoolManager Instance { get; private set; }

    [SerializeField]
    private Dictionary<string, bool> boolDictionary = new Dictionary<string, bool>();

    [SerializeField]
    private List<string> boolKeys = new List<string>();

    public delegate void BoolUpdated();
    public event BoolUpdated OnBoolUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // LoadBools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveBools();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveBools();
        }
    }

    private void SaveBools()
    {
        PlayerPrefs.SetString("BoolKeys", string.Join(",", boolKeys.ToArray()));

        foreach (var entry in boolDictionary)
        {
            PlayerPrefs.SetInt(entry.Key, entry.Value ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    private void LoadBools()
    {
        boolKeys.Clear();
        foreach (string key in PlayerPrefs.GetString("BoolKeys", "").Split(','))
        {
            if (!string.IsNullOrEmpty(key))
            {
                boolKeys.Add(key);
                if (!boolDictionary.ContainsKey(key))
                {
                    boolDictionary[key] = PlayerPrefs.GetInt(key, 0) == 1;
                }
            }
        }
    }

    public void SetBool(string boolName, bool value)
    {
        if (!boolDictionary.ContainsKey(boolName))
        {
            boolDictionary.Add(boolName, value);
            boolKeys.Add(boolName);
        }
        else
        {
            boolDictionary[boolName] = value;
        }

        SaveBools(); // Save immediately

        OnBoolUpdated?.Invoke(); // Notify listeners
    }

    public void RemoveBool(string boolName)
    {
        if (boolDictionary.ContainsKey(boolName))
        {
            boolDictionary.Remove(boolName);
            boolKeys.Remove(boolName);
            SaveBools(); // Save immediately after removal
        }
        else
        {
            // Debug.LogWarning($"Bool '{boolName}' not found.");
        }
    }

    public bool GetBool(string boolName)
    {
        if (boolDictionary.TryGetValue(boolName, out bool value))
        {
            return value;
        }
        else
        {
            // Debug.LogWarning($"Bool '{boolName}' not found.");
            return false;
        }
    }

    public List<string> GetBoolKeys()
    {
        return boolKeys;
    }

    public Dictionary<string, bool> GetBoolDictionary()
    {
        return boolDictionary;
    }

    public void ClearAllBools()
    {
        Debug.Log("clear all bools");
        boolDictionary.Clear();
        boolKeys.Clear();
        PlayerPrefs.DeleteKey("BoolKeys"); // Clear the stored keys
        PlayerPrefs.Save(); // Ensure PlayerPrefs is saved
    }
}