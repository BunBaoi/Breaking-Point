using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ItemPickUp : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Item item; // Reference to the item that can be picked up
    [SerializeField] private float raycastDistance = 5f; // Distance to check for raycast
    [SerializeField] private float pickupRadius = 1f; // Radius around the centre of the screen for pickup detection
    [SerializeField] private string playerCameraTag = "PlayerCamera"; // Tag for the player's camera
    [SerializeField] private string playerTag = "Player"; // Tag for the player object
    [SerializeField] private LayerMask itemLayer; // Layer mask to specify which layers are considered as items
    [SerializeField] private LayerMask pickUpColliderLayer; // Layer mask to specify which layers are considered as pick-up colliders

    [SerializeField] private bool canPickUp = false; // Flag to check if the player is in range
    [SerializeField] private bool isPickingUp = false; // Flag to prevent picking up multiple items at once

    [Header("Interact Text")]
    [SerializeField] private GameObject interactTextPrefab; // Prefab for interaction text

    private GameObject interactTextInstance; // Reference to instantiated text
    private Transform player; // Reference to the player's transform
    private GameObject iconObject; // Declare it at the class level

    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string pickupActionName = "PickUp"; // Action name for item pickup

    private InputAction pickupAction; // Reference to the input action for pickup

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
    void Start()
    {
        // Get Pickup action from Input Action Asset
        pickupAction = inputActions.FindAction(pickupActionName);

        if (pickupAction != null)
        {
            pickupAction.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{pickupActionName}' not found in Input Action Asset!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            canPickUp = true; // Player is in range to pick up the item
            player = other.transform;
            if (interactTextPrefab != null && interactTextInstance == null)
            {
                interactTextInstance = Instantiate(interactTextPrefab);
                interactTextInstance.transform.SetParent(transform, false); // Keep local position
                interactTextInstance.transform.localPosition = new Vector3(0, 0.5f, 0); // Position 0.5 above NPC

                // Declare the interactText variable
                string interactText = "to Pick Up"; // Default text

                // Get the keybinding data for "Interact"
                KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(pickupActionName);

                // Update text dynamically to match the correct keybinding based on input device
                TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
                if (textMesh != null)
                {
                    // We start by setting the "to Interact" text
                    textMesh.text = "to Pick Up";

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

                            UpdateSprite(iconObject.gameObject, pickupActionName);
                        }
                        else
                        {
                            // Get the first binding for keyboard and second for controller directly from the InputActionAsset
                            string keyText = "";

                            // Get the "Interact" action
                            var interactAction = inputActions.FindAction(pickupActionName);

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
                                interactText = $"[{keyText}] to Pick Up";
                            }
                            else
                            {
                                Debug.LogError("Pick Up action not found in InputActionAsset");
                            }
                        }

                        // Set the updated text (with sprite or keybinding fallback)
                        textMesh.text = interactText;
                    }
                }
            }
        }
            Debug.Log("Player is in range to pick up the item.");
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
        if (other.CompareTag(playerTag))
        {
            canPickUp = true; // Player is in range to pick up the item
            UpdateSprite(iconObject.gameObject, pickupActionName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            canPickUp = false; // Player is out of range
            player = null;
            Debug.Log("Player is out of range to pick up the item.");
            if (interactTextInstance != null)
            {
                Destroy(interactTextInstance);
                interactTextInstance = null;
            }
        }
    }

    void Update()
    {
        if (canPickUp && interactTextInstance != null && player != null)
        {
            // Make the text only rotate left and right (Y-axis only)
            Vector3 lookDirection = player.position - interactTextInstance.transform.position;
            lookDirection.y = 0; // Ignore vertical rotation
            interactTextInstance.transform.forward = -lookDirection.normalized; // Fix backwards issue
        }

        if (canPickUp && pickupAction.triggered) // Check if pickup action is triggered
        {
            Camera playerCamera = FindCameraWithTag(playerCameraTag);

            if (playerCamera != null)
            {
                RaycastHit hit;
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

                // Create a layer mask that includes pickUpColliderLayer but excludes itemLayer
                LayerMask combinedMask = pickUpColliderLayer & ~itemLayer;

                // Perform a raycast to find the item in the center of the view
                if (Physics.Raycast(ray, out hit, raycastDistance, combinedMask))
                {
                    // Check if the hit collider matches the 'Pick Up Collider' child
                    if (IsHitOnPickUpCollider(hit.collider))
                    {
                        // Check if the item is within the camera's view frustum
                        if (IsWithinCameraView(playerCamera, hit.point))
                        {
                            if (isPickingUp)
                            {
                                if (interactTextInstance != null)
                                {
                                    Destroy(interactTextInstance);
                                    interactTextInstance = null;
                                }
                                Debug.Log("Already picking up an item.");
                                return;
                            }

                            // Prevent pickup if the player already has this item in the inventory
                            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                            InventoryManager inventory = player.GetComponent<InventoryManager>();

                            if (inventory != null)
                            {
                                if (inventory.HasItem(item)) // Check if player already has the item
                                {
                                    Debug.Log("Player already has this item.");
                                    return;
                                }

                                bool added = inventory.AddItem(item);
                                Debug.Log("Item pickup attempt: " + (added ? "Success" : "Failed"));

                                if (added)
                                {
                                    if (interactTextInstance != null)
                                    {
                                        Destroy(interactTextInstance);
                                        interactTextInstance = null;
                                    }
                                    inventory.DisableItemPickup(item);
                                    Destroy(gameObject); // Destroy the item in the world after picking it up
                                }
                            }
                            else
                            {
                                Debug.LogWarning("InventoryManager component not found on player.");
                            }

                            Invoke("ResetPickingUpFlag", 0.5f); // Adjust the delay as needed
                        }
                        else
                        {
                            Debug.Log("Item is not within the camera's view.");
                        }
                    }
                    else
                    {
                        Debug.Log("Hit collider does not match the 'Pick Up Collider' child.");
                    }
                }
                else
                {
                    Debug.Log("No item detected by the raycast.");
                }
            }
            else
            {
                Debug.LogWarning("Player Camera not found with the tag " + playerCameraTag + ".");
            }
        }
    }

    private bool IsWithinCameraView(Camera camera, Vector3 worldPosition)
    {
        // Convert world position to screen point
        Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);

        // Define a rect in screen space (center of the screen with some radius)
        Rect viewRect = new Rect(Screen.width / 2 - pickupRadius, Screen.height / 2 - pickupRadius, pickupRadius * 2, pickupRadius * 2);

        // Check if the screen point is within the rect
        return viewRect.Contains(screenPoint);
    }

    private bool IsHitOnPickUpCollider(Collider hitCollider)
    {
        // Check if the hit collider is the "Pick Up Collider" child
        Transform pickUpCollider = transform.Find("Pick Up Collider");

        if (pickUpCollider != null)
        {
            Collider collider = pickUpCollider.GetComponent<Collider>();

            if (collider == hitCollider && hitCollider.gameObject.layer == LayerMask.NameToLayer("Pick Up Item Collider"))
            {
                Debug.Log("Hit collider matches the 'Pick Up Collider' child and is on the correct layer.");
                return true;
            }
            else
            {
                Debug.Log("Hit collider does not match the 'Pick Up Collider' child or is not on the correct layer.");
            }
        }
        else
        {
            Debug.Log("No 'Pick Up Collider' child found.");
        }

        return false;
    }

    private Camera FindCameraWithTag(string tag)
    {
        GameObject cameraObject = GameObject.FindGameObjectWithTag(tag);
        if (cameraObject != null)
        {
            return cameraObject.GetComponent<Camera>();
        }
        return null;
    }

    private void ResetPickingUpFlag()
    {
        isPickingUp = false;
    }
}
