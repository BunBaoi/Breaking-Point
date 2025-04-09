using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private DialogueTree npcDialogueTree;
    [SerializeField] private CompanionScript companionScript;
    [SerializeField] private string dialogueKey = "DialogueTriggered";

    [Header("Interact Text")]
    [SerializeField] private GameObject interactTextPrefab;
    [SerializeField] private float yAxis = 0.9f;
    [SerializeField] private float defaultYAxis = 0.9f;

    [Header("Testing Purposes")]
    [SerializeField] private bool playerInRange = false; // Is player in range?
    [SerializeField] private bool isDialoguePressed;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private KeyCode clearPlayerPrefs = KeyCode.C;

    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>();
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>();

    private GameObject interactTextInstance;
    private Transform player;
    private GameObject iconObject;
    private SpriteRenderer spriteRenderer;

    private InputAction interactAction;

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
        // isDialoguePressed = PlayerPrefs.GetInt(dialogueKey, 0) == 1;

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

    void ShowInteractText()
    {
        if (DialogueManager.Instance.GetDialogueProgress(npcDialogueTree.treeID))
        {
            Debug.Log("Dialogue with treeID " + npcDialogueTree.treeID + " has already been completed.");
            return;
        }

        if (!isDialoguePressed && interactTextPrefab != null && interactTextInstance == null && CanStartDialogue())
        {
            interactTextInstance = Instantiate(interactTextPrefab);

            interactTextInstance.transform.SetParent(transform, false);

            Transform objectColliderTransform = transform.Find("NPC Mesh");

            if (objectColliderTransform != null)
            {
                Collider objectCollider = objectColliderTransform.GetComponent<Collider>();

                if (objectCollider != null)
                {
                    Vector3 objectTopWorldPos = objectCollider.bounds.max;

                    // Convert the world position to local position relative to the parent
                    Vector3 pickUpTopLocalPos = interactTextInstance.transform.InverseTransformPoint(objectTopWorldPos);

                    Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

                    if (playerTransform != null)
                    {
                        // Direction from NPC to player
                        Vector3 toPlayer = (playerTransform.position - transform.position).normalized;

                        // Set position slightly in front of NPC, facing the player
                        Vector3 frontOfNPC = transform.position + toPlayer * 1.0f;

                        frontOfNPC.y += yAxis;

                        // Set world position
                        interactTextInstance.transform.position = frontOfNPC;

                        // Rotate text to face player
                        Vector3 lookDirection = (playerTransform.position - frontOfNPC);
                        lookDirection.y = 0;
                        if (lookDirection != Vector3.zero)
                        {
                            interactTextInstance.transform.rotation = Quaternion.LookRotation(lookDirection);
                        }
                    }
                }
                else
                {
                    interactTextInstance.transform.localPosition = new Vector3(0, defaultYAxis, 0);
                }
            }
            else
            {
                interactTextInstance.transform.localPosition = new Vector3(0, defaultYAxis, 0);
            }

            string interactText = "Talk"; // Default text

            KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(interactActionName);

            // Update text dynamically to match the correct keybinding based on input device
            TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = "Talk";

                // Now check if we have a keybinding sprite
                if (keyBinding != null)
                {
                    Sprite icon = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

                    // If the sprite exists, display it next to the text
                    if (icon != null)
                    {
                        iconObject = new GameObject("KeybindIcon");
                        iconObject.transform.SetParent(interactTextInstance.transform);

                        float horizontalOffset = -textMesh.preferredWidth / 2 - 0.04f;
                        iconObject.transform.localPosition = new Vector3(horizontalOffset, 0f, 0);
                        iconObject.transform.rotation = interactTextInstance.transform.rotation;

                        // Add a SpriteRenderer to display the icon
                        spriteRenderer = iconObject.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = icon;
                        spriteRenderer.sortingOrder = 1;

                        UpdateSpriteScale();

                        UpdateSprite(iconObject.gameObject, interactActionName);
                    }
                    else
                    {
                        string keyText = "";

                        var interactAction = inputActions.FindAction(interactActionName);

                        if (interactAction != null)
                        {
                            // If using a controller, get the second binding (controller binding)
                            if (KeyBindingManager.Instance.IsUsingController())
                            {
                                keyText = interactAction.bindings[1].ToDisplayString(); 
                            }
                            else
                            {
                                keyText = interactAction.bindings[0].ToDisplayString();
                            }

                            keyText = keyText.Replace("Press ", "").Trim(); // Removes "Press" and any extra spaces

                            interactText = $"[{keyText}] Talk";
                        }
                        else
                        {
                            Debug.LogError("Interact action not found in InputActionAsset");
                        }
                    }

                    textMesh.text = interactText;
                }
            }
        }
    }

    void HideInteractText()
    {
        if (interactTextInstance != null)
        {
            Destroy(interactTextInstance);
            interactTextInstance = null;
        }
    }

    void UpdateSpriteScale()
    {
        if (spriteRenderer == null) return;

        // Get the keybinding and check the device being used (controller or keyboard/mouse)
        InputAction action = inputActions.FindAction(interactActionName);
        if (action == null) return;

        int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;
        if (action.bindings.Count <= bindingIndex) return;

        InputBinding binding = action.bindings[bindingIndex];
        string boundKeyOrButton = KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);
        if (string.IsNullOrEmpty(boundKeyOrButton))
        {
            Debug.LogWarning($"No key binding found for action: {interactActionName}");
            return;
        }

        Debug.Log($"Bound Key or Button for action '{interactActionName}': {boundKeyOrButton}");

        // Check if it's a mouse button
        bool isMouseButton = boundKeyOrButton.Contains("Mouse") || boundKeyOrButton.Contains("Click") || boundKeyOrButton.Contains("Scroll") 
            || boundKeyOrButton.Contains("leftStick") || boundKeyOrButton.Contains("rightStick");

        // Set the scale based on whether it's a mouse button or not
        float scale = isMouseButton ? 0.2f : 0.08f;
        spriteRenderer.transform.localScale = new Vector3(scale, scale, scale);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.transform;
        }
    }

    private void UpdateSprite(GameObject iconObject, string actionName)
    {
        if (KeyBindingManager.Instance == null || iconObject == null || inputActions == null) return;

        InputAction action = inputActions.FindAction(actionName);
        if (action == null) return;

        int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;
        if (action.bindings.Count <= bindingIndex) return;

        InputBinding binding = action.bindings[bindingIndex];

        string boundKeyOrButton = KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);
        if (string.IsNullOrEmpty(boundKeyOrButton))
        {
            Debug.LogWarning($"No key binding found for action: {actionName}");
            return;
        }

        bool isUsingController = KeyBindingManager.Instance.IsUsingController();

        KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(actionName);
        if (keyBinding == null) return;

        SpriteRenderer spriteRenderer = iconObject.GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

        Animator animator = iconObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = iconObject.AddComponent<Animator>();
        }

        animator.enabled = true; // Ensure animator is enabled

        string folderPath = isUsingController ? "UI/Controller/" : "UI/Keyboard/";
        string animatorName = KeyBindingManager.Instance.GetSanitisedKeyName(boundKeyOrButton) + ".sprite";
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

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && playerInRange)
        {
            if (interactTextInstance != null)
            {
                UpdateSprite(iconObject.gameObject, interactActionName);

                Transform playerTransform = other.transform;

                // Get direction from NPC to player
                Vector3 toPlayer = (playerTransform.position - transform.position).normalized;

                // Position the interact text in front of NPC toward the player
                Vector3 frontOfNPC = transform.position + toPlayer * 1.0f;
                frontOfNPC.y += yAxis;
                interactTextInstance.transform.position = Vector3.Lerp(interactTextInstance.transform.position, frontOfNPC, Time.deltaTime * 15f);

                // Make it face the player
                Vector3 lookDirection = (playerTransform.position - frontOfNPC);
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    interactTextInstance.transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
            Debug.Log("Player exited NPC trigger zone.");

            HideInteractText();
        }
    }

    private void Update()
    {
        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (playerInRange && interactTextInstance != null && player != null)
        {
            if (playerCamera != null)
            {
                Vector3 lookDirection = player.position - interactTextInstance.transform.position;
                lookDirection.y = 0; // Keep the text rotation horizontal (no vertical tilt)

                interactTextInstance.transform.forward = -lookDirection.normalized;

                Vector3 currentEulerAngles = interactTextInstance.transform.eulerAngles;

                // Set the Y rotation of the interaction text based on the camera's X rotation
                currentEulerAngles.x = playerCamera.transform.eulerAngles.x;
                interactTextInstance.transform.eulerAngles = currentEulerAngles;
            }
        }

        RaycastHit hit;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out hit, 3f))
        {
            Debug.Log("Raycast hit: " + hit.collider.name);
            if (hit.collider != null && hit.collider.gameObject.name == "NPC Mesh" && hit.collider.transform.IsChildOf(transform) && playerInRange)
            {
                ShowInteractText();
                // Check if the interact key is pressed
                if (playerInRange && interactAction.triggered && !isDialoguePressed && CanStartDialogue())
                {
                    /*isDialoguePressed = true;
                    PlayerPrefs.SetInt(dialogueKey, 1);
                    PlayerPrefs.Save();*/
                    StartDialogue();

                    HideInteractText();
                }
            }
            else
            {
                HideInteractText();
            }
        }
        else
        {
            HideInteractText();
        }

        if (spriteRenderer != null)
        {
            // Dynamically update sprite scale if keybinding changes during the game
            UpdateSpriteScale();
        }

        /*if (Input.GetKeyDown(clearPlayerPrefs))
        {
            ClearPlayerPrefs();
        }*/
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
        if (companionScript != null)
        {
            companionScript.TeleportToPlayer();
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
