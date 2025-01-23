using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Linq;

public class KeyBindingManager : MonoBehaviour
{
    public static KeyBindingManager Instance;

    [SerializeField] private KeyBindingData keyBindingData;
    public InputActionAsset inputActionAsset;

    private string lastDeviceUsed = "Keyboard"; // Default to keyboard

    public static event System.Action OnInputDeviceChanged;

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
        else
        {
            Debug.Log("Keyboard is in use.");
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
        // Detect if a keyboard key is pressed
        else if (Keyboard.current != null && Keyboard.current.allControls.OfType<ButtonControl>().Any(control => control.wasPressedThisFrame))
        {
            lastDeviceUsed = "Keyboard";
        }

        // If the device changed, trigger event
        if (previousDevice != lastDeviceUsed)
        {
            OnInputDeviceChanged?.Invoke();
        }
    }
}

