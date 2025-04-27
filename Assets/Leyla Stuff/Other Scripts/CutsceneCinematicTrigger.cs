using UnityEngine;

public class CutsceneCinematicTrigger : MonoBehaviour
{
    [SerializeField] private string boolName = "";
    [SerializeField] private CinematicSequence triggerCinematic;

    private void Start()
    {
        // Check immediately at start if the cutscene already triggered
        if (BoolManager.Instance.GetBool(boolName))
        {
            this.enabled = false; // Cutscene already triggered, disable this script
        }
    }

    private void Update()
    {
        // If the bool becomes true and we haven't disabled yet
        if (BoolManager.Instance.GetBool(boolName))
        {
            TriggerCinematic();
            this.enabled = false; // Disable script to prevent retriggering
        }
    }

    public void TriggerCinematic()
    {
        Debug.Log("Triggering cinematic: " + triggerCinematic.name);
        triggerCinematic.StartCinematic();
    }
}

