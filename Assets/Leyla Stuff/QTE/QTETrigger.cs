using UnityEngine;

public class QTETrigger : MonoBehaviour
{
    [SerializeField] private bool triggered = false;
    public Transform theTargetPlace;
    [SerializeField] private QTETrigger nextTrigger;

    private bool allowAutoTrigger = true; // <-- NEW

    public void TriggerManually()
    {
        if (triggered) return;
        triggered = true;
        QTEManager.Instance.StartQTE(theTargetPlace, nextTrigger);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!allowAutoTrigger || triggered) return;
        if (other.CompareTag("Player"))
        {
            triggered = true;
            QTEManager.Instance.StartQTE(theTargetPlace, nextTrigger);
        }
    }

    public void DisableAutoTrigger()
    {
        allowAutoTrigger = false;
    }
}