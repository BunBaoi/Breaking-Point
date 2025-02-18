using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player")]
    public CharacterController controller;
    private PlayerStats playerStats;

    [Header("Settings")]
    public float walkSpeed = 12f;
    public float sprintSpeed = 20f;
    public float gravity = -9.81f;
    public bool IsSprint = false;

    [Header("Testing Purposes")]
    [SerializeField] private bool canMove = true;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string movementName = "Move";
    [SerializeField] private string sprintName = "Sprint";

    private InputAction movement;
    private InputAction sprint;

    private Vector3 velocity;
    private Vector2 moveInput;

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
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
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

    public void OxyOutputRate()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate++;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            playerStats.OxygenTankRefillRate--;
        }
    }

    public void SetMovementState(bool state)
    {
        canMove = state;
    }
}