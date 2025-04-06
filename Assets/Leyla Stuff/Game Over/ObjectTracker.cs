using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTracker : MonoBehaviour
{
    public static ObjectTracker Instance;

    private Dictionary<string, string> objectUniqueIDs = new Dictionary<string, string>();
    private HashSet<string> trackedObjects = new HashSet<string>();
    private HashSet<string> destroyedObjects = new HashSet<string>();

    [SerializeField] private LayerMask layersToTrack;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Check if the object's layer is in the LayerMask
            if (((1 << obj.layer) & layersToTrack.value) != 0)
            {
                Register(obj);
            }
        }

        LogTrackedObjects();
        LogDestroyedObjects();
    }

    private void LogTrackedObjects()
    {
        Debug.Log("Tracked Objects: ");
        foreach (string objName in trackedObjects)
        {
            Debug.Log(objName);
        }
    }

    private void LogDestroyedObjects()
    {
        Debug.Log("Destroyed Objects: ");
        foreach (string objName in destroyedObjects)
        {
            Debug.Log(objName);
        }
    }

    // Register the object with a unique ID
    public void Register(GameObject obj)
    {
        if (!trackedObjects.Contains(obj.name))
        {
            string uniqueID = System.Guid.NewGuid().ToString();
            objectUniqueIDs[obj.name] = uniqueID;
            trackedObjects.Add(obj.name);
            Debug.Log($"Object {obj.name} registered with unique ID: {uniqueID}");
        }
        else
        {
            Debug.LogWarning($"Object {obj.name} is already registered.");
        }
    }

    // Retrieve the unique ID for an object by name
    public string GetUniqueID(string objectName)
    {
        if (objectUniqueIDs.TryGetValue(objectName, out string uniqueID))
        {
            return uniqueID;
        }

        Debug.LogWarning($"Object {objectName} does not have a unique ID.");
        return null;
    }

    // Get all tracked objects
    public Dictionary<string, string> GetTrackedObjects()
    {
        return objectUniqueIDs;
    }

    // Mark the object as destroyed
    public void MarkAsDestroyed(string objectName)
    {
        if (trackedObjects.Contains(objectName))
        {
            if (!destroyedObjects.Contains(objectName))
            {
                destroyedObjects.Add(objectName);
            }
            else
            {
                Debug.LogWarning($"Object {objectName} is already marked as destroyed.");
            }
        }
        else
        {
            Debug.LogWarning($"Object {objectName} is not in the tracked list.");
        }
    }

    // Check if an object is marked as destroyed
    public bool IsObjectDestroyed(string objectName)
    {
        return destroyedObjects.Contains(objectName);
    }

    private void OnDestroy()
    {
        foreach (string objectName in trackedObjects)
        {
            // If object is destroyed, mark it as destroyed
            if (IsObjectDestroyed(objectName))
            {
                Debug.Log($"Object {objectName} is destroyed.");
            }
        }
    }
}
