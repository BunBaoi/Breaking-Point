using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbingSystem : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform cameraTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    private PlayerMovement playerMovement;
    private PlayerStats playerStats; // Reference to PlayerStats
    private Rigidbody playerRigidbody; // Reference to player's Rigidbody

    [Header("Climbing Settings")]
    public float climbSpeed = 3f;
    public float maxClimbDistance = 4f;
    public string climbableTag = "Climbable";

    [Header("Body Positioning")]
    [Tooltip("Vertical offset from hands to position the body. Positive values position the body below the hands")]
    public float bodyPositionOffset = 1.5f; // This is the new offset parameter

    [Header("Initial Climbing Response")]
    [Tooltip("Initial snap percentage toward target when first attaching")]
    public float initialSnapPercentage = 0.3f; // Add immediate movement when first attaching
    [Tooltip("Distance to stop short of pick to prevent overshooting")]
    public float targetOffsetDistance = 0.2f; // How far to stop before the actual pick position

    [Header("Energy Settings")]
    public float climbingEnergyDrainRate = 1f; // Energy drain per second while climbing
    public float lowEnergyClimbSpeedMultiplier = 0.5f; // Climbing speed multiplier when low on energy
    public float lowEnergyThreshold = 20f; // Threshold for low energy

    [Header("Climbing Feel Settings")]
    public float upwardBias = 0.1f; // Small upward force for natural climbing feel

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string leftGrabActionName = "LeftGrab";
    [SerializeField] private string rightGrabActionName = "RightGrab";
    private InputAction leftGrab;
    private InputAction rightGrab;

    // Climbing state
    private bool leftHandAttached = false;
    private bool rightHandAttached = false;
    private Vector3 leftHandPosition;
    private Vector3 rightHandPosition;
    private Vector3 leftHandOriginalLocalPos;
    private Vector3 rightHandOriginalLocalPos;

    // Debug and state management
    private bool isActivelyClimbing = false;
    private bool isFirstClimbFrame = false; // Added to track first climb frame

    private void Awake()
    {
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");
            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }
    }

    void Start()
    {
        // Get player movement component
        playerMovement = GetComponent<PlayerMovement>();

        // Get player stats component
        playerStats = GetComponent<PlayerStats>();
        if (!playerStats)
        {
            Debug.LogWarning("PlayerStats component not found. Energy drain while climbing will not work.");
        }

        // Get the player's Rigidbody if it exists
        playerRigidbody = GetComponent<Rigidbody>();

        if (!controller)
        {
            controller = GetComponent<CharacterController>();
            if (!controller)
            {
                Debug.LogError("No CharacterController found on the player!");
                enabled = false;
                return;
            }
        }

        if (!cameraTransform)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogError("No camera reference found!");
                enabled = false;
                return;
            }
        }

        // Store original hand positions
        leftHandOriginalLocalPos = leftHandTransform.localPosition;
        rightHandOriginalLocalPos = rightHandTransform.localPosition;

        SetupInputActions();
    }

    void SetupInputActions()
    {
        leftGrab = inputActions.FindAction(leftGrabActionName);
        rightGrab = inputActions.FindAction(rightGrabActionName);

        if (leftGrab != null) leftGrab.Enable();
        if (rightGrab != null) rightGrab.Enable();

        // Set up event handlers for input
        if (leftGrab != null) leftGrab.performed += _ => AttachIcepick(true);
        if (rightGrab != null) rightGrab.performed += _ => AttachIcepick(false);
        if (leftGrab != null) leftGrab.canceled += _ => DetachIcepick(true);
        if (rightGrab != null) rightGrab.canceled += _ => DetachIcepick(false);
    }

    void FixedUpdate()
    {
        // Simple climbing check
        if (IsClimbing())
        {
            // Ensure gravity and other physics forces are completely disabled
            if (GetComponent<Rigidbody>() != null)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.velocity = Vector3.zero; // Zero out any existing velocity
                rb.isKinematic = true; // Make rigidbody kinematic while climbing
            }

            DirectClimbToIcepicks(); // Renamed to reflect direct movement
            DrainEnergyWhileClimbing();

            // Reset first frame flag after first frame processing
            if (isFirstClimbFrame)
            {
                isFirstClimbFrame = false;
            }
        }
    }

    void Update()
    {
        UpdateHandVisuals();
    }

    void DrainEnergyWhileClimbing()
    {
        if (playerStats != null && isActivelyClimbing)
        {
            // Apply the configured energy drain rate
            playerStats.DrainEnergy(climbingEnergyDrainRate * Time.deltaTime);

            // Debug output for energy consumption
            if (Time.frameCount % 60 == 0) // Log once per second (approximately)
            {
                Debug.Log($"Climbing energy consumption: {climbingEnergyDrainRate} per second");
            }
        }
    }

    void AttachIcepick(bool isLeftHand)
    {
        // Fire a raycast from the camera center
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxClimbDistance))
        {
            // Check if we hit a climbable surface
            if (hit.collider.CompareTag(climbableTag))
            {
                // Get the appropriate hand transform
                Transform handTransform = isLeftHand ? leftHandTransform : rightHandTransform;

                // Find the icepick tip
                Transform icepickTip = FindIcepickTip(handTransform);

                if (icepickTip != null)
                {
                    // Calculate the offset from the hand to the icepick tip
                    Vector3 tipOffset = icepickTip.position - handTransform.position;

                    // Attach the icepick at the hit point, but adjust the hand position to account for the tip offset
                    Vector3 handAttachPosition = hit.point - tipOffset;

                    if (isLeftHand)
                    {
                        leftHandAttached = true;
                        leftHandPosition = handAttachPosition;

                        // Immediately set hand to position to avoid any initial jitter
                        leftHandTransform.position = handAttachPosition;
                    }
                    else
                    {
                        rightHandAttached = true;
                        rightHandPosition = handAttachPosition;

                        // Immediately set hand to position to avoid any initial jitter
                        rightHandTransform.position = handAttachPosition;
                    }

                    // Debug visualization
                    Debug.DrawLine(handTransform.position, hit.point, Color.red, 1.0f);
                    Debug.DrawRay(hit.point, hit.normal, Color.blue, 1.0f);

                    // Disable normal movement and gravity when climbing
                    if (IsClimbing() && !isActivelyClimbing)
                    {
                        playerMovement.SetMovementState(false);
                        playerMovement.SetApplyGravity(false);
                        isActivelyClimbing = true;
                        isFirstClimbFrame = true; // Mark the first frame of climbing

                        // Set player status to climbing if PlayerStats exists
                        if (playerStats != null)
                        {
                            playerStats.stateOfPlayer = PlayerStats.PlayerStatus.RClimbing;
                        }

                        // Perform initial snap movement if configured
                        if (initialSnapPercentage > 0)
                        {
                            PerformInitialSnap();
                        }

                        Debug.Log("Started climbing");
                    }
                }
                else
                {
                    Debug.LogWarning("No icepick tip found for " + (isLeftHand ? "left" : "right") + " hand");

                    // Fallback to original behavior if tip cannot be found
                    if (isLeftHand)
                    {
                        leftHandAttached = true;
                        leftHandPosition = hit.point;

                        // Immediately set hand to position
                        leftHandTransform.position = hit.point;
                    }
                    else
                    {
                        rightHandAttached = true;
                        rightHandPosition = hit.point;

                        // Immediately set hand to position
                        rightHandTransform.position = hit.point;
                    }
                }
            }
        }
    }

    // Helper method to find the icepick tip transform at runtime
    private Transform FindIcepickTip(Transform handTransform)
    {
        if (handTransform == null || handTransform.childCount == 0)
            return null;

        // First, check if there's an equipped icepick - look for the first child of the hand
        Transform icepickTransform = null;

        // Find the equipped item (should be the first child of the hand transform)
        if (handTransform.childCount > 0)
        {
            icepickTransform = handTransform.GetChild(0);
        }

        if (icepickTransform == null)
            return null;

        // Try to find the "IcepickTip" directly 
        Transform directTip = icepickTransform.Find("IcepickTip");
        if (directTip != null)
            return directTip;

        // If not found directly, try to find it under the IcepickHinge
        Transform hinge = icepickTransform.Find("IcepickHinge");
        if (hinge != null)
        {
            // Look for IcepickTip under the hinge
            Transform tipUnderHinge = hinge.Find("IcepickTip");
            if (tipUnderHinge != null)
                return tipUnderHinge;

            // If no dedicated tip, try to find the collider child as a fallback
            foreach (Transform child in hinge)
            {
                if (child.GetComponent<Collider>() != null)
                    return child;
            }
        }

        // If we still haven't found anything, look through the entire icepick for anything named appropriately
        foreach (Transform child in icepickTransform.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains("Tip") || child.name.Contains("Point") || child.name.Contains("End"))
                return child;
        }

        // Last resort - if it has a MeshRenderer, use its bounds.max as an approximation
        MeshRenderer renderer = icepickTransform.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            // Create a temporary transform at the "tip" position
            GameObject tempTip = new GameObject("TempTip");
            tempTip.transform.position = renderer.bounds.center + renderer.bounds.extents.z * icepickTransform.forward;
            tempTip.transform.SetParent(icepickTransform);
            return tempTip.transform;
        }

        return null;
    }

    // Get the current target position for climbing
    private Vector3 GetCurrentTargetPosition()
    {
        Vector3 targetPosition = Vector3.zero;
        int attachedCount = 0;

        if (leftHandAttached)
        {
            targetPosition += leftHandPosition;
            attachedCount++;
        }

        if (rightHandAttached)
        {
            targetPosition += rightHandPosition;
            attachedCount++;
        }

        if (attachedCount > 0)
        {
            // Calculate the average position
            targetPosition /= attachedCount;

            // Apply the vertical offset to place the body below the hands
            // This way the player's feet won't be pulled up to the icepick position
            targetPosition.y -= bodyPositionOffset;
        }

        return targetPosition;
    }

    // New method to perform initial snap toward target
    private void PerformInitialSnap()
    {
        Vector3 targetPosition = GetCurrentTargetPosition();
        Vector3 toTarget = targetPosition - transform.position;

        // Only snap if we're not too close already
        if (toTarget.magnitude > 0.2f)
        {
            // Move a percentage of the way toward the target immediately
            Vector3 snapMovement = toTarget * initialSnapPercentage;

            // Cap the maximum initial movement to prevent teleporting too far
            float maxSnapDistance = 0.5f;
            if (snapMovement.magnitude > maxSnapDistance)
            {
                snapMovement = snapMovement.normalized * maxSnapDistance;
            }

            // Apply the immediate movement
            controller.Move(snapMovement);

            Debug.Log($"Initial snap applied: {snapMovement.magnitude} meters");
        }
    }

    void DetachIcepick(bool isLeftHand)
    {
        // Detach the icepick
        if (isLeftHand)
        {
            leftHandAttached = false;
        }
        else
        {
            rightHandAttached = false;
        }

        // If no longer climbing, restore normal movement
        if (!IsClimbing() && isActivelyClimbing)
        {
            playerMovement.SetMovementState(true);
            playerMovement.SetApplyGravity(true);
            isActivelyClimbing = false;

            // Re-enable physics if we have a Rigidbody
            if (GetComponent<Rigidbody>() != null)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // Reset player status to free roam if PlayerStats exists
            if (playerStats != null)
            {
                playerStats.stateOfPlayer = PlayerStats.PlayerStatus.FreeRoam;
            }

            Debug.Log("Stopped climbing");
        }
    }

    void DetachAllIcepicks()
    {
        leftHandAttached = false;
        rightHandAttached = false;

        // Restore normal movement
        playerMovement.SetMovementState(true);
        playerMovement.SetApplyGravity(true);
        isActivelyClimbing = false;

        // Re-enable physics if we have a Rigidbody
        if (GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Reset player status to free roam if PlayerStats exists
        if (playerStats != null)
        {
            playerStats.stateOfPlayer = PlayerStats.PlayerStatus.FreeRoam;
        }
    }

    bool IsClimbing()
    {
        return leftHandAttached || rightHandAttached;
    }

    void DirectClimbToIcepicks()
    {
        // Get the target position (average of attached icepicks)
        Vector3 targetPosition = GetCurrentTargetPosition();

        // Continue only if we have a valid target
        if (targetPosition != Vector3.zero)
        {
            // Get vector to target
            Vector3 toTarget = targetPosition - transform.position;
            float distanceToTarget = toTarget.magnitude;

            // Only climb if we're not too close
            if (distanceToTarget > 0.1f)
            {
                // Calculate the target point, accounting for the offset to prevent overshooting
                Vector3 adjustedTargetPosition = targetPosition;

                // If close enough, adjust the target to stop slightly before the actual pick
                if (distanceToTarget < 1.0f)
                {
                    adjustedTargetPosition = Vector3.Lerp(
                        transform.position,
                        targetPosition,
                        (distanceToTarget - targetOffsetDistance) / distanceToTarget
                    );

                    // Don't go past the original position
                    if ((adjustedTargetPosition - transform.position).magnitude <= 0.05f)
                    {
                        // We're close enough, no need to move
                        return;
                    }

                    // Recalculate direction to adjusted target
                    toTarget = adjustedTargetPosition - transform.position;
                    distanceToTarget = toTarget.magnitude;
                }

                // Calculate raw direction to target - with no smoothing
                Vector3 climbDirection = toTarget.normalized;

                // Add a slight upward bias for better feel
                climbDirection.y += upwardBias;
                climbDirection.Normalize();

                // Calculate base speed
                float effectiveClimbSpeed = climbSpeed;

                // Apply energy penalty if needed
                if (playerStats != null && playerStats.GetEnergyPercentage() < lowEnergyThreshold)
                {
                    effectiveClimbSpeed *= lowEnergyClimbSpeedMultiplier;
                }

                // Calculate the direct movement distance for this frame
                // Clamp the distance to prevent teleporting or moving too fast
                float moveDistance = Mathf.Min(
                    effectiveClimbSpeed * Time.fixedDeltaTime,
                    distanceToTarget * 0.9f  // Never move more than 90% of the distance in one frame
                );

                // Create the direct movement vector
                Vector3 movement = climbDirection * moveDistance;

                // If using a Rigidbody, move it directly
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && rb.isKinematic)
                {
                    // Move the transform directly when using kinematic rigidbody
                    transform.position += movement;
                }
                else
                {
                    // Apply the movement directly through the controller
                    controller.Move(movement);
                }

                // Debug visualization
                Debug.DrawLine(transform.position, targetPosition, Color.yellow);
                Debug.DrawRay(transform.position, climbDirection * 2f, Color.green);
            }
        }
    }

    void UpdateHandVisuals()
    {
        // Direct hand visual updates for left hand with no smoothing
        if (leftHandAttached)
        {
            // Use direct positioning with no interpolation
            leftHandTransform.position = leftHandPosition;
        }
        else
        {
            // Immediately return hand to original position with no interpolation
            leftHandTransform.localPosition = leftHandOriginalLocalPos;
        }

        // Direct hand visual updates for right hand with no smoothing
        if (rightHandAttached)
        {
            // Use direct positioning with no interpolation
            rightHandTransform.position = rightHandPosition;
        }
        else
        {
            // Immediately return hand to original position with no interpolation
            rightHandTransform.localPosition = rightHandOriginalLocalPos;
        }
    }

    private void OnEnable()
    {
        SetupInputActions();
    }

    private void OnDisable()
    {
        // Clean up and reset when disabled
        if (leftHandAttached || rightHandAttached)
        {
            DetachAllIcepicks();
        }

        // Unsubscribe from events
        if (leftGrab != null)
        {
            leftGrab.Disable();
            leftGrab.performed -= _ => AttachIcepick(true);
            leftGrab.canceled -= _ => DetachIcepick(true);
        }

        if (rightGrab != null)
        {
            rightGrab.Disable();
            rightGrab.performed -= _ => AttachIcepick(false);
            rightGrab.canceled -= _ => DetachIcepick(false);
        }
    }
}