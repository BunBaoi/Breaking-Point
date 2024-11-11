using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    [Header("Character Components")]
    public CharacterController controller;
    private PlayerStats playerStats;
    public GameObject objectPlayer;
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform cameraTransform;
    public float playerHeight;

    [Header("Movement Settings")]
    public float walkSpeed;
    public float climbSpeed = 5f;
    public float gravity = -9.81f;
    public bool IsSprint = false;
    public float sprintSpeed = 20f;

    [Header("Climbing Settings")]
    public float maxClimbDistance = 4f; // Maximum distance for climb detection
    public float handReachOffset = 1f; // How far hands can reach from hit point
    public float pullForce = 20f;
    public float staminaDrainRate = 10f;
    public float staminaRegenRate = 5f;
    public float maxStamina = 100f;
    public float currentStamina;
    public float handSlipThreshold = 20f;
    public float slipProbability = 0.1f;
    public string climbableTag = "Climbable";

    [Header("Hand Physics")]
    public float handSwayAmount = 0.1f;
    public float handReturnSpeed = 10f;
    public float handStabilityMultiplier = 0.5f;

    private Vector3 velocity;
    private bool leftHandHolding = false;
    private bool rightHandHolding = false;
    private Vector3 leftHandHoldPosition;
    private Vector3 rightHandHoldPosition;
    private Vector3 leftHandOriginalLocalPos;
    private Vector3 rightHandOriginalLocalPos;
    private float timeSinceLastGrab;
    private bool isExhausted = false;

    [Header("Quick Time Event")]
    public QTEventUI qTEvent;
    public QTEMechanic qTEMechanic;
    public float QTEMoveSpeed = 3;
    public float move = 12;
    private float moveDuration = 5f;
    public Vector3 target = new Vector3(2, 3, 4);

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        leftHandOriginalLocalPos = leftHandTransform.localPosition;
        rightHandOriginalLocalPos = rightHandTransform.localPosition;
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (playerStats.IsAlive)
        {
            HandleClimbingInput();
            HandleStamina();

            if (IsHolding())
            {
                HandleClimbingMovement();
                CheckForSlipping();
            }
            else
            {
                HandleGroundMovement();
            }

            ApplyGravity();
            UpdateHandPositions();
            HandleSprint();
            OxyOuputRate();
            QTEControl();
        }

        //controller.Move(move * QTEMoveSpeed * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleStamina()
    {
        if (IsHolding())
        {
            float movementFactor = (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0) ? 1.5f : 1f;
            currentStamina -= staminaDrainRate * movementFactor * Time.deltaTime;
            playerStats.DrainStamina(staminaDrainRate * movementFactor * Time.deltaTime);
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            playerStats.RegenerateStamina(staminaRegenRate * Time.deltaTime);
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        isExhausted = currentStamina < handSlipThreshold;
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

    void HandleClimbingInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryGrab(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Release(true);
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryGrab(false);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            Release(false);
        }
    }

    void TryGrab(bool isLeftHand)
    {
        if (currentStamina <= 0) return;

        // Cast ray from camera center
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxClimbDistance))
        {
            if (hit.collider.CompareTag(climbableTag))
            {
                Vector3 handPosition = isLeftHand ? leftHandTransform.position : rightHandTransform.position;

                // Check if hand is within reach of the hit point
                if (Vector3.Distance(handPosition, hit.point) <= handReachOffset)
                {
                    // Add stability-based variation to grab point
                    float stabilityFactor = Mathf.Lerp(handStabilityMultiplier, 1f, currentStamina / maxStamina);
                    Vector3 randomOffset = Random.insideUnitSphere * (1f - stabilityFactor) * handSwayAmount;
                    Vector3 finalGrabPoint = hit.point + randomOffset;

                    if (isLeftHand)
                    {
                        leftHandHolding = true;
                        leftHandHoldPosition = finalGrabPoint;
                    }
                    else
                    {
                        rightHandHolding = true;
                        rightHandHoldPosition = finalGrabPoint;
                    }

                    currentStamina -= 5f;
                    playerStats.DrainStamina(5f);
                    timeSinceLastGrab = 0f;
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
    }

    public bool IsHolding()
    {
        return leftHandHolding || rightHandHolding;
    }

    void HandleClimbingMovement()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        float staminaFactor = Mathf.Lerp(0.3f, 1f, currentStamina / maxStamina);
        Vector3 climbDirection = (cameraTransform.up * vertical + cameraTransform.right * horizontal).normalized;
        Vector3 movement = climbDirection * climbSpeed * staminaFactor * Time.deltaTime;

        if (isExhausted)
        {
            movement += Random.insideUnitSphere * handSwayAmount * Time.deltaTime;
        }

        controller.Move(movement);

        if (leftHandHolding)
        {
            PullTowardsPoint(leftHandHoldPosition);
        }
        if (rightHandHolding)
        {
            PullTowardsPoint(rightHandHoldPosition);
        }
    }

    void PullTowardsPoint(Vector3 point)
    {
        Vector3 pullDirection = (point - transform.position).normalized;
        controller.Move(pullDirection * pullForce * Time.deltaTime);
    }

    void HandleGroundMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * walkSpeed * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (!IsHolding())
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0;
        }
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateHandPositions()
    {
        float handStability = Mathf.Lerp(0.5f, 1f, currentStamina / maxStamina);

        if (leftHandHolding)
        {
            Vector3 targetPos = leftHandHoldPosition;
            if (isExhausted)
            {
                targetPos += Random.insideUnitSphere * handSwayAmount;
            }
            leftHandTransform.position = Vector3.Lerp(leftHandTransform.position, targetPos, handStability);
        }
        else
        {
            leftHandTransform.localPosition = Vector3.Lerp(leftHandTransform.localPosition, leftHandOriginalLocalPos, Time.deltaTime * handReturnSpeed);
        }

        if (rightHandHolding)
        {
            Vector3 targetPos = rightHandHoldPosition;
            if (isExhausted)
            {
                targetPos += Random.insideUnitSphere * handSwayAmount;
            }
            rightHandTransform.position = Vector3.Lerp(rightHandTransform.position, targetPos, handStability);
        }
        else
        {
            rightHandTransform.localPosition = Vector3.Lerp(rightHandTransform.localPosition, rightHandOriginalLocalPos, Time.deltaTime * handReturnSpeed);
        }
    }

    private void HandleSprint()
    {
        if (Input.GetKey(KeyCode.LeftShift) && !IsHolding() && playerStats.CurrentStamina > 10f)
        {
            walkSpeed = sprintSpeed;
            IsSprint = true;
        }
        else
        {
            walkSpeed = 12f;
            IsSprint = false;
        }
    }

    public bool CheckClimbablePoint(Vector3 position, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxClimbDistance))
        {
            hitPoint = hit.point;
            return hit.collider.CompareTag(climbableTag) &&
                   Vector3.Distance(position, hit.point) <= handReachOffset;
        }
        return false;
    }

    public void MoveToPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public void QTEControl()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Z Key");
            qTEvent.QTEActive(); // Delete QTE UI
            //MoveToPosition(target);
            //qTEMechanic.MoveBlock();
            QTEMoveToTarget();
            //qTEMechanic.QTEMoveToTarget();
            //StartCoroutine(MoveCube(target));

            //qTEMechanic.PositionOfPlayer = PlayerPos.Pos2;

            //transform.position = Vector3.Lerp(transform.position, target, 10);

        }
    }

    IEnumerator MoveCube(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float timeElapsed = 0;
        while (timeElapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / moveDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;

    }

    public void OxyOuputRate()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            playerStats.OxygenTankRefillRate++;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate--;
        }

    }
    public void QTEMoveToTarget()
    {
        //if (Pos1 != null)
        //{
        //    Vector3 targetPosition = Pos1.position;
        //    transform.position = Vector3.MoveTowards(transform.position, targetPosition, QTEMoveSpeed * Time.deltaTime);
        //}
        transform.position = Vector3.MoveTowards(transform.position, target, QTEMoveSpeed * Time.deltaTime);
        Debug.Log("QTEMOVE");
    }
}