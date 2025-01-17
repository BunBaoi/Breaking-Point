using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class DialogueEventManager : MonoBehaviour
{
    public static DialogueEventManager Instance { get; private set; }

    [System.Serializable]
    public class DialogueEvent
    {
        public string eventID; // Unique ID for dialogue node
        public UnityEvent onDialogueTriggered; // Scene-based event
    }

    public List<DialogueEvent> dialogueEvents = new List<DialogueEvent>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TriggerDialogueEvent(string eventID)
    {
        foreach (var dialogueEvent in dialogueEvents)
        {
            if (dialogueEvent.eventID == eventID)
            {
                Debug.Log($"Triggering Dialogue Event: {eventID}");
                dialogueEvent.onDialogueTriggered?.Invoke();
                return;
            }
        }
        Debug.LogWarning($"No event found for ID: {eventID}");
    }
}
