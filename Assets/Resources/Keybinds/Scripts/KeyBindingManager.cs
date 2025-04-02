using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Linq;

public class KeyBindingManager : MonoBehaviour
{
    public static KeyBindingManager Instance;

    [SerializeField] private KeyBindingData keyBindingData;
    public InputActionAsset inputActionAsset;

    private string lastDeviceUsed = "Keyboard & Mouse"; // Default to keyboard & mouse

    public static event System.Action OnInputDeviceChanged;

    private Vector2 lastMousePosition;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        string previousDevice = lastDeviceUsed;
        DetectLastInputDevice();

        if (previousDevice != lastDeviceUsed)
        {
            OnInputDeviceChanged?.Invoke(); // Notify listeners (like UI elements) of the change
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Debug.Log("Controller is connected: " + gamepad.displayName);
        }
        else
        {
            Debug.Log("No controller detected.");
        }

        // Debugging: Track which device is being used
        if (lastDeviceUsed == "Gamepad")
        {
            Debug.Log("Controller is detected and in use.");
        }
        else if (lastDeviceUsed == "Keyboard & Mouse")
        {
            Debug.Log("Keyboard & Mouse are in use.");
        }
        else
        {
            Debug.Log("Unknown device in use.");
        }
    }

    public KeyBinding GetKeybinding(string actionName)
    {
        foreach (var binding in keyBindingData.keyBindings)
        {
            if (binding.actionName == actionName)
                return binding;
        }
        return null;
    }

    public string GetSanitisedKeyName(string keyName)
    {
        return keyName
            .Replace("<Keyboard>/", "")
            .Replace("<Gamepad>/", "")
            .Replace("<XInputController>/", "")
            .Replace("<Mouse>/", "")
            .Replace("leftButton", "LeftClick")
            .Replace("rightButton", "RightClick")
            .Replace("middleButton", "MiddleClick")
            .Replace("forwardButton", "MouseButton5")
            .Replace("backButton", "MouseButton4")
            .Replace("scroll/y", "ScrollUpDown")
            .Replace("dpad/y", "dpadUpDown")
            .Replace("dpad/x", "dpadLeftRight")
            .Replace("dpad/up", "dpadUp")
            .Replace("dpad/down", "dpadDown")
            .Replace("dpad/left", "dpadLeft")
            .Replace("dpad/right", "dpadRight")
            .Replace("leftStick/up", "leftStickUp")
            .Replace("leftStick/down", "leftStickDown")
            .Replace("leftStick/left", "leftStickLeft")
            .Replace("leftStick/right", "leftStickRight")
            .Replace("rightStick/up", "rightStickUp")
            .Replace("rightStick/down", "rightStickDown")
            .Replace("rightStick/left", "rightStickLeft")
            .Replace("rightStick/right", "rightStickRight");
    }

    public string GetBindingDisplayName(string actionName)
    {
        // Get the action from the InputActionAsset
        var action = inputActionAsset.FindAction(actionName);
        if (action == null) return "";

        // Get the binding display name based on the active control scheme
        string controlScheme = action.controls.Count > 0 ? action.controls[0].device.displayName : "Unknown Device";

        // Check for keyboard or controller and return the appropriate binding
        int bindingIndex = -1;
        if (controlScheme.Contains("Keyboard"))
        {
            bindingIndex = action.bindings.ToList().FindIndex(b => b.path.Contains("<Keyboard>"));
        }
        else if (controlScheme.Contains("Gamepad"))
        {
            bindingIndex = action.bindings.ToList().FindIndex(b => b.path.Contains("<Gamepad>"));
        }

        if (bindingIndex >= 0)
        {
            return action.GetBindingDisplayString(bindingIndex);
        }

        return "";
    }

    public bool IsUsingController()
    {
        // Return true if the last device used was a Gamepad
        return lastDeviceUsed == "Gamepad";
    }

    private void DetectLastInputDevice()
    {
        string previousDevice = lastDeviceUsed; // Store the previous device used

        // Detect if a controller button is pressed
        if (Gamepad.current != null && Gamepad.current.allControls.OfType<ButtonControl>().Any(control => control.wasPressedThisFrame))
        {
            lastDeviceUsed = "Gamepad";
        }
        // Detect if a keyboard key is pressed, mouse movement, mouse buttons, or mouse scroll
        else if ((Keyboard.current != null && Keyboard.current.allControls.OfType<ButtonControl>().Any(control => control.wasPressedThisFrame)) ||
                 (Mouse.current != null && (
                     Mouse.current.position.ReadValue() != lastMousePosition ||  // Detect mouse movement
                     Mouse.current.leftButton.wasPressedThisFrame ||         // Detect left mouse button press
                     Mouse.current.rightButton.wasPressedThisFrame ||        // Detect right mouse button press
                     Mouse.current.middleButton.wasPressedThisFrame ||       // Detect middle mouse button press
                     Mouse.current.forwardButton.wasPressedThisFrame ||      // Detect button 5 press
                     Mouse.current.backButton.wasPressedThisFrame ||         // Detect button 4 press
                     Mouse.current.scroll.ReadValue().y != 0)))              // Detect mouse scroll movement (Y-axis)
        {
            lastMousePosition = Mouse.current.position.ReadValue(); // Update last mouse position
            lastDeviceUsed = "Keyboard & Mouse";
        }

        // If the device changed, trigger event
        if (previousDevice != lastDeviceUsed)
        {
            OnInputDeviceChanged?.Invoke();
        }
    }
}