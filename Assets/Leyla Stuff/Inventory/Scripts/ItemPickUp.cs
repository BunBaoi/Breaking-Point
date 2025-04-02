using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ItemPickUp : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Item item;
    [SerializeField] private float raycastDistance = 5f; // Distance to check for raycast
    [SerializeField] private float pickupRadius = 1f; // Radius around the centre of the screen for pickup detection
    [SerializeField] private string playerCameraTag = "PlayerCamera";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask itemLayer; // Inventory item layer
    [SerializeField] private LayerMask pickUpColliderLayer;

    [SerializeField] private bool canPickUp = false;
    [SerializeField] private bool isPickingUp = false;

    [Header("Interact Text")]
    [SerializeField] private GameObject interactTextPrefab;

    private GameObject interactTextInstance;
    private Transform player;
    private GameObject iconObject;
    private SpriteRenderer spriteRenderer;

    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string pickupActionName = "PickUp"; 

    private InputAction pickupAction;

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

        animator.enabled = true;

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

    void UpdateSpriteScale()
    {
        if (spriteRenderer == null) return;

        // Get the keybinding and check the device being used (controller or keyboard/mouse)
        InputAction action = inputActions.FindAction(pickupActionName);
        if (action == null) return;

        int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;
        if (action.bindings.Count <= bindingIndex) return;

        InputBinding binding = action.bindings[bindingIndex];
        string boundKeyOrButton = KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);
        if (string.IsNullOrEmpty(boundKeyOrButton))
        {
            Debug.LogWarning($"No key binding found for action: {pickupActionName}");
            return;
        }

        Debug.Log($"Bound Key or Button for action '{pickupActionName}': {boundKeyOrButton}");

        // Check if it's a mouse button
        bool isMouseButton = boundKeyOrButton.Contains("Mouse") || boundKeyOrButton.Contains("Click") || boundKeyOrButton.Contains("Scroll");

        // Set the scale based on whether it's a mouse button or not
        float scale = isMouseButton ? 0.2f : 0.08f;
        spriteRenderer.transform.localScale = new Vector3(scale, scale, scale);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            canPickUp = true;
            if (interactTextInstance != null)
            {
                UpdateSprite(iconObject.gameObject, pickupActionName);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            canPickUp = false;
            player = null;
            if (interactTextInstance != null)
            {
                HideInteractText();
            }
        }
    }

    void ShowInteractText()
    {
        if (interactTextInstance == null && interactTextPrefab != null)
        {
            interactTextInstance = Instantiate(interactTextPrefab);

            // Set the parent of the interact text instance
            interactTextInstance.transform.SetParent(transform, false);

            // Find the "Pick Up Collider" child
            Transform pickUpColliderTransform = transform.Find("Pick Up Collider");

            if (pickUpColliderTransform != null)
            {
                // Get the collider of the "Pick Up Collider"
                Collider pickUpCollider = pickUpColliderTransform.GetComponent<Collider>();

                if (pickUpCollider != null)
                {
                    // Get the top position of the "Pick Up Collider" in world space
                    Vector3 pickUpTopWorldPos = pickUpCollider.bounds.max;

                    // Convert the world position to local position relative to the parent
                    Vector3 pickUpTopLocalPos = interactTextInstance.transform.InverseTransformPoint(pickUpTopWorldPos);

                    // Position the interact text just above the top of the "Pick Up Collider"
                    interactTextInstance.transform.localPosition = new Vector3(0, pickUpTopLocalPos.y + 0.2f, 0); // Adjust the Y offset as needed
                }
                else
                {
                    // If no collider is attached to "Pick Up Collider", fallback position
                    interactTextInstance.transform.localPosition = new Vector3(0, 0.2f, 0);
                }
            }
            else
            {
                // If no "Pick Up Collider" child is found, fallback position
                interactTextInstance.transform.localPosition = new Vector3(0, 0.2f, 0);
            }

        string interactText = "Pick Up"; // Default text

            KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(pickupActionName);

            // Update text dynamically to match the correct keybinding based on input device
            TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = "Pick Up";

                if (keyBinding != null)
                {
                    Sprite icon = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

                    if (icon != null)
                    {
                        // Create a object for the sprite and set it next to the text
                        iconObject = new GameObject("KeybindIcon");
                        iconObject.transform.SetParent(interactTextInstance.transform); // Make it a child of the text

                        // Position sprite to left of text
                        float horizontalOffset = -textMesh.preferredWidth / 2 - 0.04f; // Increased offset to add more space
                        iconObject.transform.localPosition = new Vector3(horizontalOffset, 0f, 0);

                        // Add a SpriteRenderer to display the icon
                        spriteRenderer = iconObject.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = icon;
                        spriteRenderer.sortingOrder = 1;

                        UpdateSpriteScale();

                        UpdateSprite(iconObject.gameObject, pickupActionName);
                    }
                    else
                    {
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
                            keyText = keyText.Replace("Press ", "").Trim();

                            // Set the fallback text to show the keybinding for "Interact"
                            interactText = $"[{keyText}] Pick Up";
                        }
                        else
                        {
                            Debug.LogError("Pick Up action not found in InputActionAsset");
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

    void Update()
    {
        // --- Rotate pick up text based on player and camera ---
        if (canPickUp && interactTextInstance != null && player != null)
        {
            GameObject playerCamera = GameObject.FindGameObjectWithTag(playerCameraTag);

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

        // --- Show pick up text ---
        if (canPickUp && interactTextPrefab != null && player != null)
        {
            Camera playerCamera = FindCameraWithTag(playerCameraTag);

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
                        ShowInteractText();
                        if (isPickingUp)
                        {
                            HideInteractText();
                        }
                    }
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
        }                        

        // --- When triggering pick up action ---
        if (canPickUp && pickupAction.triggered)
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
                                HideInteractText();
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
                                    HideInteractText();
                                    inventory.DisableItemPickup(item);
                                    Destroy(gameObject);
                                }
                            }
                            else
                            {
                                Debug.LogWarning("InventoryManager component not found on player.");
                            }

                            Invoke("ResetPickingUpFlag", 0.5f);
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
