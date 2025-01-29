using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

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
        }

        if (action.bindings.Count > 1 && action.bindings[0].isComposite)
        {
            string[] directions = { "Up", "Down", "Left", "Right" };

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
                        .Replace("Press ", "").Replace("Hold", "");
                    compositeKeybindButtonKeyboard.onClick.AddListener(() => StartRebinding(action, compositeKeybindTextKeyboard, false, keyboardIndex));

                    instantiatedButtons.Add(compositeKeybindButtonKeyboard); // Add to list
                }

                int controllerIndex = i + 6;
                if (controllerIndex < action.bindings.Count && action.bindings[controllerIndex].path.Contains("Controller"))
                {
                    compositeKeybindTextController.text = action.bindings[controllerIndex].ToDisplayString()
                        .Replace("Press ", "").Replace("Hold", "");
                    compositeKeybindButtonController.onClick.AddListener(() => StartRebinding(action, compositeKeybindTextController, true, controllerIndex));

                    instantiatedButtons.Add(compositeKeybindButtonController); // Add to list
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
                keybindTextKeyboard.text = action.bindings[0].ToDisplayString().Replace("Press ", "").Replace("Hold", "");
                keybindButtonKeyboard.onClick.AddListener(() => StartRebinding(action, keybindTextKeyboard, false, 0));

                instantiatedButtons.Add(keybindButtonKeyboard); // Add to list
            }

            if (action.bindings.Count > 1)
            {
                keybindTextController.text = action.bindings[1].ToDisplayString().Replace("Press ", "").Replace("Hold", "");
                keybindButtonController.onClick.AddListener(() => StartRebinding(action, keybindTextController, true, 1));

                instantiatedButtons.Add(keybindButtonController); // Add to list
            }
        }
    }

    private TMP_Text previousKeybindText = null;
    private string originalKeybindText = "";

    private bool isRebindingInProgress = false;

    void StartRebinding(InputAction action, TMP_Text keybindText, bool isController, int bindingIndex)
    {
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

        // Set rebinding flag to true to track the operation status
        isRebindingInProgress = true;

        rebindingOperation = action.PerformInteractiveRebinding(index)
            .WithControlsExcluding("Mouse/delta")   // Exclude mouse movement
            .WithControlsExcluding("Mouse/scroll/y") // Exclude vertical scroll
            .WithControlsExcluding("Mouse/scroll/x") // Exclude horizontal scroll
            .WithControlsExcluding("Mouse/scroll/up")
            .WithControlsExcluding("Mouse/scroll/down")
            .WithControlsExcluding("Mouse/scroll/right")
            .WithControlsExcluding("Mouse/scroll/left")
            .OnMatchWaitForAnother(0.2f) // Wait for 200ms for a valid input
            .OnComplete(operation =>
            {
                if (!isRebindingInProgress) return; // Ignore completion if the rebinding was interrupted

            string newKey = action.bindings[index].ToDisplayString()
                    .Replace("Press ", "")
                    .Replace("Hold", "")
                    .Trim();

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
                    if (isConflict) break;
                }

            // Check if the rebinding operation was interrupted or left click was pressed
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
                    ShowWarningText("Note: This key is the same as another action, recommended to only have a max of 2 per key!");
                }

                OnKeyBindingsChanged?.Invoke(action.name, newKey);
                SaveKeybinds();

            // Reset rebinding flag once done
            isRebindingInProgress = false;
                operation.Dispose();
                action.Enable();
            })
            .OnCancel(operation =>
            {
                if (previousKeybindText != null)
                {
                    previousKeybindText.text = originalKeybindText;
                }

            // Reset rebinding flag if canceled
            isRebindingInProgress = false;
                operation.Dispose();
                action.Enable();
            })
            .Start();
    }

    public void SaveKeybinds()
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

