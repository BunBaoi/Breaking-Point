using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class KeybindingMetadata
{
    public InputAction action;
    public bool isCleared;
    public List<string> bindings; // Track individual bindings in a list

    public KeybindingMetadata(InputAction action)
    {
        this.action = action;
        isCleared = false;
        bindings = new List<string>();
    }
}

public class KeybindSettings: MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform keybindListParent;
    [SerializeField] private GameObject keybindPrefab;
    [SerializeField] private ScrollRect scrollRect; // Reference to the ScrollView
    [SerializeField] private RectTransform contentRect; // Content inside ScrollView
    [SerializeField] private TMP_Text warningText;
    private const string KEYBINDS_SAVE_KEY = "PlayerKeybinds";

    public static event Action<string, string> OnKeyBindingsChanged;
    private Dictionary<string, InputAction> actionDictionary = new Dictionary<string, InputAction>();
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private List<Button> instantiatedButtons = new List<Button>();
    private Coroutine notificationMessageCoroutine;

    private Dictionary<string, string> defaultBindings = new Dictionary<string, string>();
    private List<(string actionName, int bindingIndex)> bindingHistory = new List<(string, int)>();

    public static KeybindSettings Instance;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (var actionMap in inputActions.actionMaps)
        {
            foreach (var action in actionMap.actions)
            {
                actionDictionary[action.name] = action;
            }
        }

        LoadKeybinds(); // Load saved keybinds when game starts
    }

    void Start()
    {
        PopulateKeybindList();
        // InitialiseDefaultBindings();

    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Instantiated Buttons List:");
            foreach (Button button in instantiatedButtons)
            {
                if (button != null)
                    Debug.Log($"Button: {button.name}");
                else
                    Debug.Log("Null Button Found");
            }
        }*/
    }

    private void InitialiseDefaultBindings()
    {
        Debug.LogError("default bindings");
        bindingHistory.Clear();
        foreach (var entry in actionDictionary)
        {
            InputAction action = entry.Value;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                string defaultKey = InputControlPath.ToHumanReadableString(
                    action.bindings[i].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice
                );

                string actionName = entry.Key;

                // Store default bindings
                defaultBindings[actionName] = defaultKey;

                // Track defaults in history
                bindingHistory.Add((actionName, i));
            }
        }
    }

    void PopulateKeybindList()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActions not assigned in KeybindManager!");
            return;
        }

        // Clear existing entries
        foreach (Transform child in keybindListParent)
        {
            Destroy(child.gameObject);
        }

        instantiatedButtons.Clear();
        actionDictionary.Clear();

        foreach (InputActionMap actionMap in inputActions.actionMaps)
        {
            foreach (InputAction action in actionMap.actions)
            {
                if (action.bindings.Count > 0)
                {
                    actionDictionary[action.name] = action;
                    CreateKeybindEntry(action);
                }
            }
        }

        // Update content height after adding entries
        UpdateContentSize();
    }

    private void CreateKeybindEntry(InputAction action)
    {
        foreach (var binding in action.bindings)
        {
            if (binding.path.Contains("Mouse") &&
                (binding.path.Contains("delta") || binding.path.Contains("scroll/y") || binding.path.Contains("scroll/x")))
            {
                Debug.Log($"Skipping action '{action.name}' due to Mouse movement or scrolling binding.");
                return;
            }

            if (binding.path.Contains("anyKey"))
            {
                Debug.Log($"Skipping action '{action.name}' due to Any Key Press binding.");
                return;
            }
        }

        if (action.actionMap.name == "QTE")
        {
            Debug.Log($"Skipping action '{action.name}' because it's in the 'QTE' action map.");
            return;
        }

        if (action.actionMap.name == "Main Menu")
        {
            Debug.Log($"Skipping action '{action.name}' because it's in the 'Main Menu' action map.");
            return;
        }

        if (action.bindings.Count > 1 && action.bindings[0].isComposite)
        {
            string[] directions = { "Move Forward", "Move Backward", "Move Left", "Move Right" };

            for (int i = 0; i < 4; i++)
            {
                GameObject compositeEntry = Instantiate(keybindPrefab, keybindListParent);
                TMP_Text compositeActionLabel = compositeEntry.transform.Find("ActionLabel")?.GetComponent<TMP_Text>();
                Button compositeKeybindButtonKeyboard = compositeEntry.transform.Find("KeybindButtonKeyboard")?.GetComponent<Button>();
                TMP_Text compositeKeybindTextKeyboard = compositeKeybindButtonKeyboard?.GetComponentInChildren<TMP_Text>();
                Button compositeKeybindButtonController = compositeEntry.transform.Find("KeybindButtonController")?.GetComponent<Button>();
                TMP_Text compositeKeybindTextController = compositeKeybindButtonController?.GetComponentInChildren<TMP_Text>();

                if (compositeActionLabel == null || compositeKeybindButtonKeyboard == null || compositeKeybindTextKeyboard == null || compositeKeybindButtonController == null || compositeKeybindTextController == null)
                {
                    Debug.LogError($"Missing components in KeybindPrefab! Check structure.");
                    return;
                }

                compositeActionLabel.text = directions[i];

                int keyboardIndex = i + 1;
                if (keyboardIndex < action.bindings.Count && action.bindings[keyboardIndex].path.Contains("Keyboard"))
                {
                    compositeKeybindTextKeyboard.text = action.bindings[keyboardIndex].ToDisplayString()
                        .Replace("Hold", "")
                        .Replace("Press", "")
                    .Replace("LMB", "LMB")
                    .Replace("RMB", "RMB")
                    .Replace("Forward", "MB5")
                    .Replace("Back", "MB4")
                    .Replace("MMB", "MMB")
                    .Replace("Scroll/Y", "Mouse Scroll")
                    .Replace("LB", "Left Bumper")
                    .Replace("RB", "Right Bumper")
                    .Replace("LT", "Left Trigger")
                    .Replace("RT", "Right Trigger")
                    .Replace("LS/Up", "Left Stick Up")
                    .Replace("LS/Down", "Left Stick Down")
                    .Replace("LS/Left", "Left Stick Left")
                    .Replace("LS/Right", "Left Stick Right")
                    .Replace("RS/Up", "Right Stick Up")
                    .Replace("RS/Down", "Right Stick Down")
                    .Replace("RS/Left", "Right Stick Left")
                    .Replace("RS/Right", "Right Stick Right")
                   .Replace("D-Pad/Up", "D-Pad Up")
                    .Replace("D-Pad/Down", "D-Pad Down")
                    .Replace("D-Pad/Left", "D-Pad Left")
                    .Replace("D-Pad/Right", "D-Pad Right")
                    .Replace("Menu", "Start");
                    compositeKeybindButtonKeyboard.onClick.AddListener(() => StartRebinding(action, compositeKeybindTextKeyboard, false, keyboardIndex));

                    // Add the button with the action name and "_Keyboard"
                    instantiatedButtons.Add(compositeKeybindButtonKeyboard);
                    Debug.Log($"Added button: {compositeKeybindButtonKeyboard.name}");
                    compositeKeybindButtonKeyboard.name = $"{action.name}_{keyboardIndex}_Keyboard"; // Add unique name
                }

                int controllerIndex = i + 6;
                if (controllerIndex < action.bindings.Count && action.bindings[controllerIndex].path.Contains("Controller") || action.bindings[controllerIndex].path.Contains("Gamepad"))
                {
                    compositeKeybindTextController.text = action.bindings[controllerIndex].ToDisplayString()
                        .Replace("Hold", "")
                    .Replace("LMB", "LMB")
                    .Replace("RMB", "RMB")
                    .Replace("Forward", "MB5")
                    .Replace("Back", "MB4")
                    .Replace("MMB", "MMB")
                    .Replace("Scroll/Y", "Mouse Scroll")
                    .Replace("LB", "Left Bumper")
                    .Replace("RB", "Right Bumper")
                    .Replace("LT", "Left Trigger")
                    .Replace("RT", "Right Trigger")
                    .Replace("LS/Up", "Left Stick Up")
                    .Replace("LS/Down", "Left Stick Down")
                    .Replace("LS/Left", "Left Stick Left")
                    .Replace("LS/Right", "Left Stick Right")
                    .Replace("RS/Up", "Right Stick Up")
                    .Replace("RS/Down", "Right Stick Down")
                    .Replace("RS/Left", "Right Stick Left")
                    .Replace("RS/Right", "Right Stick Right")
                   .Replace("D-Pad/Up", "D-Pad Up")
                    .Replace("D-Pad/Down", "D-Pad Down")
                    .Replace("D-Pad/Left", "D-Pad Left")
                    .Replace("D-Pad/Right", "D-Pad Right")
                    .Replace("Menu", "Start");
                    compositeKeybindButtonController.onClick.AddListener(() => StartRebinding(action, compositeKeybindTextController, true, controllerIndex));

                    string bindingText = action.bindings[1].ToDisplayString();

                    // Count occurrences of "Press"
                    int compositePressCount = bindingText.Split(new[] { "Press" }, StringSplitOptions.None).Length - 1;
                    if (compositePressCount > 0 && bindingText.StartsWith("Press"))
                    {
                        bindingText = bindingText.Replace("Press", "").TrimStart()
                            .Replace("Hold", "")
                        .Replace("LMB", "LMB")
                    .Replace("RMB", "RMB")
                    .Replace("Forward", "MB5")
                    .Replace("Back", "MB4")
                    .Replace("MMB", "MMB")
                    .Replace("Scroll/Y", "Mouse Scroll")
                        .Replace("LB", "Left Bumper")
                        .Replace("RB", "Right Bumper")
                        .Replace("LT", "Left Trigger")
                        .Replace("RT", "Right Trigger")
                        .Replace("LS/Up", "Left Stick Up")
                        .Replace("LS/Down", "Left Stick Down")
                        .Replace("LS/Left", "Left Stick Left")
                        .Replace("LS/Right", "Left Stick Right")
                        .Replace("RS/Up", "Right Stick Up")
                        .Replace("RS/Down", "Right Stick Down")
                        .Replace("RS/Left", "Right Stick Left")
                        .Replace("RS/Right", "Right Stick Right")
                       .Replace("D-Pad/Up", "D-Pad Up")
                        .Replace("D-Pad/Down", "D-Pad Down")
                        .Replace("D-Pad/Left", "D-Pad Left")
                        .Replace("D-Pad/Right", "D-Pad Right")
                        .Replace("Menu", "Start");
                        compositeKeybindTextController.text = bindingText;
                        Debug.Log("trimmed press");
                    }

                    // Add the button with the action name and "_Controller"
                    instantiatedButtons.Add(compositeKeybindButtonController);
                    Debug.Log($"Added button: {compositeKeybindButtonController.name}");
                    compositeKeybindButtonController.name = $"{action.name}_{controllerIndex}_Controller"; // Add unique name
                }
            }
        }
        else
        {
            GameObject entry = Instantiate(keybindPrefab, keybindListParent);
            TMP_Text actionLabel = entry.transform.Find("ActionLabel")?.GetComponent<TMP_Text>();
            Button keybindButtonKeyboard = entry.transform.Find("KeybindButtonKeyboard")?.GetComponent<Button>();
            TMP_Text keybindTextKeyboard = keybindButtonKeyboard?.GetComponentInChildren<TMP_Text>();
            Button keybindButtonController = entry.transform.Find("KeybindButtonController")?.GetComponent<Button>();
            TMP_Text keybindTextController = keybindButtonController?.GetComponentInChildren<TMP_Text>();

            if (actionLabel == null || keybindButtonKeyboard == null || keybindTextKeyboard == null || keybindButtonController == null || keybindTextController == null)
            {
                Debug.LogError($"Missing components in KeybindPrefab! Check structure.");
                return;
            }

            actionLabel.text = action.name;

            if (action.bindings.Count > 0)
            {
                keybindTextKeyboard.text = action.bindings[0].ToDisplayString()
                    .Replace("Hold", "")
                    .Replace("Press", "")
                    .Replace("LMB", "LMB")
                    .Replace("RMB", "RMB")
                    .Replace("Forward", "MB5")
                    .Replace("Back", "MB4")
                    .Replace("MMB", "MMB")
                    .Replace("Scroll/Y", "Mouse Scroll")
                    .Replace("LB", "Left Bumper")
                    .Replace("RB", "Right Bumper")
                    .Replace("LT", "Left Trigger")
                    .Replace("RT", "Right Trigger")
                    .Replace("LS/Up", "Left Stick Up")
                    .Replace("LS/Down", "Left Stick Down")
                    .Replace("LS/Left", "Left Stick Left")
                    .Replace("LS/Right", "Left Stick Right")
                    .Replace("RS/Up", "Right Stick Up")
                    .Replace("RS/Down", "Right Stick Down")
                    .Replace("RS/Left", "Right Stick Left")
                    .Replace("RS/Right", "Right Stick Right")
                   .Replace("D-Pad/Up", "D-Pad Up")
                    .Replace("D-Pad/Down", "D-Pad Down")
                    .Replace("D-Pad/Left", "D-Pad Left")
                    .Replace("D-Pad/Right", "D-Pad Right")
                    .Replace("Menu", "Start");

                keybindButtonKeyboard.onClick.AddListener(() => StartRebinding(action, keybindTextKeyboard, false, 0));

                // Add the button with the action name and "_Keyboard"
                instantiatedButtons.Add(keybindButtonKeyboard);
                Debug.Log($"Added button: {keybindButtonKeyboard.name}");
                keybindButtonKeyboard.name = $"{action.name}_Keyboard"; // Add unique name
            }

            if (action.bindings.Count > 1)
            {
                keybindTextController.text = action.bindings[1].ToDisplayString()

                    .Replace("Hold", "")
                    .Replace("LMB", "LMB")
                    .Replace("RMB", "RMB")
                    .Replace("Forward", "MB5")
                    .Replace("Back", "MB4")
                    .Replace("MMB", "MMB")
                    .Replace("Scroll/Y", "Mouse Scroll")
                    .Replace("LB", "Left Bumper")
                    .Replace("RB", "Right Bumper")
                    .Replace("LT", "Left Trigger")
                    .Replace("RT", "Right Trigger")
                    .Replace("LS/Up", "Left Stick Up")
                    .Replace("LS/Down", "Left Stick Down")
                    .Replace("LS/Left", "Left Stick Left")
                    .Replace("LS/Right", "Left Stick Right")
                    .Replace("RS/Up", "Right Stick Up")
                    .Replace("RS/Down", "Right Stick Down")
                    .Replace("RS/Left", "Right Stick Left")
                    .Replace("RS/Right", "Right Stick Right")
                   .Replace("D-Pad/Up", "D-Pad Up")
                    .Replace("D-Pad/Down", "D-Pad Down")
                    .Replace("D-Pad/Left", "D-Pad Left")
                    .Replace("D-Pad/Right", "D-Pad Right")
                    .Replace("Menu", "Start");
                keybindButtonController.onClick.AddListener(() => StartRebinding(action, keybindTextController, true, 1));

                string bindingText = action.bindings[1].ToDisplayString();

                // Count occurrences of "Press"
                int pressCount = bindingText.Split(new[] { "Press" }, StringSplitOptions.None).Length - 1;
                if (pressCount > 0 && bindingText.StartsWith("Press"))
                {
                    bindingText = bindingText.Replace("Press", "").TrimStart()
                        .Replace("Hold", "")
                    .Replace("LMB", "LMB")
                    .Replace("RMB", "RMB")
                    .Replace("Forward", "MB5")
                    .Replace("Back", "MB4")
                    .Replace("MMB", "MMB")
                    .Replace("Scroll/Y", "Mouse Scroll")
                    .Replace("LB", "Left Bumper")
                    .Replace("RB", "Right Bumper")
                    .Replace("LT", "Left Trigger")
                    .Replace("RT", "Right Trigger")
                    .Replace("LS/Up", "Left Stick Up")
                    .Replace("LS/Down", "Left Stick Down")
                    .Replace("LS/Left", "Left Stick Left")
                    .Replace("LS/Right", "Left Stick Right")
                    .Replace("RS/Up", "Right Stick Up")
                    .Replace("RS/Down", "Right Stick Down")
                    .Replace("RS/Left", "Right Stick Left")
                    .Replace("RS/Right", "Right Stick Right")
                   .Replace("D-Pad/Up", "D-Pad Up")
                    .Replace("D-Pad/Down", "D-Pad Down")
                    .Replace("D-Pad/Left", "D-Pad Left")
                    .Replace("D-Pad/Right", "D-Pad Right")
                    .Replace("Menu", "Start");
                    keybindTextController.text = bindingText;
                    Debug.Log("trimmed press");
                }
                // Add the button with the action name and "_Controller"
                instantiatedButtons.Add(keybindButtonController);
                Debug.Log($"Added button: {keybindButtonKeyboard.name}");
                keybindButtonController.name = $"{action.name}_Controller"; // Add unique name
            }
        }
    }

    private TMP_Text previousKeybindText = null;
    private string originalKeybindText = "";

    private bool isRebindingInProgress = false;

    void StartRebinding(InputAction action, TMP_Text keybindText, bool isController, int bindingIndex)
    {
        warningText.text = "";
        Debug.Log("Before Rebinding:");
        foreach (var entry in actionDictionary)
        {
            Debug.Log($"Action Name: {entry.Key}, Bindings: {string.Join(", ", entry.Value.bindings.Select(b => b.ToDisplayString()))}");
        }
        if (rebindingOperation != null)
        {
            rebindingOperation.Cancel();
            rebindingOperation.Dispose();
        }

        originalKeybindText = keybindText.text;
        previousKeybindText = keybindText;

        action.Disable();
        keybindText.text = "...";
        int index = bindingIndex;

        string previousBinding = action.bindings[index].ToDisplayString(); // Store previous binding
        string bindingGroup = action.bindings[index].groups; // Determine control scheme

        Button currentButton = keybindText.GetComponentInParent<Button>();
        if (currentButton != null)
        {
            currentButton.interactable = false;
        }

        // Set rebinding flag to true to track the operation status
        isRebindingInProgress = true;

        rebindingOperation = action.PerformInteractiveRebinding(index)
            // Exclude certain controls
            .WithControlsExcluding("Mouse/delta")
            .WithControlsExcluding("Mouse/scroll/y")
            .WithControlsExcluding("Mouse/scroll/x") 
            .WithControlsExcluding("Mouse/scroll/up")
            .WithControlsExcluding("Mouse/scroll/down")
            .WithControlsExcluding("Mouse/scroll/right")
            .WithControlsExcluding("Mouse/scroll/left")
            .WithControlsExcluding("Gamepad/leftStick/x")
    .WithControlsExcluding("Gamepad/leftStick/y")
    .WithControlsExcluding("XInputController/leftStick/x")
    .WithControlsExcluding("XInputController/leftStick/y")
.WithControlsExcluding("<Gamepad>/leftStick/x")
    .WithControlsExcluding("<Gamepad>/leftStick/y")
    .WithControlsExcluding("<XInputController>/leftStick/x")
    .WithControlsExcluding("<XInputController>/leftStick/y")
    .WithControlsExcluding("Gamepad/rightStick/x")
    .WithControlsExcluding("Gamepad/rightStick/y")
    .WithControlsExcluding("XInputController/rightStick/x")
    .WithControlsExcluding("XInputController/rightStick/y")
.WithControlsExcluding("<Gamepad>/rightStick/x")
    .WithControlsExcluding("<Gamepad>/rightStick/y")
    .WithControlsExcluding("<XInputController>/rightStick/x")
    .WithControlsExcluding("<XInputController>/rightStick/y")
            .OnMatchWaitForAnother(0.2f) // Wait for 200ms for a valid input
            .OnComplete(operation =>
            {
                if (!isRebindingInProgress) return; // Ignore completion if rebinding was interrupted

            string newKey = operation.selectedControl.displayName
                    .Replace("Press ", "")
                    .Replace("Hold", "")
                    .Trim();

                Debug.Log($"Keybinding changed to: {newKey}");

                bool isConflict = false;

            // Check for conflicts within the same action (including composites)
            for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (i == index) continue; // Skip checking itself

                if (action.bindings[i].groups == bindingGroup) // Only check within the same control scheme
                {
                        string existingKey = action.bindings[i].ToDisplayString()
                            .Replace("Press ", "")
                            .Replace("Hold", "")
                            .Trim();

                        if (newKey == existingKey)
                        {
                            Debug.LogWarning($"Conflict detected in the same action: {action.name} (Binding {index} conflicts with {i})");
                            isConflict = true;
                            break;
                        }
                    }
                }

            // Check for conflicts with other actions within the same control scheme
            foreach (var otherAction in actionDictionary.Values)
                {
                    if (otherAction != action)
                    {
                        foreach (var binding in otherAction.bindings)
                        {
                            if (binding.groups == bindingGroup) // Only check within the same control scheme
                        {
                                string otherKey = binding.ToDisplayString()
                                    .Replace("Press ", "")
                                    .Replace("Hold", "")
                                    .Trim();

                                if (newKey != "" && newKey == otherKey)
                                {
                                    isConflict = true;
                                    break;
                                }
                                else if (newKey == "" && binding.path == action.bindings[index].path)
                                {
                                    isConflict = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (isConflict) break;
                }

            // Prevent Left Click from overriding improperly
            if (previousBinding != newKey && newKey == "Left Click" && isRebindingInProgress)
                {
                    Debug.LogWarning("Left Click binding is only allowed if the rebinding was not interrupted.");
                    keybindText.text = originalKeybindText;
                    action.ApplyBindingOverride(index, previousBinding);
                }
                else
                {
                    keybindText.text = newKey != "" ? newKey : action.bindings[index].ToDisplayString();
                }

                if (isConflict)
                {
                    ShowWarningText("Note: This key is the same as another action. Recommended to have a max of 2 per key!");
                }

                // Capture the raw input path directly during the rebinding process
                string newPath = operation.selectedControl.path;

                Debug.Log("Selected Control Path: " + newPath);

                string deviceType = "Unknown";

                // Determine the device type by the newPath above
                if (newPath.Contains("Keyboard") || newPath.Contains("Mouse"))
                {
                    deviceType = "Keyboard";
                }
                else if (newPath.Contains("Gamepad") || newPath.Contains("XInput"))
                {
                    deviceType = "Controller";
                }
                // SET CONTROLLER BUTTON NAMES
                if (newPath.Contains("buttonNorth"))
                {
                    newKey = "Button North";  // Button Y
                }
                else if (newPath.Contains("buttonSouth"))
                {
                    newKey = "Button South";  // Button A
                }
                else if (newPath.Contains("buttonEast"))
                {
                    newKey = "Button East";  // Button B
                }
                else if (newPath.Contains("buttonWest"))
                {
                    newKey = "Button West";  // Button X
                }
                else if (newPath.Contains("leftShoulder"))
                {
                    newKey = "Left Shoulder";  // LB
                }
                else if (newPath.Contains("rightShoulder"))
                {
                    newKey = "Right Shoulder";  // RB
                }
                else if (newPath.Contains("leftTrigger"))
                {
                    newKey = "Left Trigger";  // LT
                }
                else if (newPath.Contains("rightTrigger"))
                {
                    newKey = "Right Trigger";  // RT
                }
                else if (newPath.Contains("leftStickPress"))
                {
                    newKey = "Left Stick Press";  // LS (press down on the left stick)
                }
                else if (newPath.Contains("rightStickPress"))
                {
                    newKey = "Right Stick Press";  // RS (press down on the right stick)
                }
                else if (newPath.Contains("select"))
                {
                    newKey = "Select";  // Select
                }
                else if (newPath.Contains("start"))
                {
                    newKey = "Start";  // Start (or Menu on newer controllers)
                }
                else if (newPath.Contains("dpad/up"))
                {
                    newKey = "D-Pad/Up";  // D-Pad Up
                }
                else if (newPath.Contains("dpad/down"))
                {
                    newKey = "D-Pad/Down";  // D-Pad Down
                }
                else if (newPath.Contains("dpad/left"))
                {
                    newKey = "D-Pad/Left";  // D-Pad Left
                }
                else if (newPath.Contains("dpad/right"))
                {
                    newKey = "D-Pad/Right";  // D-Pad Right
                }
                else if (newPath.Contains("leftStick/up"))
                {
                    newKey = "Left Stick/Up";  // Left Thumbstick Up
                }
                else if (newPath.Contains("leftStick/down"))
                {
                    newKey = "Left Stick/Down";  // Left Thumbstick Down
                }
                else if (newPath.Contains("leftStick/left"))
                {
                    newKey = "Left Stick/Left";  // Left Thumbstick Left
                }
                else if (newPath.Contains("leftStick/right"))
                {
                    newKey = "Left Stick/Right";  // Left Thumbstick Right
                }
                else if (newPath.Contains("rightStick/up"))
                {
                    newKey = "Right Stick/Up";  // Right Thumbstick Up
                }
                else if (newPath.Contains("rightStick/down"))
                {
                    newKey = "Right Stick/Down";  // Right Thumbstick Down
                }
                else if (newPath.Contains("rightStick/left"))
                {
                    newKey = "Right Stick/Left";  // Right Thumbstick Left
                }
                else if (newPath.Contains("rightStick/right"))
                {
                    newKey = "Right Stick/Right";  // Right Thumbstick Right
                }

                // Add new binding to history
                bindingHistory.Add((action.name, bindingIndex));

                // Unbind the latest instance based on the device type
                UnbindLatestInstance(newKey, action.name, index, deviceType);

                // Override keybind text to change specific keys to other text
                if (newKey == "Back")
                {
                    keybindText.text = "MB4"; 
                }
                if (newKey == "Forward")
                {
                    keybindText.text = "MB5";
                }
                if (newKey == "Left Button")
                {
                    keybindText.text = "LMB";
                }
                if (newKey == "Right Button")
                {
                    keybindText.text = "RMB";
                }
                if (newKey == "Middle Button")
                {
                    keybindText.text = "MMB";
                }
                if (newKey == "Scroll/Y")
                {
                    keybindText.text = "Mouse Scroll";
                }

                OnKeyBindingsChanged?.Invoke(action.name, newKey);
                SaveKeybinds();

                Debug.Log("After Rebinding:");
                foreach (var entry in actionDictionary)
                {
                    Debug.Log($"Action Name: {entry.Key}, Bindings: {string.Join(", ", entry.Value.bindings.Select(b => b.ToDisplayString()))}");
                }

                // Reset rebinding flag once done
                isRebindingInProgress = false;
                operation.Dispose();
                action.Enable();

                if (currentButton != null)
                {
                    currentButton.interactable = true; // Re-enable the button after rebinding
                }
            })
            .OnCancel(operation =>
            {
                if (previousKeybindText != null)
                {
                    previousKeybindText.text = originalKeybindText;
                }

                Debug.Log("After Rebinding:");
                foreach (var entry in actionDictionary)
                {
                    Debug.Log($"Action Name: {entry.Key}, Bindings: {string.Join(", ", entry.Value.bindings.Select(b => b.ToDisplayString()))}");
                }

                // Reset rebinding flag if canceled
                isRebindingInProgress = false;
                operation.Dispose();
                action.Enable();
                if (currentButton != null)
                {
                    currentButton.interactable = true; // Re-enable the button after rebinding
                }
            })
            .Start();
    }

    private void UnbindLatestInstance(string newKey, string currentActionName, int bindingIndex, string deviceType)
    {
        warningText.text = "";
        if (newKey == "<None>")
        {
            Debug.Log("Skipping unbinding since the new key is <None>");
            return;
        }

        InputAction action = actionDictionary[currentActionName];

        int keyBindingCount = 0;

        // Check all bindings in the current action to see if the new key is already bound
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];

            // Check if the binding's device matches the deviceType
            bool matchesDeviceType = false;

            if (deviceType == "Keyboard")
            {
                matchesDeviceType = true;
            }
            else if (deviceType == "Controller")
            {
                matchesDeviceType = true;
            }
                string boundKey = InputControlPath.ToHumanReadableString(
                    binding.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice
                );

                if (boundKey == newKey && matchesDeviceType)
                {
                    keyBindingCount++;

                    // Skip the bindingIndex we are currently modifying
                    if (i == bindingIndex)
                        continue;

                    // If it's part of a composite, handle unbinding logic
                    if (binding.isPartOfComposite)
                    {
                        ShowWarningText($"[{newKey}] is already bound in [{currentActionName}]! Unbinding [{newKey}] from [{i}] in {currentActionName}.");
                        action.ApplyBindingOverride(i, "<None>");
                        UpdateKeybindButtonText(currentActionName, i, "<Unbound>", deviceType); // Update UI text
                        Debug.Log($"Unbinding {currentActionName} at index {i} because [{newKey}] is already in use in this composite.");

                        // Remove from history and insert "<None>"
                        for (int j = 0; j < bindingHistory.Count; j++)
                        {
                            if (bindingHistory[j].actionName == currentActionName && bindingHistory[j].bindingIndex == i)
                            {
                                bindingHistory.RemoveAt(j);
                                bindingHistory.Insert(j, (currentActionName, i));
                                break;
                            }
                        }
                        return;
                    }
                }
            }

        // Debug log for keyBindingCount in the current action
        Debug.Log($"Keybinding count for {newKey} in current action {currentActionName}: {keyBindingCount}");

        // Check how many times the newKey is bound across all actions (not just current action)
        int globalBindingCount = 0;

        foreach (var actionPair in actionDictionary)
        {
            InputAction otherAction = actionPair.Value;

            // Debugging: Log the current action
            Debug.Log($"Checking action: {otherAction.name}");

            for (int i = 0; i < otherAction.bindings.Count; i++)
            {
                var binding = otherAction.bindings[i];

                // Debugging: Log each binding's path
                Debug.Log($"Binding {i}: {binding.path}");

                // Check if the binding's device matches the deviceType
                bool matchesDeviceType = false;

                // Debugging: Log the device type and whether it matches
                if (deviceType == "Keyboard")
                {
                    matchesDeviceType = true;
                    Debug.Log("Device matches Keyboard");
                }
                else if (deviceType == "Controller")
                {
                    matchesDeviceType = true;
                    Debug.Log("Device matches Controller");
                }
                else
                {
                    Debug.Log($"No match for device type: {deviceType} on binding {i}");
                }

                string boundKey = InputControlPath.ToHumanReadableString(
                    binding.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice
                );

                // Debugging: Log the bound key
                Debug.Log($"Bound Key: {boundKey}, New Key: {newKey}");

                // If the binding matches the new key, increment the count
                if (boundKey == newKey && matchesDeviceType)
                {
                    globalBindingCount++;
                    Debug.Log($"Match found. Global Binding Count: {globalBindingCount}");
                }
                else
                {
                    Debug.Log("No match or device mismatch");
                }
            }
        }

        // Debug log for globalBindingCount across all actions
        Debug.Log($"Global binding count for {newKey}: {globalBindingCount}");


        // If the key is bound to more than one action (not composite), unbind the conflicting bindings, except the current one
        if (globalBindingCount > 2)
        {
            warningText.text = "";
            Debug.Log($"[{newKey}] is bound to more than 2 actions globally. Unbinding conflicting instance...");

            for (int i = 0; i < bindingHistory.Count; i++)
            {
                var (actionName, bindingIndexInHistory) = bindingHistory[i];

                if (actionDictionary.ContainsKey(actionName))
                {
                    InputAction otherAction = actionDictionary[actionName];
                    var binding = otherAction.bindings[bindingIndexInHistory];

                    string boundKey = InputControlPath.ToHumanReadableString(
                        binding.effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice
                    );

                    // Check if the binding groups match the device type
                    if (!binding.groups.Contains(deviceType))
                    {
                        Debug.Log($"Skipping {boundKey} in {actionName} because its binding group does not match {deviceType}.");
                        continue;
                    }

                    if (binding.isPartOfComposite) continue;

                    // Ensure we don't unbind the current keybinding
                    if (boundKey == newKey && actionName != currentActionName)
                    {
                        Debug.Log($"Checking binding: {boundKey} with device {deviceType}, action: {actionName}, binding groups: {otherAction.bindings[bindingIndexInHistory].groups}");
                        // Unbind the conflicting key
                        ShowWarningText($"Only allowed up to 2 of the same key to be bound to different actions! Unbinding [{newKey}] from {actionName}.");
                        otherAction.ApplyBindingOverride(bindingIndexInHistory, "<None>");
                        UpdateKeybindButtonText(actionName, null, "<Unbound>", deviceType); // Update specific button text
                        Debug.Log($"Unbinding {actionName} at index {bindingIndexInHistory} to make room for {newKey}.");

                        // Remove from history and update it
                        bindingHistory.RemoveAt(i);
                        bindingHistory.Insert(i, (actionName, bindingIndexInHistory));

                        return;
                    }
                }
            }
        }
    }

    private void UpdateKeybindButtonText(string actionName, int? bindingIndex, string newText, string deviceType)
    {
        Debug.Log($"Updating button text for action: {actionName}, bindingIndex: {bindingIndex?.ToString() ?? "None"}, newText: {newText}");

        foreach (Button button in instantiatedButtons)
        {
            Debug.Log($"Checking button: {button.name}");

            // If bindingIndex is provided (for composite bindings), check the button name for both actionName and bindingIndex
            if (bindingIndex.HasValue)
            {
                Debug.Log($"Checking for composite binding: {actionName}_{bindingIndex.Value}_");

                if (button.name.StartsWith(actionName) && button.name.Contains($"_{bindingIndex.Value}_{deviceType}"))
                {
                    Debug.Log($"Found matching button: {button.name}");

                    TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = newText;
                        Debug.Log($"Updated text for {button.name} to: {newText}");
                    }
                    break; // Stop after updating the first match
                }
                else
                {
                    Debug.Log($"No match for composite binding on button: {button.name}");
                }
            }
            // If bindingIndex is not provided (for non-composite bindings), just check for the actionName
            else
            {
                if (button.name.StartsWith(actionName) && button.name.Contains($"_{deviceType}"))
                {
                    Debug.Log($"Found matching button for non-composite: {button.name}");

                    TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = newText;
                        Debug.Log($"Updated text for {button.name} to: {newText}");
                    }
                    break; // Stop after updating the first match
                }
                else
                {
                    Debug.Log($"No match for non-composite binding on button: {button.name}");
                }
            }
        }
    }

    public void SaveKeybinds()
    {
        var bindings = new Dictionary<string, string>();

        foreach (var action in actionDictionary.Values)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite) // Avoid composite bindings
                {
                    string assignedPath = string.IsNullOrEmpty(action.bindings[i].overridePath)
                        ? action.bindings[i].path  // If no override, keep the default path
                        : action.bindings[i].overridePath;

                    bindings[action.name + i] = assignedPath;

                    Debug.Log($"Saving: {action.name} [{i}] = {assignedPath}");
                }
            }
        }

        string json = JsonUtility.ToJson(new Serialization<string, string>(bindings));
        PlayerPrefs.SetString(KEYBINDS_SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("Keybinds saved successfully.");
    }

    private void LoadKeybinds()
    {
        if (PlayerPrefs.HasKey(KEYBINDS_SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(KEYBINDS_SAVE_KEY);
            var bindings = JsonUtility.FromJson<Serialization<string, string>>(json).ToDictionary();

            foreach (var action in actionDictionary.Values)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (!action.bindings[i].isComposite)
                    {
                        if (bindings.ContainsKey(action.name + i))
                        {
                            string savedPath = bindings[action.name + i];
                            action.ApplyBindingOverride(i, savedPath);
                            Debug.Log($"Loaded: {action.name} [{i}] = {savedPath}");
                        }
                        else
                        {
                            Debug.Log($"No saved keybind for {action.name} [{i}]. Keeping default: {action.bindings[i].path}");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log("No saved keybinds found. Using default bindings.");
        }
    }

    void ShowWarningText(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);

        // If there's an existing coroutine running, stop it
        if (notificationMessageCoroutine != null)
        {
            StopCoroutine(notificationMessageCoroutine);
        }

        // Show the reset message
        warningText.gameObject.SetActive(true);

        // Start a new coroutine
        notificationMessageCoroutine = StartCoroutine(HideWarningText());
    }

    IEnumerator HideWarningText()
    {
        yield return new WaitForSecondsRealtime(3f);

        warningText.text = "";
        // Hide the warning text after the duration
        warningText.gameObject.SetActive(false);
    }

    public void ResetToDefaults()
    {
        foreach (var action in actionDictionary.Values)
        {
            action.RemoveAllBindingOverrides();
        }
        PopulateKeybindList();
        InitialiseDefaultBindings();
    }

    void UpdateContentSize()
    {
        if (contentRect == null)
        {
            Debug.LogError("Content RectTransform is not assigned!");
            return;
        }

        float entryHeight = keybindPrefab.GetComponent<RectTransform>().rect.height;
        float spacing = 20f; // Adjust based on layout settings
        int entryCount = keybindListParent.childCount;

        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, (entryHeight + spacing) * entryCount);

        // Reset scroll position to top
        scrollRect.verticalNormalizedPosition = 1f;
    }
}

[System.Serializable]
public class Serialization<TKey, TValue>
{
    public List<TKey> keys;
    public List<TValue> values;

    public Serialization(Dictionary<TKey, TValue> dictionary)
    {
        keys = new List<TKey>(dictionary.Keys);
        values = new List<TValue>(dictionary.Values);
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        var dictionary = new Dictionary<TKey, TValue>();
        for (int i = 0; i < keys.Count; i++)
        {
            dictionary[keys[i]] = values[i];
        }
        return dictionary;
    }
}

