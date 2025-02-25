using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class DialogueEventManager : MonoBehaviour
{
    public static DialogueEventManager Instance { get; private set; }

    [System.Serializable]
    public class DialogueEvent
    {
        public string eventId; // Unique ID for dialogue node
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

    public void TriggerDialogueEvent(string eventId)
    {
        foreach (var dialogueEvent in dialogueEvents)
        {
            if (dialogueEvent.eventId == eventId)
            {
                Debug.Log($"Triggering Dialogue Event: {eventId}");
                dialogueEvent.onDialogueTriggered?.Invoke();
                return;
            }
        }
        Debug.LogWarning($"No event found for ID: {eventId}");
    }
}
