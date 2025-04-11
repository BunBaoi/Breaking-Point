using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbingState : MonoBehaviour
{
    [Header("Climbing References")]
    public ClimbingController climbingController;
    public PlayerMovement playerMovement;

    [Header("Climbing Input")]
    public KeyCode leftGrabKey = KeyCode.Mouse0;
    public KeyCode rightGrabKey = KeyCode.Mouse1;

    [Header("Climbing State")]
    private bool isClimbing = false;

    public bool canClimb = true;

    private void Start()
    {
        // Ensure references are set
        if (climbingController == null)
            climbingController = GetComponent<ClimbingController>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        // Only handle climbing input if canClimb is true
        if (canClimb)
        {
            HandleClimbingInput();
            UpdateClimbingState();
        }
        else
        {
            // Ensure we exit climbing state if climbing is no longer allowed
            if (isClimbing)
            {
                ExitClimbingState();
            }
        }
    }

    private void HandleClimbingInput()
    {
        // Left hand grab
        if (Input.GetKeyDown(leftGrabKey))
        {
            bool grabbed = climbingController.TryGrab(true, "Climbable");
            if (grabbed) isClimbing = true;
        }
        else if (Input.GetKeyUp(leftGrabKey))
        {
            climbingController.Release(true);
        }

        // Right hand grab
        if (Input.GetKeyDown(rightGrabKey))
        {
            bool grabbed = climbingController.TryGrab(false, "Climbable");
            if (grabbed) isClimbing = true;
        }
        else if (Input.GetKeyUp(rightGrabKey))
        {
            climbingController.Release(false);
        }
    }

    private void UpdateClimbingState()
    {
        if (isClimbing)
        {
            // Disable regular movement
            playerMovement.enabled = false;

            // Handle climbing movement
            if (climbingController.IsHolding())
            {
                climbingController.HandleClimbingMovement();
                climbingController.UpdateHandPositions();
            }
            else
            {
                // Exit climbing state
                ExitClimbingState();
            }
        }
    }

    public void ExitClimbingState()
    {
        isClimbing = false;
        climbingController.ResetClimbingState();
        playerMovement.enabled = true;
    }

    // Method to disable climbing from InventoryManager
    public void DisableClimbing()
    {
        canClimb = false;
        ExitClimbingState();
    }

    // Method to re-enable climbing
    public void EnableClimbing()
    {
        canClimb = true;
    }
}
