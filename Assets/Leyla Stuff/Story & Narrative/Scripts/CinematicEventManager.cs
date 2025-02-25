using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class CinematicEventManager : MonoBehaviour
{
    public static CinematicEventManager Instance { get; private set; }

    [System.Serializable]
    public class CinematicEvent
    {
        public string eventId; // Unique ID for cinematic event
        public UnityEvent onEventTriggered; // Unity event to trigger for this cinematic event
    }

    public List<CinematicEvent> cinematicEvents = new List<CinematicEvent>();

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

    public void TriggerCinematicEvent(string eventID)
    {
        foreach (var cinematicEvent in cinematicEvents)
        {
            if (cinematicEvent.eventId == eventID)
            {
                Debug.Log($"Triggering Cinematic Event: {eventID}");
                cinematicEvent.onEventTriggered?.Invoke();
                return;
            }
        }
        Debug.LogWarning($"No event found for ID: {eventID}");
    }
}