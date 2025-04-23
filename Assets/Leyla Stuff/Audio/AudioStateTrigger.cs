using UnityEngine;

public class AudioStateTrigger : MonoBehaviour
{
    [SerializeField] private string audioStateName;
    private bool hasTriggered = false;

    private void OnTriggerStay(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            AudioManager.Instance.ChangeState(audioStateName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            hasTriggered = false;
        }
    }
}
