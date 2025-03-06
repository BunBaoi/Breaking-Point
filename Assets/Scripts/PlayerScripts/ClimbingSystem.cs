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
    private PlayerStats playerStats;
    private PlayerMovement playerMovement;

    [Header("Pull Settings")]
    public float pullForce = 20f;
    public float maxPullDistance = 4f;
    public float handReachOffset = 1f;
    public float pullUpForce = 15f;
    public float movementSmoothing = 8f;

    private Vector3 currentVelocity;
    private Vector3 lastMoveDirection;

    private float originalGravity;

    public string climbableTag = "Climbable";

    [Header("Hand Physics")]
    public float handSwayAmount = 0.1f;
    public float handReturnSpeed = 10f;
    public float handStabilityMultiplier = 0.5f;

    [Header("Stamina Settings")]
    public float staminaDrainRate = 10f;
    public float handSlipThreshold = 20f;
    public float slipProbability = 0.1f;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string leftGrabActionName = "LeftGrab";
    [SerializeField] private string rightGrabActionName = "RightGrab";
    private InputAction leftGrab;
    private InputAction rightGrab;
    private InputAction movement;

    // Private variables
    private bool leftHandHolding = false;
    private bool rightHandHolding = false;
    private Vector3 leftHandHoldPosition;
    private Vector3 rightHandHoldPosition;
    private Vector3 leftHandOriginalLocalPos;
    private Vector3 rightHandOriginalLocalPos;
    private bool isExhausted = false;

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
        playerStats = GetComponent<PlayerStats>();
        playerMovement = GetComponent<PlayerMovement>();
        originalGravity = playerMovement.gravity; // Store original gravity value
        SetupInputActions();

        // Store the original local positions of hands
        leftHandOriginalLocalPos = leftHandTransform.localPosition;
        rightHandOriginalLocalPos = rightHandTransform.localPosition;
    }

    void SetupInputActions()
    {
        leftGrab = inputActions.FindAction(leftGrabActionName);
        rightGrab = inputActions.FindAction(rightGrabActionName);
        movement = inputActions.FindAction("Move");

        if (leftGrab != null) leftGrab.Enable();
        if (rightGrab != null) rightGrab.Enable();
        if (movement != null) movement.Enable();

        // Subscribe to input events
        if (leftGrab != null) leftGrab.performed += _ => TryGrab(true);
        if (rightGrab != null) rightGrab.performed += _ => TryGrab(false);
        if (leftGrab != null) leftGrab.canceled += _ => Release(true);
        if (rightGrab != null) rightGrab.canceled += _ => Release(false);
    }

    void Update()
    {
        if (!playerStats.IsAlive) return;

        if (IsHolding())
        {
            playerMovement.SetMovementState(false);
            HandlePulling();
            CheckForSlipping();
        }

        UpdateHandPositions();
    }

    void TryGrab(bool isLeftHand)
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxPullDistance))
        {
            if (hit.collider.CompareTag(climbableTag))
            {
                Vector3 handPosition = isLeftHand ? leftHandTransform.position : rightHandTransform.position;

                if (Vector3.Distance(handPosition, hit.point) <= handReachOffset)
                {
                    if (isLeftHand)
                    {
                        leftHandHolding = true;
                        leftHandHoldPosition = hit.point;
                    }
                    else
                    {
                        rightHandHolding = true;
                        rightHandHoldPosition = hit.point;
                    }

                    // Disable gravity when grabbing
                    playerMovement.SetApplyGravity(false);
                    playerMovement.SetMovementState(false);
                }
            }
        }
    }

    void Release(bool isLeftHand)
    {
        if (isLeftHand)
        {
            leftHandHolding = false;
        }
        else
        {
            rightHandHolding = false;
        }

        if (!IsHolding())
        {
            playerMovement.SetApplyGravity(true);
            playerMovement.SetMovementState(true);
            lastMoveDirection = Vector3.zero;
        }
    }

    void HandlePulling()
    {
        if (!IsHolding()) return;

        Vector3 pullPoint = Vector3.zero;
        int activeHands = 0;

        if (leftHandHolding)
        {
            pullPoint += leftHandHoldPosition;
            activeHands++;
        }
        if (rightHandHolding)
        {
            pullPoint += rightHandHoldPosition;
            activeHands++;
        }

        if (activeHands > 0)
        {
            pullPoint /= activeHands;
            Vector3 toPoint = pullPoint - transform.position;
            float distance = toPoint.magnitude;

            if (distance > 0.1f)
            {
                // Calculate basic pull direction
                Vector3 pullDirection = toPoint.normalized;
                pullDirection.y += pullUpForce * Time.deltaTime;
                pullDirection = Vector3.Lerp(lastMoveDirection, pullDirection.normalized, Time.deltaTime * movementSmoothing);

                // Simple distance-based force calculation
                float pullStrength = Mathf.Clamp01(1f - (distance / maxPullDistance));
                Vector3 moveVector = pullDirection * pullForce * pullStrength * Time.deltaTime;

                // Apply movement directly with minimal smoothing
                controller.Move(moveVector);
                lastMoveDirection = pullDirection;
            }
        }
    }

    void CheckForSlipping()
    {
        if (isExhausted)
        {
            if (Random.value < slipProbability * Time.deltaTime)
            {
                if (leftHandHolding && Random.value > 0.5f)
                {
                    Release(true);
                    Debug.Log("Left hand slipped!");
                }
                else if (rightHandHolding)
                {
                    Release(false);
                    Debug.Log("Right hand slipped!");
                }
            }
        }
    }

    void UpdateHandPositions()
    {
        if (leftHandHolding)
        {
            leftHandTransform.position = Vector3.Lerp(
                leftHandTransform.position,
                leftHandHoldPosition,
                Time.deltaTime * handReturnSpeed
            );
        }
        else
        {
            leftHandTransform.localPosition = Vector3.Lerp(
                leftHandTransform.localPosition,
                leftHandOriginalLocalPos,
                Time.deltaTime * handReturnSpeed
            );
        }

        if (rightHandHolding)
        {
            rightHandTransform.position = Vector3.Lerp(
                rightHandTransform.position,
                rightHandHoldPosition,
                Time.deltaTime * handReturnSpeed
            );
        }
        else
        {
            rightHandTransform.localPosition = Vector3.Lerp(
                rightHandTransform.localPosition,
                rightHandOriginalLocalPos,
                Time.deltaTime * handReturnSpeed
            );
        }
    }

    public bool IsHolding()
    {
        return leftHandHolding || rightHandHolding;
    }

    private void OnEnable()
    {
        SetupInputActions();
        if (playerMovement != null)
        {
            playerMovement.SetMovementState(true);
        }
    }

    private void OnDisable()
    {
        // Release both hands when system is disabled
        if (leftHandHolding)
        {
            Release(true);
        }
        if (rightHandHolding)
        {
            Release(false);
        }

        // Disable input actions
        if (leftGrab != null)
        {
            leftGrab.Disable();
            leftGrab.performed -= _ => TryGrab(true);
            leftGrab.canceled -= _ => Release(true);
        }
        if (rightGrab != null)
        {
            rightGrab.Disable();
            rightGrab.performed -= _ => TryGrab(false);
            rightGrab.canceled -= _ => Release(false);
        }

        // Ensure normal movement is restored
        if (playerMovement != null)
        {
            playerMovement.SetMovementState(true);
            playerMovement.SetApplyGravity(true);
        }
    }
}