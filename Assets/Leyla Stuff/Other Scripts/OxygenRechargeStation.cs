using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using FMODUnity;
using FMOD.Studio;

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
    private SpriteRenderer spriteRenderer;

    [Header("FMOD")]
    [SerializeField] private EventReference refillSoundEvent;

    private EventInstance refillSoundInstance;
    private bool isRefillSoundPlaying = false;

    private void Start()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            inventoryManager = player.GetComponent<InventoryManager>();
        }

        refillSoundInstance = RuntimeManager.CreateInstance(refillSoundEvent);
        RuntimeManager.AttachInstanceToGameObject(refillSoundInstance, transform, GetComponent<Rigidbody>());
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

        refillSoundInstance.release();
    }

    private void Update()
    {
        if (isPlayerInTrigger && interactTextInstance != null && player != null)
        {
            GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");

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

        if (spriteRenderer != null)
        {
            // Dynamically update sprite scale if keybinding changes during the game
            UpdateSpriteScale();
        }

        if (isPlayerInTrigger)
        {
            if (IsHoldingRequiredItem() && IsLookingAtOxygenStation())
            {
                ShowInteractText();
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
            else
            {
                HideInteractText();
            }
        }
    }

        private void StartRefilling()
    {
        if (playerStats != null && IsLookingAtOxygenStation())
        {
            Debug.Log("Refilling oxygen...");
            isRefilling = true;

            if (!isRefillSoundPlaying && playerStats.Oxygen < 100)
            {
                refillSoundInstance.start();
                isRefillSoundPlaying = true;
            }

            StartCoroutine(RefillOxygen());
        }
    }

    private void StopRefilling()
    {
        if (isRefilling)
        {
            Debug.Log("Stopped refilling oxygen");
            isRefilling = false;

            if (isRefillSoundPlaying)
            {
                refillSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                isRefillSoundPlaying = false;
            }
        }
    }

    void ShowInteractText()
    {
        if (interactTextInstance == null && interactTextPrefab != null)
        {
            interactTextInstance = Instantiate(interactTextPrefab);

            interactTextInstance.transform.SetParent(transform, false);

            Transform objectColliderTransform = transform.Find("Oxygen Tank Refill Station");

            if (objectColliderTransform != null)
            {
                Collider objectCollider = objectColliderTransform.GetComponent<Collider>();

                if (objectCollider != null)
                {
                    Vector3 objectTopWorldPos = objectCollider.bounds.max;

                    // Convert the world position to local position relative to the parent
                    Vector3 pickUpTopLocalPos = interactTextInstance.transform.InverseTransformPoint(objectTopWorldPos);

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

            // Declare the interactText variable
            string interactText = "Refill"; // Default text

            // Get the keybinding data for "Interact"
            KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(interactActionName);

            // Update text dynamically to match the correct keybinding based on input device
            TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = "Refill";

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
                        float horizontalOffset = -textMesh.preferredWidth / 2 - 0.04f; // Increased offset to add more space
                        iconObject.transform.localPosition = new Vector3(horizontalOffset, 0f, 0);

                        // Add a SpriteRenderer to display the icon
                        spriteRenderer = iconObject.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = icon;
                        spriteRenderer.sortingOrder = 1;

                        UpdateSpriteScale();

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
                            interactText = $"[{keyText}] Refill";
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

    void HideInteractText()
    {
        if (interactTextInstance != null)
        {
            Destroy(interactTextInstance);
            interactTextInstance = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && IsHoldingRequiredItem())
        {
            player = other.transform;
            isPlayerInTrigger = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && IsHoldingRequiredItem())
        {
            playerStats = other.GetComponent<PlayerStats>();
            player = other.transform;
            isPlayerInTrigger = true;
            Debug.Log("player entered oxygen refill trigger");
            if (interactTextInstance != null)
            {
                UpdateSprite(iconObject.gameObject, interactActionName);
            }
        }
if (interactTextInstance != null && !IsHoldingRequiredItem() && !IsLookingAtOxygenStation())
        {
            HideInteractText();
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
                HideInteractText();
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
        while (isRefilling && playerStats != null && playerStats.Oxygen < 100)
        {
            playerStats.Oxygen += refillRate * Time.deltaTime;

            playerStats.Oxygen = Mathf.Min(playerStats.Oxygen, 100f);

            if (playerStats.Oxygen >= 100f && isRefillSoundPlaying)
            {
                refillSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                isRefillSoundPlaying = false;
            }

            yield return null;
        }
    }
}