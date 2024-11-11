using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    private PlayerStats playerStats;
    public float walkSpeed = 12f;
    public float climbSpeed = 5f;
    public float gravity = -9.81f;
    public bool IsSprint = false;
    public float playerHeight;
    public float maxArmReach = 2f;
    public float pullForce = 20f;
    public string climbableTag = "Climbable";

    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform cameraTransform;

    Vector3 velocity;
    bool leftHandHolding = false;
    bool rightHandHolding = false;
    Vector3 leftHandHoldPosition;
    Vector3 rightHandHoldPosition;
    Vector3 leftHandOriginalLocalPos;
    Vector3 rightHandOriginalLocalPos;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        leftHandOriginalLocalPos = leftHandTransform.localPosition;
        rightHandOriginalLocalPos = rightHandTransform.localPosition;
    }

    void Update()
    {
        HandleClimbingInput();
        if (IsHolding())
        {
            HandleClimbingMovement();
        }
        else
        {
            HandleGroundMovement();
        }
        ApplyGravity();
        UpdateHandPositions();
        sprint();
        OxyOuputRate();
    }

    void HandleClimbingInput()
    {
        // Left hand
        if (Input.GetMouseButtonDown(0))
        {
            TryGrab(true);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Release(true);
        }

        // Right hand
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
        RaycastHit hit;
        Vector3 rayOrigin = isLeftHand ? leftHandTransform.position : rightHandTransform.position;

        if (Physics.Raycast(rayOrigin, cameraTransform.forward, out hit, maxArmReach))
        {
            if (hit.collider.CompareTag(climbableTag))
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

    bool IsHolding()
    {
        return leftHandHolding || rightHandHolding;
    }

    void HandleClimbingMovement()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 climbDirection = (cameraTransform.up * vertical + cameraTransform.right * horizontal).normalized;
        Vector3 movement = climbDirection * climbSpeed * Time.deltaTime;

        // Apply movement
        controller.Move(movement);

        // Pull towards hold points
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
        if (leftHandHolding)
        {
            leftHandTransform.position = leftHandHoldPosition;
        }
        else
        {
            leftHandTransform.localPosition = Vector3.Lerp(leftHandTransform.localPosition, leftHandOriginalLocalPos, Time.deltaTime * 10f);
        }

        if (rightHandHolding)
        {
            rightHandTransform.position = rightHandHoldPosition;
        }
        else
        {
            rightHandTransform.localPosition = Vector3.Lerp(rightHandTransform.localPosition, rightHandOriginalLocalPos, Time.deltaTime * 10f);
        }
    }

    private void sprint()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            walkSpeed = 20f;
            IsSprint = true;
        }
        else
        {
            walkSpeed = 12f;
            IsSprint = false;
        }
    }

    public void OxyOuputRate()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate++;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            playerStats.OxygenTankRefillRate--;
        }
    }
}