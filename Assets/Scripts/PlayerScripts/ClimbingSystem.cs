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

    [Header("Climbing Settings")]
    public float climbSpeed = 3f;
    public float maxClimbDistance = 4f;
    public string climbableTag = "Climbable";

    [Header("Smoothing Settings")]
    public float movementSmoothTime = 0.15f; // Controls how quickly movement is smoothed
    public float directionSmoothTime = 0.1f; // Controls how quickly direction changes are smoothed
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

    // Smoothing variables
    private Vector3 currentMoveVelocity = Vector3.zero;
    private Vector3 smoothedMoveDirection = Vector3.zero;
    private Vector3 directionSmoothVelocity = Vector3.zero;

    // Debug and state management
    private bool isActivelyClimbing = false;

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
            SmoothClimbToIcepicks();
        }
        else
        {
            // Reset smoothing variables when not climbing
            currentMoveVelocity = Vector3.zero;
            smoothedMoveDirection = Vector3.zero;
            directionSmoothVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        UpdateHandVisuals();
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
                // Attach the icepick
                if (isLeftHand)
                {
                    leftHandAttached = true;
                    leftHandPosition = hit.point;
                }
                else
                {
                    rightHandAttached = true;
                    rightHandPosition = hit.point;
                }

                // Disable normal movement and gravity when climbing
                if (IsClimbing() && !isActivelyClimbing)
                {
                    playerMovement.SetMovementState(false);
                    playerMovement.SetApplyGravity(false);
                    isActivelyClimbing = true;

                    // Reset smoothing variables when starting to climb
                    currentMoveVelocity = Vector3.zero;
                    smoothedMoveDirection = Vector3.zero;
                    directionSmoothVelocity = Vector3.zero;

                    Debug.Log("Started climbing");
                }
            }
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
    }

    bool IsClimbing()
    {
        return leftHandAttached || rightHandAttached;
    }

    void SmoothClimbToIcepicks()
    {
        // Get the target position (average of attached icepicks)
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

            // Get vector to target
            Vector3 toTarget = targetPosition - transform.position;

            // Only climb if we're not too close
            if (toTarget.magnitude > 0.1f)
            {
                // Calculate raw direction
                Vector3 rawDirection = toTarget.normalized;

                // Add slight upward bias for natural climbing
                rawDirection.y += upwardBias;
                rawDirection.Normalize();

                // Smooth the direction changes
                smoothedMoveDirection = Vector3.SmoothDamp(
                    smoothedMoveDirection,
                    rawDirection,
                    ref directionSmoothVelocity,
                    directionSmoothTime
                );

                // Calculate desired movement with speed
                Vector3 desiredMove = smoothedMoveDirection * climbSpeed;

                // Smooth the movement and apply
                Vector3 smoothedMove = Vector3.SmoothDamp(
                    controller.velocity,
                    desiredMove,
                    ref currentMoveVelocity,
                    movementSmoothTime
                );

                // Prevent overshooting by reducing speed when close
                float distanceRatio = Mathf.Clamp01(toTarget.magnitude / 1.5f);
                smoothedMove *= distanceRatio;

                // Apply a maximum speed limit to prevent any potential flying
                float maxSpeed = 5f;
                if (smoothedMove.magnitude > maxSpeed)
                {
                    smoothedMove = smoothedMove.normalized * maxSpeed;
                }

                // Apply the smoothed movement
                controller.Move(smoothedMove * Time.fixedDeltaTime);

                // Visualization for debugging
                Debug.DrawLine(transform.position, targetPosition, Color.yellow);
                Debug.DrawRay(transform.position, smoothedMove.normalized * 2f, Color.green);
            }
        }
    }

    void UpdateHandVisuals()
    {
        // Smooth hand visual updates for left hand
        if (leftHandAttached)
        {
            // Move hand to icepick position
            leftHandTransform.position = Vector3.Lerp(
                leftHandTransform.position,
                leftHandPosition,
                Time.deltaTime * 10f
            );
        }
        else
        {
            // Return hand to original position
            leftHandTransform.localPosition = Vector3.Lerp(
                leftHandTransform.localPosition,
                leftHandOriginalLocalPos,
                Time.deltaTime * 10f
            );
        }

        // Smooth hand visual updates for right hand
        if (rightHandAttached)
        {
            // Move hand to icepick position
            rightHandTransform.position = Vector3.Lerp(
                rightHandTransform.position,
                rightHandPosition,
                Time.deltaTime * 10f
            );
        }
        else
        {
            // Return hand to original position
            rightHandTransform.localPosition = Vector3.Lerp(
                rightHandTransform.localPosition,
                rightHandOriginalLocalPos,
                Time.deltaTime * 10f
            );
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