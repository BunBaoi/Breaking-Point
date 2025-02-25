using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbableItem : MonoBehaviour
{
    [Header("Climbing Item Settings")]
    public string climbableTag = "Climbable"; // Tag for objects that can be climbed
    public bool isClimbable = true;

    private void Start()
    {
        // Ensure the object has the climbable tag
        gameObject.tag = climbableTag;
    }

    public void EnableClimbing()
    {
        isClimbable = true;
    }

    public void DisableClimbing()
    {
        isClimbable = false;
    }
}
