using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    [Header("Scene")]
    public string sceneName;

    [Header("Player Location")]
    public SerializableVector3 playerPosition;
    public SerializableQuaternion playerRotation;  // Player's rotation
    public float cameraXRotation;

    [Header("Other Saved Data")]
    public List<BoolState> boolStates = new List<BoolState>();
    public List<ObjectActiveState> objectActiveStates = new List<ObjectActiveState>();
    public List<DialogueState> dialogueStates = new List<DialogueState>();
    public List<string> inventoryItems = new();
    public List<string> shownTipIDs = new List<string>();

    [Header("Player Stats")]
    public float oxygen;
    public float energy;

    [Header("Day and Night")]
    public int savedDay;
    public int savedHours;
    public int savedMinutes;

    [Header("Journal Pages")]
    public List<string> journalPageIDs = new List<string>();
}

    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public struct SerializableQuaternion
    {
        public float x, y, z, w;

        public SerializableQuaternion(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

[Serializable]
public struct BoolState
{
    public string key;
    public bool value;

    public BoolState(string key, bool value)
    {
        this.key = key;
        this.value = value;
    }
}

[Serializable]
public struct ObjectActiveState
{
    public string objectName;
    public bool isActive;
    public bool isDestroyed;

    public ObjectActiveState(string objectName, bool isActive, bool isDestroyed)
    {
        this.objectName = objectName;
        this.isActive = isActive;
        this.isDestroyed = isDestroyed;
    }
}

[Serializable]
public struct DialogueState
{
    public string dialogueID;
    public bool isCompleted;

    public DialogueState(string dialogueID, bool isCompleted)
    {
        this.dialogueID = dialogueID;
        this.isCompleted = isCompleted;
    }
}
