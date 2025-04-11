using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallingAnimationEvents : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    public void TriggerLeftFootstepSound()
    {
        playerMovement.PlayLeftFootstepSound();
    }

    public void TriggerRightFootstepSound()
    {
        playerMovement.PlayRightFootstepSound();
    }
}
