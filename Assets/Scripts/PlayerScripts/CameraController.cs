using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string lookName = "Look";

    private InputAction lookAction;
    private Vector2 lookInput;
    public float xRotation = 0f;

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
        Cursor.lockState = CursorLockMode.Locked;

        // Get Look action from Input Action Asset
        lookAction = inputActions.FindAction(lookName);

        if (lookAction != null)
        {
            lookAction.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{lookName}' not found in Input Action Asset!");
        }
    }

    void Update()
    {
        HandleLook();
    }

    void HandleLook()
    {
        // Read input from Input Action Asset
        lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
