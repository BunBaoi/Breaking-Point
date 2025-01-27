using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    [Header("Cinemachine")]
    public CinemachineVirtualCamera virtualCamera;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string lookName = "Look";

    [SerializeField] private bool canLook = true;
    private InputAction lookAction;
    private Vector2 lookInput;
    public float xRotation = 0f;

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
        Cursor.lockState = CursorLockMode.Locked;

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
        if (canLook)
        {
            HandleLook();
        }
    }

    void HandleLook()
    {
        if (playerBody == null || virtualCamera == null || lookAction == null) return;

        lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Rotate the Cinemachine Virtual Camera for up/down look
        virtualCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotate the player body for left/right look
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void SetLookState(bool state)
    {
        canLook = state;
    }
}
