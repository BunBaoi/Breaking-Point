using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class OxygenRechargeStation : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private float refillRate = 1f; // Default refill rate per second
    [SerializeField] private bool isRefilling = false;
    [SerializeField] private bool isPlayerInTrigger = false; // Track if player is in trigger
    [SerializeField] private PlayerStats playerStats;

    [Header("Required Item")]
    [SerializeField] private Item requiredItem;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions; // Reference to the Input Action Asset
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private GameObject interactTextPrefab; // Prefab for interaction text
    private GameObject interactTextInstance;
    private GameObject iconObject;
    private InputAction interactAction;
    private Transform player; // Reference to the player's transform

    private void Start()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            inventoryManager = player.GetComponent<InventoryManager>(); // Get InventoryManager attached to "Player"
        }
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            interactAction = inputActions.FindAction(interactActionName);
            if (interactAction != null)
            {
                interactAction.Enable();
            }
            else
            {
                Debug.LogWarning($"Action '{interactActionName}' not found in InputActionAsset.");
            }
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.Disable();
        }
    }

    private void Update()
    {
        if (isPlayerInTrigger && interactTextInstance != null && player != null)
        {
            // Make the text only rotate left and right (Y-axis only)
            Vector3 lookDirection = player.position - interactTextInstance.transform.position;
            lookDirection.y = 0; // Ignore vertical rotation
            interactTextInstance.transform.forward = -lookDirection.normalized; // Fix backwards issue

            if (isPlayerInTrigger && IsHoldingRequiredItem() && IsLookingAtOxygenStation())
            {
                // Check if interact button is held down
                if (interactAction.IsPressed())
                {
                    if (!isRefilling)
                    {
                        StartRefilling();
                    }
                }
                else
                {
                    StopRefilling();
                }
            }
        }
    }

    private void StartRefilling()
    {
        if (playerStats != null && IsLookingAtOxygenStation())
        {
            Debug.Log("Refilling oxygen...");
            isRefilling = true;
            StartCoroutine(RefillOxygen());
        }
    }

    private void StopRefilling()
    {
        if (isRefilling)
        {
            Debug.Log("Stopped refilling oxygen");
            isRefilling = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && IsHoldingRequiredItem())
        {
            player = other.transform;
            isPlayerInTrigger = true;

            if (interactTextPrefab != null && interactTextInstance == null)
            {
                interactTextInstance = Instantiate(interactTextPrefab);
                interactTextInstance.transform.SetParent(transform, false);
                interactTextInstance.transform.localPosition = new Vector3(0, 0.5f, 0);

                // Declare the interactText variable
                string interactText = "to Refill"; // Default text

                // Get the keybinding data for "Interact"
                KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(interactActionName);

                // Update text dynamically to match the correct keybinding based on input device
                TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
                if (textMesh != null)
                {
                    textMesh.text = "to Refill";

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
                                interactText = $"[{keyText}] to Refill";
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

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && IsHoldingRequiredItem())
        {
            UpdateSprite(iconObject.gameObject, interactActionName);
            playerStats = other.GetComponent<PlayerStats>();
            isPlayerInTrigger = true;
            Debug.Log("player entered oxygen refill trigger");
        }
        if (interactTextInstance != null && !IsHoldingRequiredItem())
        {
            Destroy(interactTextInstance);
            interactTextInstance = null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            isRefilling = false;
            player = null;
            Debug.Log("player exited oxygen refill trigger");

            if (interactTextInstance != null)
            {
                Destroy(interactTextInstance);
                interactTextInstance = null;
            }
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

    private bool IsLookingAtOxygenStation()
    {
        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");

        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerCamera not found! Ensure it has the correct tag.");
            return false;
        }

        Transform cameraTransform = playerCamera.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        // Define the layer mask to only check objects on the "OxygenLayer" layer.
        int layerMask = LayerMask.GetMask("Oxygen Refill Station");

        // Perform the raycast with the layer mask to limit the detection to that layer.
        if (Physics.Raycast(ray, out hit, 3f, layerMask)) // Check within 3 meters
        {
            return hit.collider.CompareTag("Oxygen Refill Station"); // Ensure the looked-at object has the correct tag
        }

        return false;
    }

    private bool IsHoldingRequiredItem()
    {
        if (inventoryManager == null || requiredItem == null) return false;

        Item equippedItem = inventoryManager.GetEquippedItem();
        return equippedItem != null && equippedItem == requiredItem;
    }

    private IEnumerator RefillOxygen()
    {
        while (isRefilling && playerStats != null && playerStats.OxygenTank < 100)
        {
            playerStats.OxygenTank += refillRate * Time.deltaTime;
            playerStats.OxygenTank = Mathf.Min(playerStats.OxygenTank, 100f);
            yield return null;
        }
    }
}