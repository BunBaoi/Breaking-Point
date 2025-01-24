using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private DialogueTree npcDialogueTree; // NPC's dialogue tree reference
    [SerializeField] private string dialogueKey = "DialogueTriggered";

    [Header("Testing Purposes")]
    [SerializeField] private bool playerInRange = false; // Is player in range?
    [SerializeField] private bool isDialoguePressed;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions; // Reference to the Input Action Asset
    [SerializeField] private string interactActionName = "Interact"; // Action name as a string that can be edited in the inspector
    [SerializeField] private KeyCode clearPlayerPrefs = KeyCode.C;
    [SerializeField] private GameObject interactTextPrefab; // Prefab for interaction text

    private GameObject interactTextInstance; // Reference to instantiated text
    private Transform player; // Reference to the player's transform
    private GameObject iconObject; // Declare it at the class level

    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>(); // List of bool keys that should be true
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>(); // List of bool keys that should be false

    private InputAction interactAction; // Reference to the "Interact" InputAction

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

    private void Start()
    {
        isDialoguePressed = PlayerPrefs.GetInt(dialogueKey, 0) == 1;

        // Find the action dynamically using the interactActionName string
        interactAction = inputActions.FindAction(interactActionName);

        if (interactAction != null)
        {
            interactAction.Enable(); // Enable the action
        }
        else
        {
            Debug.LogError($"Input action '{interactActionName}' not found in Input Action Asset!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.transform; // Store player reference
            Debug.Log("Player entered NPC trigger zone.");

            // Prevent text from appearing if dialogue is already triggered
            if (!isDialoguePressed && interactTextPrefab != null && interactTextInstance == null && CanStartDialogue())
            {
                interactTextInstance = Instantiate(interactTextPrefab);
                interactTextInstance.transform.SetParent(transform, false); // Keep local position
                interactTextInstance.transform.localPosition = new Vector3(0, 0.5f, 0); // Position 0.5 above NPC

                // Declare the interactText variable
                string interactText = "to Interact"; // Default text

                // Get the keybinding data for "Interact"
                KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(interactActionName);

                // Update text dynamically to match the correct keybinding based on input device
                TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
                if (textMesh != null)
                {
                    // We start by setting the "to Interact" text
                    textMesh.text = "to Interact";

                    // Now check if we have a keybinding sprite
                    if (keyBinding != null)
                    {
                        Sprite icon = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

                        // If the sprite exists, display it next to the text
                        if (icon != null)
                        {
                            // Create a object for the sprite and set it next to the text
                            iconObject = new GameObject("KeybindIcon");
                            iconObject.transform.SetParent(interactTextInstance.transform); // Make it a child of the text

                            // Position sprite to left of text
                            // Increase the horizontal space by adjusting the x-position further
                            float horizontalOffset = -textMesh.preferredWidth / 2 - 0.5f; // Increased offset to add more space
                            iconObject.transform.localPosition = new Vector3(horizontalOffset, 0.7f, 0);

                            // Add a SpriteRenderer to display the icon
                            SpriteRenderer spriteRenderer = iconObject.AddComponent<SpriteRenderer>();
                            spriteRenderer.sprite = icon;
                            spriteRenderer.sortingOrder = 1; // Ensure the sprite is above the text

                            spriteRenderer.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                            UpdateSprite(iconObject.gameObject, interactActionName);
                        }
                        else
                        {
                            // Get the first binding for keyboard and second for controller directly from the InputActionAsset
                            string keyText = "";

                            // Get the "Interact" action
                            var interactAction = inputActions.FindAction(interactActionName);

                            if (interactAction != null)
                            {
                                // If using a controller, get the second binding (controller binding)
                                if (KeyBindingManager.Instance.IsUsingController())
                                {
                                    keyText = interactAction.bindings[1].ToDisplayString();  // Second binding (controller)
                                }
                                else
                                {
                                    keyText = interactAction.bindings[0].ToDisplayString();  // First binding (keyboard)
                                }

                                // Remove the word "Press" from the keyText if it exists
                                keyText = keyText.Replace("Press ", "").Trim(); // Removes "Press" and any extra spaces

                                // Set the fallback text to show the keybinding for "Interact"
                                interactText = $"[{keyText}] to Interact";
                            }
                            else
                            {
                                Debug.LogError("Interact action not found in InputActionAsset");
                            }
                        }

                        // Set the updated text (with sprite or keybinding fallback)
                        textMesh.text = interactText;
                    }
                }
            }
        }
    }

    private void UpdateSprite(GameObject iconObject, string actionName)
    {
        if (KeyBindingManager.Instance == null || iconObject == null || inputActions == null) return;

        KeyBinding binding = KeyBindingManager.Instance.GetKeybinding(actionName);
        if (binding == null) return;

        bool isUsingController = KeyBindingManager.Instance.IsUsingController();

        // Ensure the GameObject has a SpriteRenderer
        SpriteRenderer spriteRenderer = iconObject.GetComponent<SpriteRenderer>();

        // Assign sprite based on input type
        spriteRenderer.sprite = isUsingController ? binding.controllerSprite : binding.keySprite;

        // Check for an animator and assign one if needed
        Animator animator = iconObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = iconObject.AddComponent<Animator>();
        }

        animator.enabled = true; // Ensure animator is enabled

        string folderPath = isUsingController ? "UI/Controller/" : "UI/Keyboard/";
        string animatorName = KeyBindingManager.Instance.GetSanitisedKeyName(GetBoundKeyOrButton(actionName)) + ".sprite";
        RuntimeAnimatorController assignedAnimator = Resources.Load<RuntimeAnimatorController>(folderPath + animatorName);

        if (assignedAnimator != null)
        {
            animator.runtimeAnimatorController = assignedAnimator;
            Debug.Log($"Assigned animator '{animatorName}' to {iconObject.name}");
        }
        else
        {
            Debug.LogError($"Animator '{animatorName}' not found in {folderPath}");
        }
    }

    private string GetBoundKeyOrButton(string actionName)
    {
        if (inputActions == null) return null;

        InputAction action = inputActions.FindAction(actionName);

        if (action == null || action.bindings.Count == 0) return null;

        foreach (var binding in action.bindings)
        {
            if (binding.isPartOfComposite) continue;

            return KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);
        }

        return null;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UpdateSprite(iconObject.gameObject, interactActionName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
            Debug.Log("Player exited NPC trigger zone.");

            if (interactTextInstance != null)
            {
                Destroy(interactTextInstance);
                interactTextInstance = null;
            }
        }
    }

    private void Update()
    {
        if (playerInRange && interactTextInstance != null && player != null)
        {
            // Make the text only rotate left and right (Y-axis only)
            Vector3 lookDirection = player.position - interactTextInstance.transform.position;
            lookDirection.y = 0; // Ignore vertical rotation
            interactTextInstance.transform.forward = -lookDirection.normalized; // Fix backwards issue
        }

        // Check if the interact key is pressed
        if (playerInRange && interactAction.triggered && !isDialoguePressed && CanStartDialogue())
        {
            isDialoguePressed = true;
            PlayerPrefs.SetInt(dialogueKey, 1);
            PlayerPrefs.Save();
            StartDialogue();

            // Remove interact text when dialogue starts
            if (interactTextInstance != null)
            {
                Destroy(interactTextInstance);
                interactTextInstance = null;
            }
        }

        if (Input.GetKeyDown(clearPlayerPrefs))
        {
            ClearPlayerPrefs();
        }
    }

    private bool CanStartDialogue()
    {
        // Check if all required bool conditions are met (true or false based on lists)
        foreach (string boolKey in requiredBoolKeysTrue)
        {
            if (!BoolManager.Instance.GetBool(boolKey))
            {
                return false; // If any bool is false when it should be true, return false
            }
        }

        foreach (string boolKey in requiredBoolKeysFalse)
        {
            if (BoolManager.Instance.GetBool(boolKey))
            {
                return false; // If any bool is true when it should be false, return false
            }
        }

        return true; // All conditions are met, return true
    }

    private void StartDialogue()
    {
        if (npcDialogueTree != null)
        {
            DialogueManager.Instance.StartDialogue(npcDialogueTree);
        }
    }

    private void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs cleared!");
        isDialoguePressed = false;
    }
}
