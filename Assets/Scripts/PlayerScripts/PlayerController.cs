using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static QTEMechanic;

public class PlayerController : MonoBehaviour
{
    public QTEventUI qTEvent;
    public QTEMechanic qTEMechanic;
    public CharacterController controller;
    public PlayerStats playerStats;

    public float objectSpeed = 3;
    private float moveDuration = 5f;
    public Vector3 target = new Vector3(2, 3, 4);


    public float walkSpeed;
    public float gravity = -9.81f;
    public float climbSpeed = 5f;
    public bool IsSprint = false;
    public float playerHeight;
    public float maxArmReach = 2f;
    public float pullForce = 20f;
    public string climbableTag = "Climbable";

    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform cameraTransform;

    Vector3 velocity;
    Rigidbody rb;
    bool leftHandHolding = false;
    bool rightHandHolding = false;
    Vector3 leftHandHoldPosition;
    Vector3 rightHandHoldPosition;
    Vector3 leftHandOriginalLocalPos;
    Vector3 rightHandOriginalLocalPos;

    //public float maxSlopeAngle;
    //private RaycastHit slopehit;

    void Start()
    {
        leftHandOriginalLocalPos = leftHandTransform.localPosition;
        rightHandOriginalLocalPos = rightHandTransform.localPosition;
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * walkSpeed * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        HandleClimbingInput();
        if (IsHolding())
        {
            HandleClimbingMovement();
        }
        else
        {
            HandleGroundMovement();
        }

        sprint();
        OxyOuputRate();
        QTEControl();
        ApplyGravity();
        UpdateHandPositions();
    }

    private void sprint()
    {
        if(Input.GetKey(KeyCode.LeftShift))
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

    public void QTEControl()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Z Key");
            qTEvent.QTEActive(); // Delete QTE UI
            //MoveToPosition(target);
            //qTEMechanic.MoveBlock();
            //StartCoroutine(MoveCube(target));

            qTEMechanic.PositionOfPlayer = PlayerPos.Pos2;

            //transform.position = Vector3.Lerp(transform.position, target, 10);
        }
    }

    // QTE //
    public void MoveToPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
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


    // Climbing Mechanic //
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

    // Oxygen Mechanic //
    public void OxyOuputRate()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            playerStats.OxygenTankRefillRate++;
            //Debug.log("Rate Up");
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate--;
            //Debug.log("Rate Down");
        }
    }

}
