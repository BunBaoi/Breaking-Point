using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerStats;
//using static QTEMechanic;

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
    public float walkSpeed = 12f;
    public float sprintSpeed = 20f;
    public float gravity = -9.81f;
    public bool IsSprint = false;
    public float playerHeight;

    [Header("Testing Purposes")]
    public bool canMove = true;

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
        // If inputActions is not assigned via the inspector, load it from the Resources/Keybinds folder
        if (inputActions == null)
        {
            // Load from the "Keybinds" folder in Resources
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

        if (movement != null)
        {
            movement.Enable(); // Enable the action
        }
        else
        {
            Debug.LogError($"Input action '{movementName}' not found in Input Action Asset!");
        }

        if (sprint != null)
        {
            sprint.Enable(); // Enable the action
        }
        else
        {
            Debug.LogError($"Input action '{sprintName}' not found in Input Action Asset!");
        }
    }

    void Update()
    {
        if (canMove)
        {
            HandleGroundMovement();
            HandleSprint();
        }

        ApplyGravity();
        OxyOutputRate();
        QTEControl();
    }

    void HandleGroundMovement()
    {
        // Read movement input from InputActionAsset
        moveInput = movement.ReadValue<Vector2>();

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * walkSpeed * Time.deltaTime);
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
            velocity.y = 0; // Reset vertical velocity when gravity is not applied
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
            walkSpeed = 12f;
            IsSprint = false;
        }
    }

    public void QTEControl()
    {
        if (Input.GetKeyDown(KeyCode.F) && playerStats.stateOfPlayer == PlayerStatus.QTE)
        {
            qTEMechanicScript.QTEMove();
            canMove = false;
            Debug.Log("Player Movement Locked");
            playerStats.QTEState = true;

        }
    }

    public void OxyOutputRate()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            playerStats.OxygenTankRefillRate++;
            //Debug.log("Rate Up");
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate--;
            //Debug.log("Rate Down");

            // Need to add condition that the rate can't go negative

        }
    }

    public void SetMovementState(bool state)
    {
        canMove = state;
    }
}