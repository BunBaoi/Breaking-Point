using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;

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

    private void Awake()
    {
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
        // Check if the action has a Mouse movement binding and skip it, but allow mouse buttons
        foreach (var binding in action.bindings)
        {
            if (binding.path.Contains("Mouse") &&
                (binding.path.Contains("delta") || binding.path.Contains("scroll/y") || binding.path.Contains("scroll/x")))
            {
                Debug.Log($"Skipping action '{action.name}' due to Mouse movement or scrolling binding.");
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

    private TMP_Text previousKeybindText = null;
    private string originalKeybindText = "";

    void StartRebinding(InputAction action, TMP_Text keybindText, bool isController, int bindingIndex)
    {
        if (rebindingOperation != null)
        {
            rebindingOperation.Cancel();
        }

        originalKeybindText = keybindText.text;
        previousKeybindText = keybindText;

        action.Disable();
        keybindText.text = "...";
        int index = bindingIndex;

        rebindingOperation = action.PerformInteractiveRebinding(index)
    .WithControlsExcluding("Mouse/delta")   // Exclude mouse movement
    .WithControlsExcluding("Mouse/scroll/y") // Exclude vertical scroll
    .WithControlsExcluding("Mouse/scroll/x") // Exclude horizontal scroll
    /*.WithControlsExcluding("<Mouse>/leftButton")
    .WithControlsExcluding("<Pointer>/press")*/
    .OnComplete(operation =>
    {
        string newKey = action.bindings[index].ToDisplayString()
            .Replace("Press ", "")
            .Replace("Hold", "");

        Debug.Log($"Keybinding changed to: {newKey}");

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

        keybindText.text = newKey != "" ? newKey : action.bindings[index].ToDisplayString();

        if (isConflict)
        {
            ShowWarningText("Note: This key is the same as another action, recommended to only have a max of 2 per key!");
        }

        OnKeyBindingsChanged?.Invoke(action.name, newKey);

        SaveKeybinds();

        operation.Dispose();
        action.Enable();
    })
    .OnCancel(operation =>
    {
        if (previousKeybindText != null)
        {
            previousKeybindText.text = originalKeybindText;
        }

        operation.Dispose();
        action.Enable();
    })
    .Start();
    }

        private void SaveKeybinds()
    {
        var bindings = new Dictionary<string, string>();

        foreach (var action in actionDictionary.Values)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite) // Avoid composite bindings (e.g., WASD)
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

