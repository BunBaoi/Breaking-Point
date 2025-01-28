using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class KeybindManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform keybindListParent;
    [SerializeField] private GameObject keybindPrefab;
    [SerializeField] private ScrollRect scrollRect; // Reference to the ScrollView
    [SerializeField] private RectTransform contentRect; // Content inside ScrollView
    [SerializeField] private TMP_Text warningText;

    private Dictionary<string, InputAction> actionDictionary = new Dictionary<string, InputAction>();
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    void Start()
    {
        PopulateKeybindList();
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

    void CreateKeybindEntry(InputAction action)
    {
        // Check if the action has a Mouse binding, and skip if true
        foreach (var binding in action.bindings)
        {
            if (binding.path.Contains("Mouse"))
            {
                // Skip creating keybind entries for actions with Mouse bindings
                Debug.Log($"Skipping action '{action.name}' due to Mouse binding.");
                return;
            }
        }

        // Check if it's a composite action (e.g., a 2D Vector with separate bindings for Up, Down, Left, Right)
        if (action.bindings.Count > 1 && action.bindings[0].isComposite)
        {
            // Only instantiate 4 entries for Up, Down, Left, Right
            string[] directions = { "Up", "Down", "Left", "Right" };

            // Iterate over the 4 directions
            for (int i = 0; i < 4; i++)
            {
                // Instantiate the prefab for each direction (Up, Down, Left, Right)
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

                // Set the label for the action (e.g., Up, Down, Left, Right)
                compositeActionLabel.text = directions[i];

                // Adjust index logic for the keyboard (1, 2, 3, 4)
                int keyboardIndex = i + 1; // Up = 1, Down = 2, Left = 3, Right = 4
                if (keyboardIndex < action.bindings.Count && action.bindings[keyboardIndex].path.Contains("Keyboard"))
                {
                    compositeKeybindTextKeyboard.text = action.bindings[keyboardIndex].ToDisplayString()
                        .Replace("Press ", "")  // Remove "Press"
                        .Replace("Hold", "");   // Remove "Hold"
                    compositeKeybindButtonKeyboard.onClick.AddListener(() => StartRebinding(action, compositeKeybindTextKeyboard, false, keyboardIndex));
                }

                // Adjust index logic for the controller (6, 7, 8, 9)
                int controllerIndex = i + 6; // Up = 6, Down = 7, Left = 8, Right = 9
                if (controllerIndex < action.bindings.Count && action.bindings[controllerIndex].path.Contains("Controller"))
                {
                    compositeKeybindTextController.text = action.bindings[controllerIndex].ToDisplayString()
                        .Replace("Press ", "")  // Remove "Press"
                        .Replace("Hold", "");   // Remove "Hold"
                    compositeKeybindButtonController.onClick.AddListener(() => StartRebinding(action, compositeKeybindTextController, true, controllerIndex));
                }
            }
        }
        else
        {
            // Handle non-composite actions if necessary, for example for simple actions like Jump, Shoot, etc.
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

            actionLabel.text = action.name; // For non-composite actions

            // Set the keyboard binding
            if (action.bindings.Count > 0)
            {
                keybindTextKeyboard.text = action.bindings[0].ToDisplayString()
                    .Replace("Press ", "")  // Remove "Press"
                    .Replace("Hold", "");   // Remove "Hold"
                keybindButtonKeyboard.onClick.AddListener(() => StartRebinding(action, keybindTextKeyboard, false, 0));
            }

            // Set the controller binding
            if (action.bindings.Count > 1)
            {
                keybindTextController.text = action.bindings[1].ToDisplayString()
                    .Replace("Press ", "")  // Remove "Press"
                    .Replace("Hold", "");   // Remove "Hold"
                keybindButtonController.onClick.AddListener(() => StartRebinding(action, keybindTextController, true, 1));
            }
        }
    }

    // A variable to store the original state of the keybinding text.
    private TMP_Text previousKeybindText = null;
    private string originalKeybindText = "";

    // Start Rebinding Function
    void StartRebinding(InputAction action, TMP_Text keybindText, bool isController, int bindingIndex)
    {
        // If there is an ongoing rebinding, cancel it first
        if (rebindingOperation != null)
        {
            rebindingOperation.Cancel();
        }

        // Store the current keybinding text as the original before starting the rebinding
        originalKeybindText = keybindText.text;
        previousKeybindText = keybindText;

        // Temporarily disable the action before rebinding
        action.Disable();

        // Change the keybinding text to show the rebinding process is in progress
        keybindText.text = "...";
        int index = bindingIndex;

        // Start the rebinding operation
        rebindingOperation = action.PerformInteractiveRebinding(index)
            .WithControlsExcluding("Mouse") // Exclude mouse bindings
            .OnComplete(operation =>
            {
            // Get the new pressed key as a string
            string newKey = action.bindings[index].ToDisplayString()
                    .Replace("Press ", "")  // Remove "Press"
                    .Replace("Hold", "");   // Remove "Hold"

            // Debug log the new keybinding
            Debug.Log($"Keybinding changed to: {newKey}");

            // Check for conflicts with other bindings
            bool isConflict = false;

                foreach (var otherAction in actionDictionary.Values)
                {
                    if (otherAction != action)
                    {
                        foreach (var binding in otherAction.bindings)
                        {
                            string otherKey = binding.ToDisplayString()
                                .Replace("Press ", "")
                                .Replace("Hold", "");

                        // Check if the new key is already bound
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

                    if (isConflict) break;
                }

            // Update the binding text to reflect the new keybinding or the default if empty
            keybindText.text = newKey != "" ? newKey : action.bindings[index].ToDisplayString();

            // Show a warning if there was a conflict with another binding
            if (isConflict)
                {
                    ShowWarningText("Note: This key is the same as another action, recommended to only have a max of 2 per key!");
                }

            // Dispose of the operation and re-enable the action after rebinding
            operation.Dispose();
                action.Enable();
            })
            .OnCancel(operation =>
            {
            // If the rebinding was canceled, reset the keybinding to its original state
            if (previousKeybindText != null)
                {
                    previousKeybindText.text = originalKeybindText; // Reset the text to the original value
            }

            // Reset the rebinding process
            operation.Dispose();
                action.Enable();
            })
            .Start();
    }

    void ShowWarningText(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);

        // Start a coroutine that doesn't depend on the time scale
        StartCoroutine(HideWarningText());
    }

    IEnumerator HideWarningText()
    {
        // Use unscaled time so it runs even when the game is paused
        float elapsedTime = 0f;
        float warningDuration = 3f; // Duration for the warning text

        while (elapsedTime < warningDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

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


