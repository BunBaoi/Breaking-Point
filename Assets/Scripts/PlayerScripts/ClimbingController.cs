using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbingController : MonoBehaviour
{
    [Header("Climbing Parameters")]
    public float climbSpeed = 5f;
    public float maxArmReach = 2f;
    public float pullForce = 20f;

    [Header("Hand Transforms")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform cameraTransform;

    [Header("Climbing States")]
    private bool leftHandHolding = false;
    private bool rightHandHolding = false;
    private Vector3 leftHandHoldPosition;
    private Vector3 rightHandHoldPosition;
    private Vector3 leftHandOriginalLocalPos;
    private Vector3 rightHandOriginalLocalPos;

    private CharacterController characterController;
    private PlayerMovement playerMovement;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        // Store original hand positions
        leftHandOriginalLocalPos = leftHandTransform.localPosition;
        rightHandOriginalLocalPos = rightHandTransform.localPosition;
    }

    public bool TryGrab(bool isLeftHand, string climbableTag)
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
                return true;
            }
        }
        return false;
    }

    public void Release(bool isLeftHand)
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

    public void HandleClimbingMovement()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 climbDirection = (cameraTransform.up * vertical + cameraTransform.right * horizontal).normalized;
        Vector3 movement = climbDirection * climbSpeed * Time.deltaTime;

        // Apply movement
        characterController.Move(movement);

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

    private void PullTowardsPoint(Vector3 point)
    {
        Vector3 pullDirection = (point - transform.position).normalized;
        characterController.Move(pullDirection * pullForce * Time.deltaTime);
    }

    public void UpdateHandPositions()
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

    // Reset climbing state
    public void ResetClimbingState()
    {
        leftHandHolding = false;
        rightHandHolding = false;
    }
}
