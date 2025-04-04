using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using FMOD.Studio;
using static PlayerStats;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player")]
    public QTEvent qTEvent;
    public QTEMechanicScript qTEMechanicScript;
    public CharacterController controller;
    private PlayerStats playerStats;

    public GameObject targetPos;
    public GameObject playerPos;

    [Header("Settings")]
    public float objectSpeed = 3;
    public float defaultWalkSpeed = 12f;
    public float walkSpeed = 12f;
    public float sprintSpeed = 20f;
    public float gravity = -9.81f;
    public bool IsSprint = false;
    public float playerHeight;

    [Header("Testing Purposes")]
    public bool canMove = true;

    [Header("Footstep Sounds (FMOD)")]
    public EventReference[] leftFootstepEvents; // Left footstep sounds
    public EventReference[] rightFootstepEvents; // Right footstep sounds
    private int leftFootstepIndex = 0;
    private int rightFootstepIndex = 0;

    [Header("Animator")]
    public Animator animator;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string movementName = "Move";
    [SerializeField] private string sprintName = "Sprint";

    private InputAction movement;
    private InputAction sprint;

    private Vector3 velocity;
    private Vector2 moveInput;

    private bool applyGravity = true;

    void Awake()
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
        movement = inputActions.FindAction(movementName);
        sprint = inputActions.FindAction(sprintName);

        if (movement != null) movement.Enable();
        if (sprint != null) sprint.Enable();
    }

    void Update()
    {
        if (canMove)
        {
            HandleGroundMovement();
            HandleSprint();
        }

        UpdateAnimation();

        ApplyGravity();
        QTEControl();
    }

    void HandleGroundMovement()
    {
        moveInput = movement.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Move the player
        controller.Move(move * walkSpeed * Time.deltaTime);
    }

    void UpdateAnimation()
    {
        // Calculate the current speed based on movement input and sprinting
        float currentSpeed = moveInput.magnitude * (IsSprint ? 1f : 0.5f);

        if (!canMove)
        {
            currentSpeed = 0f;
        }

        animator.SetFloat("speed", currentSpeed);
    }

    void ApplyGravity()
    {
        if (applyGravity)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
        else
        {
            velocity.y = 0;
        }
    }

    public void SetApplyGravity(bool apply)
    {
        applyGravity = apply;
    }

    private void HandleSprint()
    {
        if (sprint.IsPressed())
        {
            walkSpeed = sprintSpeed;
            IsSprint = true;
        }
        else
        {
            walkSpeed = defaultWalkSpeed;
            IsSprint = false;
        }
    }

    public void QTEControl()
    {
        if (Input.GetKeyDown(KeyCode.F) && playerStats.stateOfPlayer == PlayerStatus.QTE)
        {
            qTEMechanicScript.QTEMove();
            canMove = false;
            playerStats.QTEState = true;
        }
    }

    public void SetMovementState(bool state)
    {
        canMove = state;
    }

    // Animation event for Left Footstep
    public void PlayLeftFootstepSound()
    {
        if (leftFootstepEvents.Length == 0) return;

        EventReference soundEventReference = leftFootstepEvents[leftFootstepIndex];

        EventInstance footstepInstance = RuntimeManager.CreateInstance(soundEventReference);
        footstepInstance.start();
        footstepInstance.release();

        // Move to the next footstep in the array, looping back if necessary
        leftFootstepIndex = (leftFootstepIndex + 1) % leftFootstepEvents.Length;
    }

    // Animation event for Right Footstep
    public void PlayRightFootstepSound()
    {
        if (rightFootstepEvents.Length == 0) return;

        EventReference soundEventReference = rightFootstepEvents[rightFootstepIndex];

        EventInstance footstepInstance = RuntimeManager.CreateInstance(soundEventReference);
        footstepInstance.start();
        footstepInstance.release();

        // Move to the next footstep in the array, looping back if necessary
        rightFootstepIndex = (rightFootstepIndex + 1) % rightFootstepEvents.Length;
    }
}