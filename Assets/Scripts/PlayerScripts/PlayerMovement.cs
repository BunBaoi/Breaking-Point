using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using FMOD.Studio;
using static PlayerStats;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player")]
    public QTEvent qTEvent;
    public QTEMechanicScript qTEMechanicScript;
    public CharacterController controller;
    private PlayerStats playerStats;
    public Camera playerCamera;

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
    public float zoomFOV, normalFOV;
    public RawImage binocularsUI;

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

    [SerializeField] private bool applyGravity = true;
    private bool wasGravityApplied = false;

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
        playerPos = GameObject.FindWithTag("Player");

        if (movement != null) movement.Enable();
        if (sprint != null) sprint.Enable();

        GameObject qteObject = GameObject.FindWithTag("QTE");
        if (qteObject != null)
            qTEMechanicScript = qteObject.GetComponent<QTEMechanicScript>();

        GameObject qteUIObject = GameObject.FindWithTag("QTEUI");
        if (qteUIObject != null)
            qTEvent = qteUIObject.GetComponent<QTEvent>();

        targetPos = GameObject.FindWithTag("StartPos");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Game") || scene.name.Contains("Level"))
        {
            // Try to find and assign the components safely
            GameObject qteObject = GameObject.FindWithTag("QTE");
            if (qteObject != null)
                qTEMechanicScript = qteObject.GetComponent<QTEMechanicScript>();

            GameObject qteUIObject = GameObject.FindWithTag("QTEUI");
            if (qteUIObject != null)
                qTEvent = qteUIObject.GetComponent<QTEvent>();

            targetPos = GameObject.FindWithTag("StartPos");
        }
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
        binocularZoom();
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

            // Debug log for gravity application
            // Debug.Log($"Applying Gravity: {velocity.y} (gravity: {gravity})");
        }
        else
        {
            velocity.y = 0;

            // Debug log when gravity is not applied
            // Debug.Log("Gravity disabled: velocity.y set to 0");
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
            qTEMechanicScript.QTEMechanicScriptActive = true;
        }
    }

    public void SetMovementState(bool state)
    {
        canMove = state;
    }

    // Left Footstep
    public void PlayLeftFootstepSound()
    {
        if (leftFootstepEvents.Length == 0) return;

        Transform footTransform = transform;

        EventReference soundEventReference = leftFootstepEvents[leftFootstepIndex];

        EventInstance footstepInstance = RuntimeManager.CreateInstance(soundEventReference);

        // Set the 3D attributes for the footstep sound based on the foot's position
        Vector3 footPosition = footTransform.position + new Vector3(0, 0, 0.3f);
        footstepInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(footPosition));

        footstepInstance.start();
        footstepInstance.release();

        // Move to the next footstep in the array, looping back if necessary
        leftFootstepIndex = (leftFootstepIndex + 1) % leftFootstepEvents.Length;
    }

    // Right Footstep
    public void PlayRightFootstepSound()
    {
        if (rightFootstepEvents.Length == 0) return;

        Transform footTransform = transform;

        EventReference soundEventReference = rightFootstepEvents[rightFootstepIndex];

        EventInstance footstepInstance = RuntimeManager.CreateInstance(soundEventReference);

        // Set the 3D attributes for the footstep sound based on the foot's position
        Vector3 footPosition = footTransform.position + new Vector3(0, 0, 0.3f);
        footstepInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(footPosition));

        footstepInstance.start();
        footstepInstance.release();

        // Move to the next footstep in the array, looping back if necessary
        rightFootstepIndex = (rightFootstepIndex + 1) % rightFootstepEvents.Length;
    }

    public void binocularZoom()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            //Debug.Log("Z button pressed");
            playerCamera.fieldOfView = zoomFOV;
            binocularsUI.enabled = true;
        }
        else
        {
            playerCamera.fieldOfView = normalFOV;
            binocularsUI.enabled = false;
        }
    }
    
}