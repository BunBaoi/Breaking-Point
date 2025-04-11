using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using FMOD.Studio;

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

    [Header("Icepick Movement")]
    public float icepickMoveSpeed = 5f; // How fast the icepick moves to target
    private Vector3 leftHandTargetPosition; // Target position for left hand
    private Vector3 rightHandTargetPosition; // Target position for right hand
    private bool leftHandMoving = false; // Is left hand currently moving to target
    private bool rightHandMoving = false; // Is right hand currently moving to target

    [Header("Climbing Animation and Feel")]
    [Tooltip("Delay in seconds after pickaxe digs in before pulling the player")]
    public float digInDelay = 0.5f; // Time to wait after dig in before pulling
    [Tooltip("Whether player is currently being pulled toward climb position")]
    private bool isPulling = false; // Flag to track if actively pulling
    [Tooltip("Screen shake intensity when pickaxe connects")]
    public float screenShakeIntensity = 0.05f;
    [Tooltip("Screen shake duration when pickaxe connects")]
    public float screenShakeDuration = 0.2f;
    private float screenShakeTimer = 0f;
    private Vector3 originalCameraPosition;

    [Header("Climbing Sounds (FMOD)")]
    public EventReference[] icepickSounds; // Universal icepick strike sounds
    private int icepickSoundIndex = 0;

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

    [Header("Pickaxe Animation Settings")]
    [Tooltip("Starting rotation angle on X axis when moving icepick")]
    public float startRotationX = -45f; // Start with pickaxe tilted upward
    [Tooltip("Ending rotation angle on X axis when 'digging in'")]
    public float endRotationX = 30f; // End with pickaxe tilted downward for digging in
    [Tooltip("Speed of the digging in animation")]
    public float digInSpeed = 8f; // How fast the pickaxe rotates when digging in
    [Tooltip("Whether to animate the entire hand or just the pickaxe")]
    public bool animateEntireHand = false; // If true, rotates the hand; if false, finds the pickaxe

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

    // Pickaxe animation state
    private Transform leftPickaxeTransform;
    private Transform rightPickaxeTransform;
    private Quaternion leftPickaxeOriginalRotation;
    private Quaternion rightPickaxeOriginalRotation;
    private bool leftPickaxeDiggingIn = false;
    private bool rightPickaxeDiggingIn = false;
    private float leftDigInProgress = 0f;
    private float rightDigInProgress = 0f;

    // Debug and state management
    private bool isActivelyClimbing = false;
    private bool isFirstClimbFrame = false; // Added to track first climb frame

    private bool leftSoundPlayed = false;
    private bool rightSoundPlayed = false;

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

        // Find and store references to the pickaxes
        FindAndStorePickaxeReferences();

        SetupInputActions();
    }

    // New method to find and store pickaxe references
    private void FindAndStorePickaxeReferences()
    {
        // We'll attempt to find pickaxe references, but it's okay if they don't exist yet
        // The method will be called again when needed

        // Find left pickaxe
        if (leftHandTransform != null && leftHandTransform.childCount > 0)
        {
            leftPickaxeTransform = animateEntireHand ? leftHandTransform : leftHandTransform.GetChild(0);
            if (leftPickaxeTransform != null)
            {
                leftPickaxeOriginalRotation = leftPickaxeTransform.localRotation;
            }
        }

        // Find right pickaxe
        if (rightHandTransform != null && rightHandTransform.childCount > 0)
        {
            rightPickaxeTransform = animateEntireHand ? rightHandTransform : rightHandTransform.GetChild(0);
            if (rightPickaxeTransform != null)
            {
                rightPickaxeOriginalRotation = rightPickaxeTransform.localRotation;
            }
        }
    }

    private void ApplyScreenShake()
    {
        if (screenShakeTimer > 0)
        {
            // Store original position on first frame of shake
            if (screenShakeTimer == screenShakeDuration)
            {
                originalCameraPosition = cameraTransform.localPosition;
            }

            // Calculate shake amount (decreases over time)
            float currentIntensity = screenShakeIntensity * (screenShakeTimer / screenShakeDuration);

            // Apply random offset
            Vector3 shakeOffset = new Vector3(
                Random.Range(-currentIntensity, currentIntensity),
                Random.Range(-currentIntensity, currentIntensity),
                0
            );

            // Apply to camera
            cameraTransform.localPosition = originalCameraPosition + shakeOffset;

            // Decrease timer
            screenShakeTimer -= Time.deltaTime;

            // Reset position when done
            if (screenShakeTimer <= 0)
            {
                cameraTransform.localPosition = originalCameraPosition;
            }
        }
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

            // Only move body if at least one hand is fully attached and the pull delay has elapsed
            if (((leftHandAttached && !leftHandMoving) || (rightHandAttached && !rightHandMoving)) && isPulling)
            {
                DirectClimbToIcepicks(); // Renamed to reflect direct movement
                DrainEnergyWhileClimbing();
            }

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
        UpdatePickaxeRotations();
        ApplyScreenShake();
    }

    // New method to update pickaxe rotations
    void UpdatePickaxeRotations()
    {
        // Check if we need to find pickaxe references (in case pickaxes were equipped after Start)
        if ((leftHandAttached && leftPickaxeTransform == null && leftHandTransform != null && leftHandTransform.childCount > 0) ||
            (rightHandAttached && rightPickaxeTransform == null && rightHandTransform != null && rightHandTransform.childCount > 0))
        {
            // Pickaxe might have been equipped since last check - try to find references again
            FindAndStorePickaxeReferences();
        }

        // Process left pickaxe animation if we have a valid reference
        if (leftHandAttached && leftPickaxeTransform != null)
        {
            // Store the original rotation if we haven't already (might happen if pickaxe was equipped after climbing started)
            if (leftPickaxeOriginalRotation == Quaternion.identity)
            {
                leftPickaxeOriginalRotation = leftPickaxeTransform.localRotation;
            }

            if (leftHandMoving)
            {
                // Set initial rotation when moving (pickaxe up)
                Quaternion targetRotation = Quaternion.Euler(startRotationX, 0, 0) * leftPickaxeOriginalRotation;
                leftPickaxeTransform.localRotation = targetRotation;

                // Reset digging in progress
                leftDigInProgress = 0f;
                leftPickaxeDiggingIn = false;
                leftSoundPlayed = false; // Reset sound flag when moving
            }
            else if (leftHandAttached)
            {
                if (!leftPickaxeDiggingIn)
                {
                    // Start digging in animation when hand reaches target
                    leftPickaxeDiggingIn = true;
                }

                if (leftPickaxeDiggingIn)
                {
                    // Only progress the animation if not complete
                    if (leftDigInProgress < 1.0f)
                    {
                        // Progress the digging in animation
                        leftDigInProgress = Mathf.Min(leftDigInProgress + Time.deltaTime * digInSpeed, 1.0f);

                        // Smoothly interpolate between start and end rotation
                        float currentRotationX = Mathf.Lerp(startRotationX, endRotationX, leftDigInProgress);
                        Quaternion targetRotation = Quaternion.Euler(currentRotationX, 0, 0) * leftPickaxeOriginalRotation;
                        leftPickaxeTransform.localRotation = targetRotation;

                        // Play sound exactly once when animation reaches completion point
                        if (leftDigInProgress >= 0.95f && !leftSoundPlayed)
                        {
                            PlayIcepickSound(); // Use universal sound method
                            leftSoundPlayed = true;

                            // Start the delay timer for pulling
                            StartCoroutine(StartPulling(digInDelay));
                        }
                    }
                }
            }
        }
        else if (leftPickaxeTransform != null && leftPickaxeOriginalRotation != Quaternion.identity)
        {
            // Return to original rotation when detached
            leftPickaxeTransform.localRotation = leftPickaxeOriginalRotation;
            leftDigInProgress = 0f;
            leftPickaxeDiggingIn = false;
            leftSoundPlayed = false;
        }

        // Process right pickaxe animation if we have a valid reference
        if (rightHandAttached && rightPickaxeTransform != null)
        {
            // Store the original rotation if we haven't already (might happen if pickaxe was equipped after climbing started)
            if (rightPickaxeOriginalRotation == Quaternion.identity)
            {
                rightPickaxeOriginalRotation = rightPickaxeTransform.localRotation;
            }

            if (rightHandMoving)
            {
                // Set initial rotation when moving (pickaxe up)
                Quaternion targetRotation = Quaternion.Euler(startRotationX, 0, 0) * rightPickaxeOriginalRotation;
                rightPickaxeTransform.localRotation = targetRotation;

                // Reset digging in progress
                rightDigInProgress = 0f;
                rightPickaxeDiggingIn = false;
                rightSoundPlayed = false; // Reset sound flag when moving
            }
            else if (rightHandAttached)
            {
                if (!rightPickaxeDiggingIn)
                {
                    // Start digging in animation when hand reaches target
                    rightPickaxeDiggingIn = true;
                }

                if (rightPickaxeDiggingIn)
                {
                    // Only progress the animation if not complete
                    if (rightDigInProgress < 1.0f)
                    {
                        // Progress the digging in animation
                        rightDigInProgress = Mathf.Min(rightDigInProgress + Time.deltaTime * digInSpeed, 1.0f);

                        // Smoothly interpolate between start and end rotation
                        float currentRotationX = Mathf.Lerp(startRotationX, endRotationX, rightDigInProgress);
                        Quaternion targetRotation = Quaternion.Euler(currentRotationX, 0, 0) * rightPickaxeOriginalRotation;
                        rightPickaxeTransform.localRotation = targetRotation;

                        // Play sound exactly once when animation reaches completion point
                        if (rightDigInProgress >= 0.95f && !rightSoundPlayed)
                        {
                            PlayIcepickSound(); // Use universal sound method
                            rightSoundPlayed = true;

                            // Start the delay timer for pulling
                            StartCoroutine(StartPulling(digInDelay));
                        }
                    }
                }
            }
        }
        else if (rightPickaxeTransform != null && rightPickaxeOriginalRotation != Quaternion.identity)
        {
            // Return to original rotation when detached
            rightPickaxeTransform.localRotation = rightPickaxeOriginalRotation;
            rightDigInProgress = 0f;
            rightPickaxeDiggingIn = false;
            rightSoundPlayed = false;
        }
    }

    private IEnumerator StartPulling(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Start pulling the player
        isPulling = true;
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
        // Fix for allowing same-side reattachment
        if (isLeftHand && leftHandAttached && leftHandMoving)
            return; // Already moving this hand
        if (!isLeftHand && rightHandAttached && rightHandMoving)
            return; // Already moving this hand

        // Check if we need to find pickaxe references first (in case pickaxes were equipped after Start)
        if ((isLeftHand && leftPickaxeTransform == null && leftHandTransform != null && leftHandTransform.childCount > 0) ||
            (!isLeftHand && rightPickaxeTransform == null && rightHandTransform != null && rightHandTransform.childCount > 0))
        {
            // Pickaxe might have been equipped since last check - try to find references again
            FindAndStorePickaxeReferences();
        }

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
                        // Set the target position instead of instantly moving there
                        leftHandTargetPosition = handAttachPosition;
                        leftHandMoving = true; // Hand is now moving to target
                        leftHandAttached = true; // Mark as attached even during movement
                    }
                    else
                    {
                        // Set the target position instead of instantly moving there
                        rightHandTargetPosition = handAttachPosition;
                        rightHandMoving = true; // Hand is now moving to target
                        rightHandAttached = true; // Mark as attached even during movement
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

                        // Initial snap will now happen only once both hands are fully attached
                        Debug.Log("Started climbing");
                    }
                }
                else
                {
                    Debug.LogWarning("No icepick tip found for " + (isLeftHand ? "left" : "right") + " hand");

                    // Fallback to original behavior if tip cannot be found, but still use gradual movement
                    if (isLeftHand)
                    {
                        leftHandTargetPosition = hit.point;
                        leftHandMoving = true;
                        leftHandAttached = true;
                    }
                    else
                    {
                        rightHandTargetPosition = hit.point;
                        rightHandMoving = true;
                        rightHandAttached = true;
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

        if (leftHandAttached && !leftHandMoving)
        {
            targetPosition += leftHandPosition;
            attachedCount++;
        }

        if (rightHandAttached && !rightHandMoving)
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

        // Only proceed if we have a valid target position
        if (targetPosition != Vector3.zero)
        {
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
    }

    void DetachIcepick(bool isLeftHand)
    {
        // Detach the icepick
        if (isLeftHand)
        {
            leftHandAttached = false;
            leftHandMoving = false;
        }
        else
        {
            rightHandAttached = false;
            rightHandMoving = false;
        }

        // Reset pulling state if no hands attached
        if (!leftHandAttached && !rightHandAttached)
        {
            isPulling = false;
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
        leftHandMoving = false;
        rightHandAttached = false;
        rightHandMoving = false;
        isPulling = false; // Reset pulling flag

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
                        isPulling = false; // Reset pulling flag once we've reached the target
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
            else
            {
                // We're very close to target
                isPulling = false; // Reset pulling flag
            }
        }
    }

    void UpdateHandVisuals()
    {
        // Gradual movement for left hand
        if (leftHandAttached)
        {
            if (leftHandMoving)
            {
                // Calculate direction to target
                Vector3 toTarget = leftHandTargetPosition - leftHandTransform.position;
                float distanceToTarget = toTarget.magnitude;

                // Check if we've reached the target
                if (distanceToTarget <= 0.01f)
                {
                    // We've reached the target
                    leftHandTransform.position = leftHandTargetPosition;
                    leftHandPosition = leftHandTargetPosition;
                    leftHandMoving = false;

                    // If this is the first hand to fully attach, do initial snap
                    if (isFirstClimbFrame && !rightHandMoving)
                    {
                        PerformInitialSnap();
                        isFirstClimbFrame = false;
                    }
                }
                else
                {
                    // Move the hand toward the target at the configured speed
                    float moveDistance = Mathf.Min(icepickMoveSpeed * Time.deltaTime, distanceToTarget);
                    Vector3 movement = toTarget.normalized * moveDistance;

                    // Update hand position
                    leftHandTransform.position += movement;
                    leftHandPosition = leftHandTransform.position;
                }
            }
            else
            {
                // Keep hand at the stable position
                leftHandTransform.position = leftHandPosition;
            }
        }
        else
        {
            // Return hand to original position
            leftHandTransform.localPosition = leftHandOriginalLocalPos;
        }

        // Gradual movement for right hand
        if (rightHandAttached)
        {
            if (rightHandMoving)
            {
                // Calculate direction to target
                Vector3 toTarget = rightHandTargetPosition - rightHandTransform.position;
                float distanceToTarget = toTarget.magnitude;

                // Check if we've reached the target
                if (distanceToTarget <= 0.01f)
                {
                    // We've reached the target
                    rightHandTransform.position = rightHandTargetPosition;
                    rightHandPosition = rightHandTargetPosition;
                    rightHandMoving = false;

                    // If this is the first hand to fully attach, do initial snap
                    if (isFirstClimbFrame && !leftHandMoving)
                    {
                        PerformInitialSnap();
                        isFirstClimbFrame = false;
                    }
                }
                else
                {
                    // Move the hand toward the target at the configured speed
                    float moveDistance = Mathf.Min(icepickMoveSpeed * Time.deltaTime, distanceToTarget);
                    Vector3 movement = toTarget.normalized * moveDistance;

                    // Update hand position
                    rightHandTransform.position += movement;
                    rightHandPosition = rightHandTransform.position;
                }
            }
            else
            {
                // Keep hand at the stable position
                rightHandTransform.position = rightHandPosition;
            }
        }
        else
        {
            // Return hand to original position
            rightHandTransform.localPosition = rightHandOriginalLocalPos;
        }
    }

    // Universal method for Icepick Sound
    public void PlayIcepickSound()
    {
        if (icepickSounds.Length == 0) return;

        EventReference soundEventReference = icepickSounds[icepickSoundIndex];

        EventInstance icepickInstance = RuntimeManager.CreateInstance(soundEventReference);
        icepickInstance.start();
        icepickInstance.release();

        // Start screen shake
        screenShakeTimer = screenShakeDuration;

        // Move to the next sound in the array, looping back if necessary
        icepickSoundIndex = (icepickSoundIndex + 1) % icepickSounds.Length;
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