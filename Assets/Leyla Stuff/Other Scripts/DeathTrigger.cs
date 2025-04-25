using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    [SerializeField] private bool inTrigger;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !inTrigger)
        {
            inTrigger = true;
            PlayerStats.Instance.PlayerDeath();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inTrigger = false;
        }
    }
}
